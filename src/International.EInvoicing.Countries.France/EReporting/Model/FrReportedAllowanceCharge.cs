using International.EInvoicing.Model;
using International.EInvoicing.Values;

namespace International.EInvoicing.Countries.France.EReporting.Model;

/// <summary>A discount or a charge, at document or line level.</summary>
public sealed class FrReportedAllowanceCharge : InvoiceNode
{
    /// <summary>Whether this is a charge. <c>false</c>, the default, makes it a discount.</summary>
    public IndicatorField IsCharge { get; set; }

    /// <summary>The amount, excluding VAT.</summary>
    public AmountField Amount { get; set; }

    /// <summary>The VAT category it falls under.</summary>
    public CodeField TaxCategoryCode { get; set; }

    /// <summary>The VAT rate applied to it.</summary>
    public Field<decimal> TaxPercent { get; set; }
}
