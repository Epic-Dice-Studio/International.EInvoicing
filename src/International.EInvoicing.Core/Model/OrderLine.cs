using International.EInvoicing.Values;

namespace International.EInvoicing.Model;

/// <summary>One thing a buyer asked for, and on what terms.</summary>
public sealed class OrderLine : InvoiceNode
{
    /// <summary>The line's identifier within the order.</summary>
    public IdentifierField Identifier { get; set; }

    /// <summary>Free-text notes about this line.</summary>
    public List<InvoiceNote> Notes { get; } = [];

    /// <summary>
    /// What is happening to the line, when the document amends an earlier order.
    /// </summary>
    /// <remarks>
    /// An order change restates every line and marks the ones that moved, so without this a seller cannot
    /// tell an amended line from one repeated unchanged — and would reprocess the lot.
    /// </remarks>
    public CodeField StatusCode { get; set; }

    /// <summary>How much is wanted.</summary>
    public QuantityField Quantity { get; set; }

    /// <summary>How many packages that quantity comes in.</summary>
    public QuantityField PackageQuantity { get; set; }

    /// <summary>How many units are in each of them.</summary>
    public QuantityField UnitsPerPackage { get; set; }

    /// <summary>What the line is expected to come to, before tax.</summary>
    public AmountField NetAmount { get; set; }

    /// <summary>The buyer's accounting cost centre for this line.</summary>
    public TextField AccountingReference { get; set; }

    /// <summary>
    /// Whether the buyer will accept part of this line rather than all of it.
    /// </summary>
    /// <remarks>
    /// It is the difference between a short delivery being acceptable and being a failure, which is why the
    /// despatch advice has to say how much it left outstanding.
    /// </remarks>
    public IndicatorField PartialDeliveryAccepted { get; set; }

    /// <summary>What it costs.</summary>
    public LinePrice? Price { get; set; }

    /// <summary>Allowances and charges applying to this line.</summary>
    public List<AllowanceCharge> AllowancesAndCharges { get; } = [];

    /// <summary>The line of an earlier order this one restates.</summary>
    public IdentifierField OrderLineReference { get; set; }

    /// <summary>The quotation this line accepts, and the line of it.</summary>
    public IdentifierField QuotationReference { get; set; }

    /// <summary>Which line of that quotation.</summary>
    public IdentifierField QuotationLineReference { get; set; }

    /// <summary>The catalogue the item was chosen from, and the line of it.</summary>
    public IdentifierField CatalogueReference { get; set; }

    /// <summary>Which line of that catalogue.</summary>
    public IdentifierField CatalogueLineReference { get; set; }

    /// <summary>The line of the blanket order this one draws down against.</summary>
    public IdentifierField BlanketOrderLineReference { get; set; }

    /// <summary>Documents sent with this line.</summary>
    public List<AdditionalDocument> AdditionalDocuments { get; } = [];

    /// <summary>What was ordered.</summary>
    public OrderItem? Item { get; set; }

    /// <summary>Where and when this line in particular is wanted, when it differs from the order.</summary>
    public OrderDelivery? Delivery { get; set; }

    /// <summary>Who asked for this line, when a third party did.</summary>
    public Party? Originator { get; set; }
}
