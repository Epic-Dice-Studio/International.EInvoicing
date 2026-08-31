using System.Text.Json;
using International.EInvoicing.Diagnostics;
using International.EInvoicing.Profiles;
using International.EInvoicing.Validation;

namespace International.EInvoicing.Cli.Commands;

/// <summary>
/// <c>einvoice validate</c> — the reason this tool exists.
/// </summary>
/// <remarks>
/// The reference validator in this space is a Java jar. This is the .NET one, and it holds itself to the
/// same standard the library does: a report says what was <em>checked</em>, not only what failed. A document
/// nothing was registered to judge is reported as unchecked and exits non-zero, because a validator that
/// says "valid" when it means "I had no rules for this" is worse than no validator.
/// </remarks>
internal static class ValidateCommand
{
    public static int Run(CommandLine command, TextWriter output, TextWriter errors)
    {
        IReadOnlyList<string> paths = Documents.Resolve(command.Operands);

        if (paths.Count == 0)
        {
            errors.WriteLine("error: nothing to validate. Give a file or a directory.");
            return Exit.CouldNotRun;
        }

        EInvoicing library = Library.Build(command, errors);
        bool json = command.Has("json");
        bool quiet = command.Has("quiet", "q");

        List<(string Path, ValidationReport? Report, string? Note)> results = [];

        foreach (string path in paths)
        {
            SourceDocument? document = Documents.Open(path, errors);

            if (document is null)
            {
                results.Add((path, null, "no such file"));
                continue;
            }

            string? xml = document.Xml();

            if (xml is null)
            {
                results.Add((path, null, "the PDF carries no invoice payload"));
                continue;
            }

            ValidationReport report = library.Validate(xml);
            results.Add((path, report, Specialisation(library, xml, report)));
        }

        if (json)
        {
            WriteJson(results, output);
        }
        else
        {
            WriteText(results, output, quiet);
        }

        // A file that is not there, or a PDF with no payload, is the tool failing to do the job — not a
        // document that was judged and found wanting. A pipeline that cannot tell the two apart passes while
        // checking nothing.
        if (results.Any(result => result.Report is null))
        {
            return Exit.CouldNotRun;
        }

        return results.All(result => result.Report!.IsConforming) ? Exit.Ok : Exit.DocumentRejected;
    }

    private static void WriteText(
        List<(string Path, ValidationReport? Report, string? Note)> results,
        TextWriter output,
        bool quiet)
    {
        foreach ((string path, ValidationReport? report, string? note) in results)
        {
            if (report is null)
            {
                output.WriteLine($"{Verdict.CouldNotRead} {path} — {note}");
                continue;
            }

            output.WriteLine($"{VerdictOf(report)} {path}");

            if (note is not null)
            {
                output.WriteLine($"    note         {note}");
            }

            foreach (ValidationMessage message in report.OfAtLeast(RuleSeverity.Warning))
            {
                output.WriteLine($"    {Label(message.Severity)} {message.RuleIdentifier}  {message.Message}");

                if (message.Location is { Length: > 0 } location)
                {
                    output.WriteLine($"           at {location}");
                }
            }

            // What ran matters as much as what failed: a document checked against fewer rule sets than apply
            // to it is unchecked, not valid.
            foreach (RuleSetOutcome ruleSet in report.NotRun)
            {
                output.WriteLine($"    not checked  {ruleSet.Name} {ruleSet.Version} — {ruleSet.SkippedBecause}");
            }

            if (!quiet)
            {
                foreach (RuleSetOutcome ruleSet in report.RuleSets.Where(outcome => outcome.Ran))
                {
                    output.WriteLine($"    checked      {ruleSet.Name} {ruleSet.Version}");
                }
            }
        }

        int conforming = results.Count(result => result.Report?.IsConforming == true);
        output.WriteLine();
        output.WriteLine($"{conforming}/{results.Count} conforming.");
    }

    private static void WriteJson(
        List<(string Path, ValidationReport? Report, string? Note)> results,
        TextWriter output)
    {
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartArray();

            foreach ((string path, ValidationReport? report, string? note) in results)
            {
                writer.WriteStartObject();
                writer.WriteString("file", path);

                if (report is null)
                {
                    writer.WriteString("verdict", "could-not-read");
                    writer.WriteString("reason", note);
                    writer.WriteEndObject();
                    continue;
                }

                writer.WriteString("verdict", VerdictOf(report));
                writer.WriteString("note", note);
                writer.WriteBoolean("valid", report.IsValid);
                writer.WriteBoolean("complete", report.IsComplete);

                writer.WriteStartArray("messages");

                foreach (ValidationMessage message in report.OfAtLeast(RuleSeverity.Warning))
                {
                    writer.WriteStartObject();
                    writer.WriteString("rule", message.RuleIdentifier);
                    writer.WriteString("severity", message.Severity.ToString());
                    writer.WriteString("message", message.Message);
                    writer.WriteString("location", message.Location);
                    writer.WriteString("businessTerm", message.BusinessTerm);
                    writer.WriteString("ruleSet", message.RuleSet);
                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
                writer.WriteStartArray("ruleSets");

                foreach (RuleSetOutcome ruleSet in report.RuleSets)
                {
                    writer.WriteStartObject();
                    writer.WriteString("name", ruleSet.Name);
                    writer.WriteString("version", ruleSet.Version);
                    writer.WriteBoolean("ran", ruleSet.Ran);
                    writer.WriteString("skippedBecause", ruleSet.SkippedBecause);
                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }

        output.WriteLine(System.Text.Encoding.UTF8.GetString(buffer.ToArray()));
    }

    /// <summary>
    /// Says so when a document declares a specialisation of EN 16931 and only one rule set judged it.
    /// </summary>
    /// <remarks>
    /// A CIUS or an extension carries rules of its own, and most of those artefacts may not be redistributed,
    /// so they are absent until <c>--rules</c> brings them in. Nothing in a report would otherwise say that:
    /// every rule set registered ran, so <c>IsComplete</c> is true, and the document reads as fully checked
    /// when it has only been checked against the base. This states what was observed — the declared
    /// identifier, and that only one rule set ran — and leaves the conclusion to the reader.
    /// </remarks>
    private static string? Specialisation(EInvoicing library, string xml, ValidationReport report)
    {
        if (report.RuleSets.Count(outcome => outcome.Ran) > 1)
        {
            // Something beyond the base judged it, which is what a registered specialisation looks like.
            return null;
        }

        ProfileIdentifier declared = library.Read(xml).Profile?.Declared ?? default;

        bool isSpecialisation = declared.IsDeclared
            && (declared.Value.Contains("#compliant#", StringComparison.Ordinal)
                || declared.Value.Contains("#conformant#", StringComparison.Ordinal));

        return isSpecialisation
            ? $"declares {declared}, a specialisation of EN 16931, and only the base judged it. Its own "
                + "rules are published separately — see build/fetch-specs.sh, then --rules."
            : null;
    }

    private static string VerdictOf(ValidationReport report) => report switch
    {
        { IsConforming: true } => Verdict.Conforming,
        { IsValid: true } => Verdict.Unchecked,
        _ => Verdict.Rejected,
    };

    private static string Label(RuleSeverity severity) => severity switch
    {
        RuleSeverity.Error => "error       ",
        RuleSeverity.Warning => "warning     ",
        _ => "info        ",
    };

    private static class Verdict
    {
        public const string Conforming = "conforming  ";
        public const string Rejected = "rejected    ";
        public const string Unchecked = "unchecked   ";
        public const string CouldNotRead = "unreadable  ";
    }
}
