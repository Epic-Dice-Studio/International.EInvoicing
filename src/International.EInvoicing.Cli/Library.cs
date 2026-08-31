using International.EInvoicing.Configuration;
using International.EInvoicing.Countries.Belgium;
using International.EInvoicing.Countries.France;
using International.EInvoicing.Countries.Germany;
using International.EInvoicing.Diagnostics;
using International.EInvoicing.FacturX.Pdf;
using International.EInvoicing.FacturX.PdfSharp;
using International.EInvoicing.Peppol;
using International.EInvoicing.Profiles;
using International.EInvoicing.Validation.Schematron;
using International.EInvoicing.Validation.Schematron.XPath;
using International.EInvoicing.Validation.XRechnung;

namespace International.EInvoicing.Cli;

/// <summary>
/// Assembling the library the way the command line asked for it.
/// </summary>
/// <remarks>
/// Everything shippable is on by default — EN 16931, XRechnung, the three country packages, Peppol's code
/// lists. What is missing is what may not be redistributed: the Peppol, Factur-X and national Schematron
/// artefacts. <c>--rules</c> points at those, and a report always names what did not run, so a document
/// judged by fewer rule sets than apply to it is never reported as passing.
/// </remarks>
internal static class Library
{
    public static EInvoicing Build(CommandLine command, TextWriter warnings)
    {
        IReadOnlyList<string> rulePaths = RulePaths(command);

        if (rulePaths.Count > 0 && !command.Has("quiet", "q"))
        {
            // Said out loud because it is the one thing about --rules that can mislead: an artefact carries
            // no statement of which profiles it governs, so one loaded this way judges every document in its
            // syntax, whatever that document declares.
            warnings.WriteLine(
                $"note: {rulePaths.Count} rule set(s) loaded from disk will judge every document in their "
                + "syntax — an artefact does not say which profiles it governs.");
        }

        EInvoicing library = EInvoicing.Create(
            einvoicing =>
            {
                einvoicing
                    .AddDefaults()
                    .AddPeppol()
                    .AddXRechnungRules()
                    .AddFrance()
                    .AddGermany()
                    .AddBelgium();

                if (command.Has("strict"))
                {
                    einvoicing.UseDiagnosticPreset(DiagnosticPreset.Strict);
                }

                if (command.Has("lenient"))
                {
                    einvoicing.UseDiagnosticPreset(DiagnosticPreset.Lenient);
                }

                foreach (string path in rulePaths)
                {
                    AddRuleFile(einvoicing, path, warnings);
                }
            },
            new PdfSharpAttachmentReader());

        return library;
    }

    /// <summary>
    /// Reads one rule set off disk, whichever of the two shapes it is published in.
    /// </summary>
    /// <remarks>
    /// Source Schematron and Schematron already compiled to XSLT are both what publishers ship — Peppol the
    /// first, Factur-X and several national authorities the second — so the tool works out which it is
    /// rather than making the caller say.
    /// </remarks>
    private static void AddRuleFile(EInvoicingBuilder builder, string path, TextWriter warnings)
    {
        string text = File.ReadAllText(path);
        string name = Path.GetFileNameWithoutExtension(path);
        DocumentSyntax syntax = SyntaxOf(text, name);

        try
        {
            builder.AddRules(
                syntax,
                IsCompiled(text)
                    ? CompiledSchematron.Read(text, name, "(from file)")
                    : SchematronRuleSet.Load(text, name, "(from file)"));
        }
        catch (XPathException failure)
        {
            warnings.WriteLine($"warning: {path} could not be read as a rule set — {failure.Message}");
        }
    }

    private static bool IsCompiled(string text) =>
        text.Contains("http://www.w3.org/1999/XSL/Transform", StringComparison.Ordinal);

    /// <summary>
    /// Which syntax a rule file is written against.
    /// </summary>
    /// <remarks>
    /// Guessed from the artefact itself — the CII namespace appears in a CII rule set and not in a UBL one —
    /// and from the file name as published, because both conventions are in wide use and neither is declared
    /// anywhere machine-readable.
    /// </remarks>
    private static DocumentSyntax SyntaxOf(string text, string name) =>
        text.Contains("urn:un:unece:uncefact:data:standard:CrossIndustryInvoice", StringComparison.Ordinal)
        || name.Contains("CII", StringComparison.OrdinalIgnoreCase)
            ? DocumentSyntax.Cii
            : DocumentSyntax.Ubl;

    private static IReadOnlyList<string> RulePaths(CommandLine command)
    {
        string? given = command.Value("rules");

        if (given is null)
        {
            return [];
        }

        if (Directory.Exists(given))
        {
            // Not recursive. A published artefact tree holds rule sets for several jurisdictions side by
            // side, and nothing in a Schematron file says which profiles it governs — so a recursive sweep
            // would hand a Peppol invoice to the United Arab Emirates rules and report their verdict as
            // though it meant something. Name the directory that holds the rules you want.
            SearchOption depth = command.Has("recurse")
                ? SearchOption.AllDirectories
                : SearchOption.TopDirectoryOnly;

            return
            [
                .. Directory
                    .EnumerateFiles(given, "*.*", depth)
                    .Where(IsRuleFile)
                    .Order(StringComparer.Ordinal),
            ];
        }

        return File.Exists(given) ? [given] : [];
    }

    private static bool IsRuleFile(string path) =>
        Path.GetExtension(path) is ".sch" or ".xsl" or ".xslt";
}
