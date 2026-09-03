using International.EInvoicing.Values;

namespace International.EInvoicing.Model;

/// <summary>The price the line is charged at (BG-29).</summary>
public sealed class LinePrice : InvoiceNode
{
    /// <summary>BT-146 — the net price of one base quantity, after any discount.</summary>
    public AmountField NetPrice { get; set; }

    /// <summary>BT-147 — the discount taken off the gross price to reach the net price.</summary>
    public AmountField Discount { get; set; }

    /// <summary>
    /// The per-unit allowances and charges that take the gross price to the net price, in full.
    /// </summary>
    /// <remarks>
    /// EN 16931 has one term for this (BT-147, the item price discount) and it is a single amount, which is
    /// what <see cref="Discount"/> carries. Order-X allows several, each with its own reason, and a document
    /// that states two reasons has said something a single amount cannot hold. So this is the full account
    /// and <see cref="Discount"/> is their total — a syntax that can write only one writes that.
    /// </remarks>
    public List<AllowanceCharge> Adjustments { get; } = [];

    /// <summary>BT-148 — the price before discount.</summary>
    public AmountField GrossPrice { get; set; }

    /// <summary>BT-149 — the quantity the price applies to. Defaults to one when absent.</summary>
    public QuantityField BaseQuantity { get; set; }

    /// <summary>What kind of price this is — a list price, a net price, a contract price.</summary>
    public CodeField PriceTypeCode { get; set; }
}
