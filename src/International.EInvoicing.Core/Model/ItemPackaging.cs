using International.EInvoicing.Values;

namespace International.EInvoicing.Model;

/// <summary>How an ordered item is packed, and how big the package is.</summary>
/// <remarks>
/// A buyer who orders by the pallet and a seller who ships by the case are not disagreeing about quantity;
/// they are disagreeing about packaging. Order-X states it on the line, and a warehouse plans against it.
/// </remarks>
public sealed class ItemPackaging : InvoiceNode
{
    /// <summary>What the package is — UN/ECE Recommendation 21, the same list a despatch advice uses.</summary>
    public CodeField TypeCode { get; set; }

    /// <summary>How wide the package is.</summary>
    public QuantityField Width { get; set; }

    /// <summary>How long it is.</summary>
    public QuantityField Length { get; set; }

    /// <summary>How high it is.</summary>
    public QuantityField Height { get; set; }
}
