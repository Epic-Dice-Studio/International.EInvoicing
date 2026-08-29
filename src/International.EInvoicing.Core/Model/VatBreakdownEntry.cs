using International.EInvoicing.Values;

namespace International.EInvoicing.Model;

/// <summary>
/// VAT for one category and rate (BG-23). The breakdown is where the arithmetic rules bite: BR-CO-14 ties
/// these amounts to the document totals, and BR-S / BR-Z / BR-E / BR-AE constrain each category.
/// </summary>
public sealed class VatBreakdownEntry : InvoiceNode
{
    /// <summary>BT-116 — the amount VAT is calculated on.</summary>
    public AmountField TaxableAmount { get; set; }

    /// <summary>BT-117 — the VAT amount for this category and rate.</summary>
    public AmountField TaxAmount { get; set; }

    /// <summary>BT-118 — VAT category code (UNTDID 5305).</summary>
    public CodeField CategoryCode { get; set; }

    /// <summary>BT-119 — VAT rate, as a percentage.</summary>
    public Field<decimal> Rate { get; set; }

    /// <summary>BT-120 — why VAT is not charged, in words.</summary>
    public TextField ExemptionReason { get; set; }

    /// <summary>BT-121 — why VAT is not charged, as a code.</summary>
    public CodeField ExemptionReasonCode { get; set; }
}
