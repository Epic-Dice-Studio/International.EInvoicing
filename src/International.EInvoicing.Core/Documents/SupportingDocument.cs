namespace International.EInvoicing.Documents;

/// <summary>A document the invoice carries beside itself — BG-24, attached as BT-125.</summary>
/// <remarks>
/// Something else, attached: a timesheet, a delivery note, a contract. It is not the invoice in readable
/// form, which is <see cref="InvoiceRendition"/>.
/// </remarks>
/// <param name="Content">The decoded bytes.</param>
/// <param name="MediaType">BT-125-1 — what it is, when the sender said. <c>null</c> when they did not.</param>
/// <param name="FileName">BT-125-2 — what it is called, when the sender said.</param>
/// <param name="Identifier">BT-122 — the sender's identifier for it.</param>
/// <param name="Description">BT-123 — what the sender says it is.</param>
public sealed record SupportingDocument(
    byte[] Content,
    string? MediaType = null,
    string? FileName = null,
    string? Identifier = null,
    string? Description = null)
{
    /// <summary>Opens the content for reading. The caller disposes it.</summary>
    public Stream OpenRead() => new MemoryStream(Content, writable: false);

    /// <summary>Compares content, not references, so two documents carrying the same bytes are equal.</summary>
    public bool Equals(SupportingDocument? other) =>
        other is not null
        && MediaType == other.MediaType
        && FileName == other.FileName
        && Identifier == other.Identifier
        && Description == other.Description
        && Content.AsSpan().SequenceEqual(other.Content);

    /// <inheritdoc />
    public override int GetHashCode() =>
        HashCode.Combine(Content.Length, MediaType, FileName, Identifier, Description);
}
