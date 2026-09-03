using International.EInvoicing.Values;

namespace International.EInvoicing.Model;

/// <summary>The price the line is charged at (BG-29).</summary>
public sealed class LinePrice : InvoiceNode
{
    /// <summary>BT-146 — the net price of one base quantity, after any discount.</summary>
    public AmountField NetPrice { get; set; }

    /// <summary>BT-147 — the discount taken off the gross price to reach the net price.</summary>
    public AmountField Discount { get; set; }

    /// <summary>BT-148 — the price before discount.</summary>
    public AmountField GrossPrice { get; set; }

    /// <summary>BT-149 — the quantity the price applies to. Defaults to one when absent.</summary>
    public QuantityField BaseQuantity { get; set; }

    /// <summary>What kind of price this is — a list price, a net price, a contract price.</summary>
    public CodeField PriceTypeCode { get; set; }
}
