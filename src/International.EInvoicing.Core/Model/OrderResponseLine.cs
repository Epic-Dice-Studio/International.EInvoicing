using International.EInvoicing.Values;

namespace International.EInvoicing.Model;

/// <summary>What the seller says about one line of the order.</summary>
public sealed class OrderResponseLine : InvoiceNode
{
    /// <summary>Which line of the order this answers.</summary>
    public IdentifierField OrderLineReference { get; set; }

    /// <summary>The line's identifier in this response.</summary>
    public IdentifierField Identifier { get; set; }

    /// <summary>A free-text note about this line.</summary>
    public TextField Note { get; set; }

    /// <summary>
    /// What the seller is doing with the line, from UNTDID 1229 — accepted, changed, rejected.
    /// </summary>
    public CodeField StatusCode { get; set; }

    /// <summary>How much the seller will supply, which is not always how much was asked for.</summary>
    public QuantityField Quantity { get; set; }

    /// <summary>The most the seller will place on back order.</summary>
    public QuantityField MaximumBackorderQuantity { get; set; }

    /// <summary>What the line comes to, as agreed.</summary>
    public AmountField NetAmount { get; set; }

    /// <summary>What the seller will charge.</summary>
    public LinePrice? Price { get; set; }

    /// <summary>What the seller will supply.</summary>
    public OrderItem? Item { get; set; }

    /// <summary>When the seller undertakes to deliver this line.</summary>
    public OrderDelivery? Delivery { get; set; }

    /// <summary>The line's identifier in the seller's own numbering, when they substitute a product.</summary>
    public IdentifierField SubstitutedIdentifier { get; set; }

    /// <summary>
    /// What the seller offers instead, when they cannot supply what was ordered.
    /// </summary>
    /// <remarks>
    /// It is the answer a buyer most needs to see before the goods arrive, and the one a response reduced to
    /// a status code cannot carry.
    /// </remarks>
    public OrderItem? SubstitutedItem { get; set; }
}
