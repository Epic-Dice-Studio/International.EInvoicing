namespace International.EInvoicing.FacturX.Pdf;

/// <summary>
/// Finds the invoice payload inside a PDF. Implemented by whichever PDF library you choose to bring.
/// </summary>
/// <remarks>
/// An incoming PDF is hostile input: an implementation must not execute embedded JavaScript, must not follow
/// external references, and must bound the size of what it extracts.
/// </remarks>
public interface IPdfAttachmentReader
{
    /// <summary>
    /// Returns the first attachment whose name matches one of <paramref name="fileNames"/>, or <c>null</c>
    /// when the PDF carries none of them.
    /// </summary>
    /// <param name="pdf">The PDF to read. Left open.</param>
    /// <param name="fileNames">The names to look for, in order of preference.</param>
    /// <param name="maximumBytes">The largest attachment to decode. Larger ones must be refused, not truncated.</param>
    /// <remarks>
    /// <b>An implementation must not throw for anything the PDF is or is not.</b> Not a PDF, truncated,
    /// encrypted, structurally broken, no attachment, an attachment over the limit — all of those are
    /// <c>null</c>, because the caller's next move is the same in every case and because reading a document
    /// somebody else wrote never throws anywhere else in this library. Argument checks still throw: a
    /// <c>null</c> stream is the caller's mistake, not the document's.
    /// </remarks>
    FacturXAttachment? FindAttachment(Stream pdf, IReadOnlyList<string> fileNames, long maximumBytes);
}
