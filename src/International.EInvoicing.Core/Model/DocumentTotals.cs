using International.EInvoicing.Values;

namespace International.EInvoicing.Model;

/// <summary>
/// What the invoice adds up to (BG-22). Every amount here is constrained by a BR-CO rule against the lines
/// and the VAT breakdown, which is where implementations most often disagree with validators.
/// </summary>
public sealed class DocumentTotals : InvoiceNode
{
    /// <summary>BT-106 — sum of the line net amounts.</summary>
    public AmountField LineTotalAmount { get; set; }

    /// <summary>BT-107 — sum of document level allowances.</summary>
    public AmountField AllowanceTotalAmount { get; set; }

    /// <summary>BT-108 — sum of document level charges.</summary>
    public AmountField ChargeTotalAmount { get; set; }

    /// <summary>BT-109 — total amount without VAT.</summary>
    public AmountField TaxExclusiveAmount { get; set; }

    /// <summary>BT-110 — total VAT amount.</summary>
    public AmountField TaxAmount { get; set; }

    /// <summary>BT-111 — total VAT amount in the accounting currency (BT-6).</summary>
    public AmountField TaxAmountInAccountingCurrency { get; set; }

    /// <summary>BT-112 — total amount with VAT.</summary>
    public AmountField TaxInclusiveAmount { get; set; }

    /// <summary>BT-113 — amount already paid.</summary>
    public AmountField PrepaidAmount { get; set; }

    /// <summary>BT-114 — rounding applied to the amount due.</summary>
    public AmountField RoundingAmount { get; set; }

    /// <summary>BT-115 — the amount due for payment.</summary>
    public AmountField DuePayableAmount { get; set; }
}
