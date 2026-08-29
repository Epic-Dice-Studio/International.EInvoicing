using International.EInvoicing.Values;

namespace International.EInvoicing.Model;

/// <summary>Where and when the goods or services were delivered (BG-13).</summary>
public sealed class DeliveryInformation : InvoiceNode
{
    /// <summary>BT-70 — the name of the party the goods were delivered to.</summary>
    public TextField Name { get; set; }

    /// <summary>BT-71 — an identifier for the delivery location.</summary>
    public IdentifierField LocationIdentifier { get; set; }

    /// <summary>BT-72 — the date delivery actually took place.</summary>
    public DateField ActualDeliveryDate { get; set; }

    /// <summary>BG-15 — the delivery address.</summary>
    public PostalAddress? Address { get; set; }
}
