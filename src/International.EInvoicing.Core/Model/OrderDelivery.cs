using International.EInvoicing.Values;

namespace International.EInvoicing.Model;

/// <summary>Where the goods are wanted, when, and who receives them.</summary>
public sealed class OrderDelivery : InvoiceNode
{
    /// <summary>The delivery's identifier, when the parties number them.</summary>
    public IdentifierField Identifier { get; set; }

    /// <summary>How much is to be delivered here, when a line is split across deliveries.</summary>
    public QuantityField Quantity { get; set; }

    /// <summary>Where the goods go.</summary>
    public IdentifierField LocationIdentifier { get; set; }

    /// <summary>What that place is called.</summary>
    public TextField LocationName { get; set; }

    /// <summary>Its address.</summary>
    public PostalAddress? Address { get; set; }

    /// <summary>Who receives the goods there.</summary>
    public Party? Recipient { get; set; }

    /// <summary>Who the goods are to be collected from, when that is not the seller.</summary>
    public Party? Consignor { get; set; }

    /// <summary>The earliest the buyer wants them.</summary>
    public DateTimeField RequestedFrom { get; set; }

    /// <summary>The latest the buyer wants them.</summary>
    public DateTimeField RequestedUntil { get; set; }

    /// <summary>When the buyer wants them despatched.</summary>
    public DateTimeField RequestedDespatchAt { get; set; }

    /// <summary>
    /// The earliest the seller undertakes to deliver, which is their answer to <see cref="RequestedFrom"/>.
    /// </summary>
    /// <remarks>
    /// Requested and promised are different claims by different parties, so they are different fields: a
    /// buyer asking for Friday and a seller promising Monday is the ordinary case, and collapsing the two
    /// would lose which of them said what.
    /// </remarks>
    public DateTimeField PromisedFrom { get; set; }

    /// <summary>The latest the seller undertakes to deliver.</summary>
    public DateTimeField PromisedUntil { get; set; }

    /// <summary>The shipment's identifier, when the buyer names one.</summary>
    public IdentifierField ShipmentIdentifier { get; set; }

    /// <summary>How urgent the shipment is.</summary>
    public CodeField ShippingPriorityCode { get; set; }
}
