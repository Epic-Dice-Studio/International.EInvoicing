using International.EInvoicing.Cii.Writing;
using International.EInvoicing.FacturX.Pdf;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;

namespace International.EInvoicing.FacturX;

/// <summary>
/// Produces a Factur-X or ZUGFeRD invoice: the CII payload, and optionally the hybrid PDF carrying it.
/// </summary>
/// <remarks>
/// This library does not render PDFs. A hybrid invoice starts from the PDF you already produce for humans,
/// and this writer embeds the machine-readable half into it — which is also what keeps the two halves
/// agreeing, since both come from the same model.
/// </remarks>
public sealed class FacturXWriter
{
    private readonly CiiInvoiceWriter _cii;
    private readonly IPdfAttachmentWriter? _pdf;

    /// <summary>Creates a writer. Without <paramref name="pdf"/> only the CII payload can be produced.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="cii"/> is <c>null</c>.</exception>
    public FacturXWriter(CiiInvoiceWriter cii, IPdfAttachmentWriter? pdf = null)
    {
        ArgumentNullException.ThrowIfNull(cii);

        _cii = cii;
        _pdf = pdf;
    }

    /// <summary>Writes the CII payload alone, without a PDF container.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="invoice"/> is <c>null</c>.</exception>
    public FacturXAttachment WritePayload(EInvoice invoice)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        return new FacturXAttachment(
            FacturXAttachment.FacturXFileName,
            System.Text.Encoding.UTF8.GetBytes(_cii.WriteToString(invoice)));
    }

    /// <summary>
    /// Writes a hybrid invoice: <paramref name="humanReadablePdf"/> with the CII payload embedded and the
    /// Factur-X metadata declaring the profile the invoice claims.
    /// </summary>
    /// <param name="invoice">The invoice to write.</param>
    /// <param name="humanReadablePdf">The PDF a person will read. Left open.</param>
    /// <param name="destination">Where the hybrid invoice is written. Left open.</param>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">No PDF writer was supplied.</exception>
    public void Write(EInvoice invoice, Stream humanReadablePdf, Stream destination)
    {
        ArgumentNullException.ThrowIfNull(invoice);
        ArgumentNullException.ThrowIfNull(humanReadablePdf);
        ArgumentNullException.ThrowIfNull(destination);

        if (_pdf is null)
        {
            throw new InvalidOperationException(
                "Writing a hybrid invoice needs an IPdfAttachmentWriter. Reference "
                + "International.EInvoicing.FacturX.PdfSharp, or supply your own implementation.");
        }

        Profile profile = KnownProfiles.Find(invoice.SpecificationIdentifier, DocumentSyntax.Cii)
            ?? FacturXProfiles.En16931;

        _pdf.Attach(humanReadablePdf, WritePayload(invoice), profile, destination);
    }
}
