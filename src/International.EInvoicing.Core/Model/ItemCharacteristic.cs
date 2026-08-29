using International.EInvoicing.Values;

namespace International.EInvoicing.Model;

/// <summary>
/// A named characteristic of the item (BG-32), such as colour or size. The semantic model calls these item
/// attributes; the name here follows CII's <c>ApplicableProductCharacteristic</c> to avoid reading as a
/// .NET attribute type.
/// </summary>
public sealed class ItemCharacteristic : InvoiceNode
{
    /// <summary>BT-160 — the attribute's name.</summary>
    public TextField Name { get; set; }

    /// <summary>BT-161 — the attribute's value.</summary>
    public TextField Value { get; set; }
}
