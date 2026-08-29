using International.EInvoicing.Profiles;

namespace International.EInvoicing.FacturX.Pdf;

/// <summary>
/// Produces a hybrid invoice by attaching the payload to a PDF. Implemented by whichever PDF library you
/// choose to bring.
/// </summary>
public interface IPdfAttachmentWriter
{
    /// <summary>
    /// Writes <paramref name="source"/> to <paramref name="destination"/> with <paramref name="attachment"/>
    /// embedded and the Factur-X XMP metadata describing <paramref name="profile"/>.
    /// </summary>
    /// <param name="source">The PDF to start from. Left open.</param>
    /// <param name="attachment">The payload to embed.</param>
    /// <param name="profile">The profile the payload conforms to, which the metadata must declare.</param>
    /// <param name="destination">Where the hybrid invoice is written. Left open.</param>
    void Attach(Stream source, FacturXAttachment attachment, Profile profile, Stream destination);
}
