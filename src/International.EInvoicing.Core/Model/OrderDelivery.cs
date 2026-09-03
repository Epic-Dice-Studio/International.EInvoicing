using International.EInvoicing.Values;

namespace International.EInvoicing.Model;

/// <summary>Where the goods are wanted, when, and who receives them.</summary>
public sealed class OrderDelivery : InvoiceNode
{
    /// <summary>The delivery's identifier, when the parties number them.</summary>
    public IdentifierField Identifier { get; set; }

    /// <summary>Where the goods go.</summary>
    public IdentifierField LocationIdentifier { get; set; }

    /// <summary>What that place is called.</summary>
    public TextField LocationName { get; set; }

    /// <summary>Its address.</summary>
    public PostalAddress? Address { get; set; }

    /// <summary>Who receives the goods there.</summary>
    public Party? Recipient { get; set; }

    /// <summary>The earliest the buyer wants them.</summary>
    public DateTimeField RequestedFrom { get; set; }

    /// <summary>The latest the buyer wants them.</summary>
    public DateTimeField RequestedUntil { get; set; }

    /// <summary>When the buyer wants them despatched.</summary>
    public DateTimeField RequestedDespatchAt { get; set; }

    /// <summary>The shipment's identifier, when the buyer names one.</summary>
    public IdentifierField ShipmentIdentifier { get; set; }

    /// <summary>How urgent the shipment is.</summary>
    public CodeField ShippingPriorityCode { get; set; }
}
