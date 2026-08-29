using International.EInvoicing.Values;

namespace International.EInvoicing.Model;

/// <summary>The card an invoice was paid with (BG-18).</summary>
public sealed class PaymentCard : InvoiceNode
{
    /// <summary>
    /// BT-87 — the card number. The semantic model requires it to be masked to the last four digits: this
    /// field must never carry a full card number.
    /// </summary>
    public IdentifierField PrimaryAccountNumber { get; set; }

    /// <summary>BT-88 — the cardholder's name.</summary>
    public TextField HolderName { get; set; }
}
