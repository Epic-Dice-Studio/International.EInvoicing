using International.EInvoicing.Values;

namespace International.EInvoicing.Model;

/// <summary>
/// Which physical items were sent, as opposed to which kind of item.
/// </summary>
/// <remarks>
/// A despatch line says "ten of this product"; this says which ten. It is what a recall, a warranty claim or
/// a food-safety trace is answered from, so it carries the serial number, the lot and the dates that bound
/// the goods' life.
/// </remarks>
public sealed class ItemInstance : InvoiceNode
{
    /// <summary>The traceability identifier, a UDI or GS1 key.</summary>
    public IdentifierField ProductTraceIdentifier { get; set; }

    /// <summary>When it was made.</summary>
    public DateField ManufactureDate { get; set; }

    /// <summary>The date it should be used by.</summary>
    public DateField BestBeforeDate { get; set; }

    /// <summary>The serial number of this individual item.</summary>
    public IdentifierField SerialIdentifier { get; set; }

    /// <summary>Which production lot it came from.</summary>
    public IdentifierField LotIdentifier { get; set; }

    /// <summary>When that lot expires.</summary>
    public DateField LotExpiryDate { get; set; }

    /// <summary>Named properties of this individual item, as opposed to of the kind of item.</summary>
    public List<ItemCharacteristic> Characteristics { get; } = [];
}
