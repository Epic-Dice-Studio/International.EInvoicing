using International.EInvoicing.Values;

namespace International.EInvoicing.Model;

/// <summary>What the seller says about one line of the order.</summary>
public sealed class OrderResponseLine : InvoiceNode
{
    /// <summary>Which line of the order this answers.</summary>
    public IdentifierField OrderLineReference { get; set; }

    /// <summary>The line's identifier in this response.</summary>
    public IdentifierField Identifier { get; set; }

    /// <summary>Free-text notes about this line.</summary>
    public List<InvoiceNote> Notes { get; } = [];

    /// <summary>
    /// What the seller is doing with the line, from UNTDID 1229 — accepted, changed, rejected.
    /// </summary>
    public CodeField StatusCode { get; set; }

    /// <summary>How much the seller will supply, which is not always how much was asked for.</summary>
    public QuantityField Quantity { get; set; }

    /// <summary>The most the seller will place on back order.</summary>
    public QuantityField MaximumBackorderQuantity { get; set; }

    /// <summary>
    /// What the buyer asked for on this line, carried back so the two can be compared.
    /// </summary>
    /// <remarks>
    /// Order-X states the requested quantity and the agreed one side by side, and the difference between
    /// them is the whole point of a response that is not a plain acceptance.
    /// </remarks>
    public QuantityField RequestedQuantity { get; set; }

    /// <summary>What the line references.</summary>
    public List<AdditionalDocument> AdditionalDocuments { get; } = [];

    /// <summary>The allowances and charges the seller applies to this line.</summary>
    public List<AllowanceCharge> AllowancesAndCharges { get; } = [];

    /// <summary>Whether the seller will deliver this line in parts.</summary>
    public IndicatorField PartialDeliveryAccepted { get; set; }

    /// <summary>How many packages the agreed quantity comes in.</summary>
    public QuantityField PackageQuantity { get; set; }

    /// <summary>How many units are in each of them.</summary>
    public QuantityField UnitsPerPackage { get; set; }

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
