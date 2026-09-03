using International.EInvoicing.Values;

namespace International.EInvoicing.Model;

/// <summary>A pallet, a box, a crate — what the goods are physically handled as.</summary>
public sealed class TransportHandlingUnit : InvoiceNode
{
    /// <summary>The unit's identifier, which is what a warehouse scans.</summary>
    public IdentifierField Identifier { get; set; }

    /// <summary>What kind of unit it is (UN/ECE Recommendation 21).</summary>
    public CodeField TypeCode { get; set; }

    /// <summary>Whether it carries dangerous goods, which decides how it may be moved.</summary>
    public IndicatorField Hazardous { get; set; }

    /// <summary>What is written on the outside.</summary>
    public TextField ShippingMarks { get; set; }

    /// <summary>What is being measured — length, gross weight — in UNTDID 6313.</summary>
    public CodeField MeasuredAttribute { get; set; }

    /// <summary>The measurement itself, with its unit.</summary>
    public QuantityField Measure { get; set; }

    /// <summary>The packages inside it.</summary>
    public List<Package> Packages { get; } = [];
}
