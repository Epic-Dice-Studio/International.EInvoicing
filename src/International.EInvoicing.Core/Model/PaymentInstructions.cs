using International.EInvoicing.Values;

namespace International.EInvoicing.Model;

/// <summary>How the invoice is to be paid (BG-16).</summary>
public sealed class PaymentInstructions : InvoiceNode
{
    /// <summary>BT-81 — payment means type code (UNTDID 4461).</summary>
    public CodeField MeansTypeCode { get; set; }

    /// <summary>BT-82 — payment means in words.</summary>
    public TextField MeansText { get; set; }

    /// <summary>
    /// BT-83 — the reference the payer must quote so the payment can be reconciled. Several countries define
    /// a structured form for it, such as the Belgian <c>+++nnn/nnnn/nnnnn+++</c>.
    /// </summary>
    public TextField RemittanceInformation { get; set; }

    /// <summary>BG-17 — the accounts a credit transfer may be made to.</summary>
    public List<CreditTransfer> CreditTransfers { get; } = [];

    /// <summary>BG-18 — the payment card used.</summary>
    public PaymentCard? Card { get; set; }

    /// <summary>BG-19 — the direct debit arrangement.</summary>
    public DirectDebit? DirectDebit { get; set; }
}
