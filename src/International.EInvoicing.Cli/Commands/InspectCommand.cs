using International.EInvoicing.Diagnostics;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;

namespace International.EInvoicing.Cli.Commands;

/// <summary>
/// <c>einvoice inspect</c> — what is this document, and what did reading it report?
/// </summary>
/// <remarks>
/// The question that comes before validation: a file arrives, and nobody is quite sure what it is. This
/// answers with the syntax, the profile and how it was resolved, the business terms a person recognises, and
/// every diagnostic — including the ones that say a value survived only as raw text.
/// </remarks>
internal static class InspectCommand
{
    public static int Run(CommandLine command, TextWriter output, TextWriter errors)
    {
        IReadOnlyList<string> paths = Documents.Resolve(command.Operands);

        if (paths.Count == 0)
        {
            errors.WriteLine("error: nothing to inspect. Give a file or a directory.");
            return Exit.CouldNotRun;
        }

        EInvoicing library = Library.Build(command, errors);
        bool everythingRead = true;

        foreach (string path in paths)
        {
            SourceDocument? source = Documents.Open(path, errors);

            if (source is null)
            {
                everythingRead = false;
                continue;
            }

            output.WriteLine(path);

            DocumentResult result = library.Read(source.Bytes);

            output.WriteLine($"    kind         {result.Kind}");

            if (source.IsPdf)
            {
                output.WriteLine("    carried in   a hybrid PDF");
            }

            WriteProfile(result.Profile, output);

            if (result.Invoice is { } invoice)
            {
                WriteInvoice(invoice, output);
            }

            WriteDiagnostics(result.Diagnostics, output);

            everythingRead &= result.IsUsable;
        }

        return everythingRead ? Exit.Ok : Exit.DocumentRejected;
    }

    private static void WriteProfile(ProfileResolution? profile, TextWriter output)
    {
        if (profile is null)
        {
            output.WriteLine("    profile      (none declared)");
            return;
        }

        output.WriteLine($"    profile      {profile.Declared}");

        // How the profile resolved is the part that decides what will be validated, so it is not a detail.
        output.WriteLine(profile.IsExact
            ? "    resolved     exactly"
            : $"    resolved     to {profile.Profile?.Id.ToString() ?? "nothing"} — {profile.Outcome}");
    }

    private static void WriteInvoice(EInvoice invoice, TextWriter output)
    {
        output.WriteLine($"    number       {invoice.Number.Value ?? invoice.Number.Raw}");
        output.WriteLine($"    issued       {invoice.IssueDate.Value?.ToString("yyyy-MM-dd") ?? invoice.IssueDate.Raw}");
        output.WriteLine($"    currency     {invoice.CurrencyCode.Value}");
        output.WriteLine($"    seller       {invoice.Seller?.Name.Value}");
        output.WriteLine($"    buyer        {invoice.Buyer?.Name.Value}");
        output.WriteLine($"    lines        {invoice.Lines.Count}");
        output.WriteLine($"    due          {invoice.Totals.DuePayableAmount.Value}");

        int extensions = invoice.Extensions().Count();

        if (extensions > 0)
        {
            output.WriteLine($"    kept aside   {extensions} element(s) the model has no field for");
        }
    }

    private static void WriteDiagnostics(IReadOnlyList<Diagnostic> diagnostics, TextWriter output)
    {
        foreach (Diagnostic diagnostic in diagnostics.Where(d => d.Severity >= DiagnosticSeverity.Warning))
        {
            output.WriteLine($"    {diagnostic.Severity.ToString().ToLowerInvariant(),-12} {diagnostic.Code}  {diagnostic.Message}");
        }

        int informational = diagnostics.Count(d => d.Severity < DiagnosticSeverity.Warning);

        if (informational > 0)
        {
            output.WriteLine($"    info         {informational} more, at info level (use --all to see them)");
        }
    }
}
