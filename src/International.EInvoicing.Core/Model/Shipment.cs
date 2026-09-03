using International.EInvoicing.Values;

namespace International.EInvoicing.Model;

/// <summary>How the goods travel: how much there is of them, who carries them, and when they arrive.</summary>
public sealed class Shipment : InvoiceNode
{
    /// <summary>The shipment's identifier.</summary>
    public IdentifierField Identifier { get; set; }

    /// <summary>Free text about the shipment.</summary>
    public TextField Information { get; set; }

    /// <summary>What it weighs, packaging included.</summary>
    public QuantityField GrossWeight { get; set; }

    /// <summary>How much space it takes.</summary>
    public QuantityField GrossVolume { get; set; }

    /// <summary>How many handling units it comes in.</summary>
    public QuantityField HandlingUnitCount { get; set; }

    /// <summary>The consignment's identifier, when the carrier gives one.</summary>
    public IdentifierField ConsignmentIdentifier { get; set; }

    /// <summary>Free text about the consignment.</summary>
    public TextField ConsignmentInformation { get; set; }

    /// <summary>Who carries the goods.</summary>
    public Party? Carrier { get; set; }

    /// <summary>What the recipient quotes to find out where the goods are.</summary>
    public IdentifierField TrackingIdentifier { get; set; }

    /// <summary>When the goods actually left.</summary>
    public DateTimeField DespatchedAt { get; set; }

    /// <summary>Where they left from.</summary>
    public PostalAddress? DespatchAddress { get; set; }

    /// <summary>The earliest the goods are expected.</summary>
    public DateTimeField EstimatedDeliveryFrom { get; set; }

    /// <summary>The latest they are expected.</summary>
    public DateTimeField EstimatedDeliveryUntil { get; set; }

    /// <summary>How the goods travel (UN/ECE Recommendation 19): road, rail, sea, air.</summary>
    public CodeField TransportModeCode { get; set; }

    /// <summary>How the goods are packed for transport.</summary>
    public List<TransportHandlingUnit> HandlingUnits { get; } = [];
}
