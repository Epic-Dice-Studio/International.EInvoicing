using International.EInvoicing.Model;
using International.EInvoicing.Values;

namespace International.EInvoicing.Countries.France.EReporting.Model;

/// <summary>One rate's share of a day's transactions.</summary>
public sealed class FrTransactionTaxSubtotal : InvoiceNode
{
    /// <summary>The rate, as a percentage.</summary>
    public Field<decimal> Percent { get; set; }

    /// <summary>What the rate applies to.</summary>
    public AmountField TaxableAmount { get; set; }

    /// <summary>The VAT it comes to.</summary>
    public AmountField TaxAmount { get; set; }
}
