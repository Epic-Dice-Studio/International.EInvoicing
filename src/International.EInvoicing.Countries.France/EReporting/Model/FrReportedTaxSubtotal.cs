using International.EInvoicing.Model;
using International.EInvoicing.Values;

namespace International.EInvoicing.Countries.France.EReporting.Model;

/// <summary>One line of the VAT breakdown: a rate, what it applies to, and what it comes to.</summary>
public sealed class FrReportedTaxSubtotal : InvoiceNode
{
    /// <summary>What the rate applies to.</summary>
    public AmountField TaxableAmount { get; set; }

    /// <summary>The VAT it comes to.</summary>
    public AmountField TaxAmount { get; set; }

    /// <summary>The VAT category — <c>S</c> standard, <c>E</c> exempt, and the rest.</summary>
    public CodeField CategoryCode { get; set; }

    /// <summary>The rate, as a percentage.</summary>
    public Field<decimal> Percent { get; set; }

    /// <summary>Why VAT is not charged. Required, with its code, when the category is exempt.</summary>
    public TextField ExemptionReason { get; set; }

    /// <summary>Why VAT is not charged, as a code from the VATEX list.</summary>
    public CodeField ExemptionReasonCode { get; set; }
}
