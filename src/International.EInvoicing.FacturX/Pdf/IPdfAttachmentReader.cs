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
    FacturXAttachment? FindAttachment(Stream pdf, IReadOnlyList<string> fileNames, long maximumBytes);
}
