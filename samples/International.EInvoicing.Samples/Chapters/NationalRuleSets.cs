using International.EInvoicing.Countries.Germany;
using International.EInvoicing.Profiles;
using International.EInvoicing.Validation;
using International.EInvoicing.Validation.Schematron;
using International.EInvoicing.Validation.XRechnung;

namespace International.EInvoicing.Samples.Chapters;

/// <summary>
/// The rules a country adds on top, including the ones this library may not ship.
/// </summary>
/// <remarks>
/// EN 16931 and XRechnung are packaged: their artefacts are published under licences that allow it. The
/// Peppol and French ones declare no licence at all, so they are fetched rather than redistributed — and
/// then they are ordinary rule sets, added with one call.
/// </remarks>
internal static class NationalRuleSets
{
    public static void Run()
    {
        Report.Chapter("National rule sets, shipped and fetched");

        Shipped();
        Fetched();
    }

    /// <summary>What comes in the box.</summary>
    private static void Shipped()
    {
        EInvoicing einvoicing = EInvoicing.Create(library => library
            .AddDefaults()          // EN 16931, for every UBL and CII document
            .AddGermany()
            .AddXRechnungRules());  // and the German rules, for documents that declare XRechnung

        Report.Fact("rule sets available", einvoicing.RuleSets.Count);
        Report.Say("Asked about two UBL documents — would this rule set run?");

        foreach (IDocumentRuleSet ruleSet in einvoicing.RuleSets)
        {
            bool german = ruleSet.AppliesTo(DocumentSyntax.Ubl, KnownProfiles.XRechnung3CiusUbl.Id);
            bool plain = ruleSet.AppliesTo(DocumentSyntax.Ubl, KnownProfiles.En16931Ubl.Id);

            Report.Note(
                $"{ruleSet.Name,-24} one declaring XRechnung: {(german ? "yes" : "no ")}"
                + $"   one declaring plain EN 16931: {(plain ? "yes" : "no")}");
        }

        Report.Say("Each rule set decides for itself whether it governs the document in front of it.");
    }

    /// <summary>And what a caller fetches for themselves.</summary>
    private static void Fetched()
    {
        string? peppol = Find(Path.Combine("specs", "peppol", "rules", "PEPPOL-EN16931-UBL.sch"));
        string? french = Find(Path.Combine("specs", "fr-dse", "rules"), "*CDAR*.sch");

        if (peppol is null && french is null)
        {
            Report.Say("No fetched artefacts here. `build/fetch-specs.sh peppol france` brings them, and then:");
            Report.Note(".AddRulesFromFile(DocumentSyntax.Ubl, \"PEPPOL-EN16931-UBL.sch\", \"Peppol BIS Billing\", \"3.0\")");
            return;
        }

        EInvoicing einvoicing = EInvoicing.Create(library =>
        {
            library.AddDefaults();

            if (peppol is not null)
            {
                library.AddRulesFromFile(DocumentSyntax.Ubl, peppol, "Peppol BIS Billing 3.0", "3.0");
            }

            if (french is not null)
            {
                library.AddRulesFromFile(DocumentSyntax.Cdar, french, "BR-FR-CDV (CDAR)", "1.4.0.03");
            }
        });

        foreach (IDocumentRuleSet ruleSet in einvoicing.RuleSets)
        {
            Report.Note($"{ruleSet.Name} {ruleSet.Version}");
        }

        Report.Say("Fetched once, they are ordinary rule sets. Nothing in the library had to change to take them.");
    }

    /// <summary>Looks for an artefact from the repository root, wherever the sample was started from.</summary>
    private static string? Find(string relativePath, string? pattern = null)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, ".git")))
        {
            directory = directory.Parent;
        }

        if (directory is null)
        {
            return null;
        }

        string candidate = Path.Combine(directory.FullName, relativePath);

        if (pattern is null)
        {
            return File.Exists(candidate) ? candidate : null;
        }

        return Directory.Exists(candidate)
            ? Directory.EnumerateFiles(candidate, pattern, SearchOption.AllDirectories).FirstOrDefault()
            : null;
    }
}
