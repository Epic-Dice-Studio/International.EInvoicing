using International.EInvoicing.Model;
using International.EInvoicing.Values;

namespace International.EInvoicing.Countries.France.EReporting.Model;

/// <summary>
/// What an item cost. When all three are given, the net price must be the gross price less the discount.
/// </summary>
public sealed class FrReportedPrice : InvoiceNode
{
    /// <summary>The net price of the item.</summary>
    public AmountField NetAmount { get; set; }

    /// <summary>The discount on the item price.</summary>
    public AmountField DiscountAmount { get; set; }

    /// <summary>The gross price of the item.</summary>
    public AmountField GrossAmount { get; set; }
}
