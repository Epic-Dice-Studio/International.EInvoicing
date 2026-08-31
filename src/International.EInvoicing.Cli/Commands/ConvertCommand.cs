namespace International.EInvoicing.Cli.Commands;

/// <summary>
/// <c>einvoice convert</c> — carry a document to the other syntax, and say what it cost.
/// </summary>
/// <remarks>
/// A recipient must accept whatever arrives; converting is the integration. The loss report is printed to
/// standard error rather than folded into the document, so <c>einvoice convert in.xml --to cii &gt; out.xml</c>
/// does the obvious thing and still tells the person watching what did not cross.
/// </remarks>
internal static class ConvertCommand
{
    public static int Run(CommandLine command, TextWriter output, TextWriter errors)
    {
        if (command.Operands.Count != 1)
        {
            errors.WriteLine("error: convert takes exactly one document.");
            return Exit.CouldNotRun;
        }

        string? target = command.Value("to");

        if (target is null)
        {
            errors.WriteLine("error: say which syntax to convert to, with --to ubl or --to cii.");
            return Exit.CouldNotRun;
        }

        if (!TryParseFormat(target, out DocumentFormat format))
        {
            errors.WriteLine($"error: '{target}' is not a syntax this tool writes. Use ubl or cii.");
            return Exit.CouldNotRun;
        }

        SourceDocument? source = Documents.Open(command.Operands[0], errors);

        if (source is null)
        {
            return Exit.CouldNotRun;
        }

        string? xml = source.Xml();

        if (xml is null)
        {
            errors.WriteLine($"error: {source.Path} carries no invoice payload.");
            return Exit.CouldNotRun;
        }

        EInvoicing library = Library.Build(command, errors);
        ConversionResult result = library.Convert(xml, format);

        if (result.Xml.Length == 0)
        {
            errors.WriteLine($"error: {source.Path} could not be read as an invoice.");

            foreach (Diagnostics.Diagnostic diagnostic in result.Diagnostics.Take(10))
            {
                errors.WriteLine($"    {diagnostic.Code}  {diagnostic.Message}");
            }

            return Exit.DocumentRejected;
        }

        if (command.Value("out") is { } destination)
        {
            File.WriteAllText(destination, result.Xml);
            errors.WriteLine($"written to {destination}");
        }
        else
        {
            output.WriteLine(result.Xml);
        }

        foreach (ConversionLoss loss in result.Losses)
        {
            errors.WriteLine($"lost: {loss}");
        }

        errors.WriteLine(result.IsLossless
            ? "the conversion carried everything the source had."
            : $"{result.Losses.Count} thing(s) did not cross. Validate the result before sending it.");

        return Exit.Ok;
    }

    private static bool TryParseFormat(string value, out DocumentFormat format)
    {
        switch (value.ToLowerInvariant())
        {
            case "ubl":
                format = DocumentFormat.Ubl;
                return true;
            case "cii":
                format = DocumentFormat.Cii;
                return true;
            default:
                format = default;
                return false;
        }
    }
}
