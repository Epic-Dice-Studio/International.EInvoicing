using International.EInvoicing.Values;

namespace International.EInvoicing.Model;

/// <summary>A reference to another document (BG-3), such as the invoice this one corrects.</summary>
public sealed class DocumentReference : InvoiceNode
{
    /// <summary>BT-25 — the referenced document's identifier.</summary>
    public IdentifierField Identifier { get; set; }

    /// <summary>BT-26 — the date the referenced document was issued.</summary>
    public DateField IssueDate { get; set; }
}
