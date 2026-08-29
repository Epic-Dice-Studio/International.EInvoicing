using International.EInvoicing.Values;

namespace International.EInvoicing.Model;

/// <summary>An account a credit transfer may be made to (BG-17).</summary>
public sealed class CreditTransfer : InvoiceNode
{
    /// <summary>BT-84 — the account identifier, usually an IBAN.</summary>
    public IdentifierField AccountIdentifier { get; set; }

    /// <summary>BT-85 — the name the account is held under.</summary>
    public TextField AccountName { get; set; }

    /// <summary>BT-86 — the servicing bank's identifier, usually a BIC.</summary>
    public IdentifierField ServiceProviderIdentifier { get; set; }
}
