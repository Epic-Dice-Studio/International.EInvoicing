using International.EInvoicing.Values;

namespace International.EInvoicing.Model;

/// <summary>A direct debit arrangement (BG-19).</summary>
public sealed class DirectDebit : InvoiceNode
{
    /// <summary>BT-89 — the mandate reference the debit is made under.</summary>
    public IdentifierField MandateReference { get; set; }

    /// <summary>BT-90 — the creditor identifier.</summary>
    public IdentifierField CreditorIdentifier { get; set; }

    /// <summary>BT-91 — the account to be debited.</summary>
    public IdentifierField DebitedAccountIdentifier { get; set; }
}
