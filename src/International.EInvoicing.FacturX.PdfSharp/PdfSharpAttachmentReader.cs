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
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    public FacturXAttachment? FindAttachment(Stream pdf, IReadOnlyList<string> fileNames, long maximumBytes)
    {
        ArgumentNullException.ThrowIfNull(pdf);
        ArgumentNullException.ThrowIfNull(fileNames);

        PdfDocument document;
        try
        {
            document = PdfReader.Open(pdf, PdfDocumentOpenMode.Import);
        }
        catch (PdfReaderException)
        {
            return null;
        }

        using (document)
        {
            foreach (string fileName in fileNames)
            {
                if (Find(document, fileName, maximumBytes) is { } attachment)
                {
                    return attachment;
                }
            }
        }

        return null;
    }

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
