using International.EInvoicing.Values;

namespace International.EInvoicing.Model;

/// <summary>One kind of goods on a despatch advice: what was sent, and what was not.</summary>
public sealed class DespatchLine : InvoiceNode
{
    /// <summary>The line's identifier within the document.</summary>
    public IdentifierField Identifier { get; set; }

    /// <summary>A free-text note about this line.</summary>
    public TextField Note { get; set; }

    /// <summary>How much was actually sent.</summary>
    public QuantityField DeliveredQuantity { get; set; }

    /// <summary>
    /// How much was ordered and not sent.
    /// </summary>
    /// <remarks>
    /// A quantity here obliges the sender to say why: <c>PEPPOL-T16-R007</c> refuses an outstanding quantity
    /// with no <see cref="OutstandingReason"/>, because a buyer told goods are missing and not told why has
    /// nothing to act on.
    /// </remarks>
    public QuantityField OutstandingQuantity { get; set; }

    /// <summary>Why the outstanding quantity was not sent.</summary>
    public TextField OutstandingReason { get; set; }

    /// <summary>Which line of the order this fulfils.</summary>
    public IdentifierField OrderLineReference { get; set; }

    /// <summary>The seller's own line number for it, when they number theirs differently.</summary>
    public IdentifierField SalesOrderLineReference { get; set; }

    /// <summary>Which order that line belongs to, when it is not the document's own.</summary>
    public IdentifierField OrderReference { get; set; }

    /// <summary>Documents about this line in particular.</summary>
    public List<AdditionalDocument> AdditionalDocuments { get; } = [];

    /// <summary>What was sent.</summary>
    public DespatchItem? Item { get; set; }

    /// <summary>
    /// How this line's goods are packed, which is a different question from how the consignment travels.
    /// </summary>
    /// <remarks>
    /// UBL states both with <c>cac:Shipment</c>: the document's one says how the delivery moves, and a
    /// line's one says which boxes these particular goods are in.
    /// </remarks>
    public Shipment? Packaging { get; set; }
}
