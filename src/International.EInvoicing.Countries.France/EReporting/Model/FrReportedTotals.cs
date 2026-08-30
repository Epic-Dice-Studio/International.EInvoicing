using International.EInvoicing.Model;
using International.EInvoicing.Values;

namespace International.EInvoicing.Countries.France.EReporting.Model;

/// <summary>
/// The invoice totals. In euro they must match the VAT breakdown, to within a cent per line of it.
/// </summary>
public sealed class FrReportedTotals : InvoiceNode
{
    /// <summary>The total excluding VAT.</summary>
    public AmountField TaxExclusiveAmount { get; set; }

    /// <summary>The total VAT.</summary>
    public AmountField TaxAmount { get; set; }
}
