namespace International.EInvoicing.Documents;

/// <summary>A document the invoice points at rather than carries — BG-24, located by BT-124.</summary>
/// <remarks>
/// Neither the invoice readable nor a document in hand: only an address. Fetching it is network I/O, which
/// this library does not do at all, so the caller decides whether that address is one they are willing to
/// open — an invoice arrives from a third party, and so does everything it points at.
/// </remarks>
/// <param name="Location">BT-124 — where the sender says the document can be retrieved.</param>
/// <param name="Identifier">BT-122 — the sender's identifier for it.</param>
/// <param name="Description">BT-123 — what the sender says it is.</param>
public sealed record SupportingDocumentLink(
    string Location,
    string? Identifier = null,
    string? Description = null);
