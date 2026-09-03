namespace International.EInvoicing.Documents;

/// <summary>The invoice as a person reads it — for a hybrid invoice, the PDF the XML arrived in.</summary>
/// <remarks>
/// A rendition is the same invoice in a form a human opens, and is not a supporting document: a caller who
/// treats a delivery note as the invoice's readable copy has mixed up two different things. See
/// <see cref="SupportingDocument"/> for what an invoice carries <em>beside</em> itself.
/// </remarks>
/// <param name="Content">The bytes of the readable copy.</param>
/// <param name="MediaType">What it is, as an IANA media type.</param>
/// <param name="FileName">What it was called, when the caller read it from somewhere that has names.</param>
public sealed record InvoiceRendition(byte[] Content, string MediaType, string? FileName = null)
{
    /// <summary>Opens the content for reading. The caller disposes it.</summary>
    public Stream OpenRead() => new MemoryStream(Content, writable: false);

    /// <summary>Compares content, not references, so two renditions carrying the same bytes are equal.</summary>
    public bool Equals(InvoiceRendition? other) =>
        other is not null
        && MediaType == other.MediaType
        && FileName == other.FileName
        && Content.AsSpan().SequenceEqual(other.Content);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Content.Length, MediaType, FileName);
}
