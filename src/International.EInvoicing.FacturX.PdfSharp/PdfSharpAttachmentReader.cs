using International.EInvoicing.FacturX.Pdf;
using PdfSharp.Pdf;
using PdfSharp.Pdf.Advanced;
using PdfSharp.Pdf.IO;

namespace International.EInvoicing.FacturX.PdfSharp;

/// <summary>
/// Finds the invoice payload inside a PDF, using PDFsharp.
/// </summary>
/// <remarks>
/// An incoming PDF is treated as hostile: nothing in it is executed, no external reference is followed, and
/// an attachment larger than the caller's limit is refused rather than decoded.
/// </remarks>
public sealed class PdfSharpAttachmentReader : IPdfAttachmentReader
{
    /// <inheritdoc />
    /// <remarks>
    /// A PDF that arrives is hostile input, and this answers <c>null</c> for every way one can be unusable —
    /// not a PDF at all, truncated, encrypted, or structurally broken somewhere inside the tables this walks.
    /// PDFsharp signals those with whatever exception the failure happens to reach first, and a reader that
    /// let one out would break the promise the rest of this library keeps: reading never throws on a document
    /// somebody else wrote.
    /// </remarks>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    public FacturXAttachment? FindAttachment(Stream pdf, IReadOnlyList<string> fileNames, long maximumBytes)
    {
        ArgumentNullException.ThrowIfNull(pdf);
        ArgumentNullException.ThrowIfNull(fileNames);

        try
        {
            using PdfDocument document = PdfReader.Open(pdf, PdfDocumentOpenMode.Import);

            foreach (string fileName in fileNames)
            {
                if (Find(document, fileName, maximumBytes) is { } attachment)
                {
                    return attachment;
                }
            }

            return null;
        }
        catch (Exception exception) when (IsMalformedDocument(exception))
        {
            return null;
        }
    }

    /// <summary>
    /// Whether an exception is the PDF's fault rather than the caller's or the machine's.
    /// </summary>
    /// <remarks>
    /// PDFsharp raises <see cref="PdfReaderException"/> for what it recognises as a bad document, and
    /// whatever the code happened to hit for what it does not: a cast on a dictionary that holds something
    /// else, an index into a table that ends early, a null where the structure promised an object. All of
    /// those mean the same thing to a caller — there is no invoice in this file. What must still escape is
    /// anything that says the process is in trouble, or that the caller asked to stop.
    /// </remarks>
    private static bool IsMalformedDocument(Exception exception) =>
        exception is not OutOfMemoryException and not OperationCanceledException and not StackOverflowException;

    private static FacturXAttachment? Find(PdfDocument document, string fileName, long maximumBytes)
    {
        foreach (PdfDictionary specification in EmbeddedFileSpecifications(document))
        {
            string? name = specification.Elements.GetString("/F") is { Length: > 0 } value
                ? value
                : specification.Elements.GetString("/UF");

            if (!string.Equals(name, fileName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            byte[]? content = ContentOf(specification, maximumBytes);
            if (content is not null)
            {
                return new FacturXAttachment(
                    fileName,
                    content,
                    specification.Elements.GetString("/AFRelationship").TrimStart('/'));
            }
        }

        return null;
    }

    /// <summary>
    /// Walks the two places a PDF files its attachments: the document-level associated files array, and the
    /// EmbeddedFiles name tree. Real invoices use both.
    /// </summary>
    private static IEnumerable<PdfDictionary> EmbeddedFileSpecifications(PdfDocument document)
    {
        foreach (PdfDictionary specification in AssociatedFiles(document.Internals.Catalog))
        {
            yield return specification;
        }

        foreach (PdfDictionary specification in EmbeddedFilesNameTree(document.Internals.Catalog))
        {
            yield return specification;
        }
    }

    private static IEnumerable<PdfDictionary> AssociatedFiles(PdfDictionary catalog)
    {
        if (catalog.Elements.GetArray("/AF") is not { } associated)
        {
            yield break;
        }

        for (int index = 0; index < associated.Elements.Count; index++)
        {
            if (Resolve(associated.Elements[index]) is { } specification)
            {
                yield return specification;
            }
        }
    }

    private static IEnumerable<PdfDictionary> EmbeddedFilesNameTree(PdfDictionary catalog)
    {
        PdfDictionary? names = Resolve(catalog.Elements["/Names"]);
        PdfDictionary? embedded = names is null ? null : Resolve(names.Elements["/EmbeddedFiles"]);
        if (embedded?.Elements.GetArray("/Names") is not { } pairs)
        {
            yield break;
        }

        // The name tree alternates name, file specification, name, file specification.
        for (int index = 1; index < pairs.Elements.Count; index += 2)
        {
            if (Resolve(pairs.Elements[index]) is { } specification)
            {
                yield return specification;
            }
        }
    }

    private static byte[]? ContentOf(PdfDictionary specification, long maximumBytes)
    {
        PdfDictionary? embeddedFiles = Resolve(specification.Elements["/EF"]);
        if (Resolve(embeddedFiles?.Elements["/F"]) is not { Stream: not null } file)
        {
            return null;
        }

        byte[] content = file.Stream.UnfilteredValue;
        return content.LongLength > maximumBytes ? null : content;
    }

    private static PdfDictionary? Resolve(PdfItem? item) => item switch
    {
        PdfReference reference => reference.Value as PdfDictionary,
        PdfDictionary dictionary => dictionary,
        _ => null,
    };
}
