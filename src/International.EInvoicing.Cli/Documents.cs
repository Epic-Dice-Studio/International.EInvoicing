using International.EInvoicing.FacturX.Pdf;
using International.EInvoicing.FacturX.PdfSharp;

namespace International.EInvoicing.Cli;

/// <summary>What the tool was pointed at.</summary>
/// <param name="Path">Where it came from.</param>
/// <param name="Bytes">Its content, as it is on disk.</param>
internal sealed record SourceDocument(string Path, byte[] Bytes)
{
    /// <summary>Whether it is a hybrid PDF rather than XML.</summary>
    public bool IsPdf => FacturX.FacturXReader.LooksLikePdf(Bytes);

    /// <summary>
    /// The XML to judge: the file itself, or the payload a hybrid PDF carries.
    /// </summary>
    /// <remarks>
    /// The payload is taken out of the PDF as it was embedded, not re-serialised from the model. Validating a
    /// document this tool wrote itself would only prove that this tool is self-consistent.
    /// </remarks>
    public string? Xml()
    {
        if (!IsPdf)
        {
            return System.Text.Encoding.UTF8.GetString(Bytes);
        }

        using var stream = new MemoryStream(Bytes);
        FacturXAttachment? attachment = new PdfSharpAttachmentReader()
            .FindAttachment(stream, FacturXAttachment.KnownFileNames, MaximumAttachmentBytes);

        return attachment is null ? null : System.Text.Encoding.UTF8.GetString(attachment.Xml);
    }

    private const long MaximumAttachmentBytes = 32 * 1024 * 1024;
}

/// <summary>Turning what the caller typed into files to work on.</summary>
internal static class Documents
{
    private static readonly string[] Extensions = [".xml", ".pdf"];

    /// <summary>
    /// Every file the operands name: the files themselves, and the documents inside any directory given.
    /// </summary>
    /// <remarks>
    /// Directories are walked because the question a validator is usually asked is "is this batch good", and
    /// making the caller assemble a file list in the shell is the sort of small friction that ends in nobody
    /// running the validator at all.
    /// </remarks>
    public static IReadOnlyList<string> Resolve(IReadOnlyList<string> operands)
    {
        List<string> paths = [];

        foreach (string operand in operands)
        {
            if (Directory.Exists(operand))
            {
                paths.AddRange(Directory
                    .EnumerateFiles(operand, "*.*", SearchOption.AllDirectories)
                    .Where(path => Extensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
                    .Order(StringComparer.Ordinal));
                continue;
            }

            paths.Add(operand);
        }

        return paths;
    }

    /// <summary>Reads one, or <c>null</c> when there is nothing there.</summary>
    public static SourceDocument? Open(string path, TextWriter errors)
    {
        if (!File.Exists(path))
        {
            errors.WriteLine($"error: no file at '{path}'.");
            return null;
        }

        return new SourceDocument(path, File.ReadAllBytes(path));
    }
}
