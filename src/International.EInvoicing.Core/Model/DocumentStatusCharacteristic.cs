using International.EInvoicing.Values;

namespace International.EInvoicing.Model;

/// <summary>
/// One value a status detail is about, and where in the document it came from.
/// </summary>
/// <remarks>
/// This is what turns "refused" into something actionable: the business term at issue, the value the sender
/// read, and the value they expected instead.
/// </remarks>
public sealed class DocumentStatusCharacteristic : InvoiceNode
{
    /// <summary>The business term this is about, <c>BT-152</c> for instance.</summary>
    public IdentifierField Identifier { get; set; }

    /// <summary>What kind of value this is, in the profiling's own list.</summary>
    public CodeField TypeCode { get; set; }

    /// <summary>Whether the value differs from the one in the document.</summary>
    public IndicatorField ValueChanged { get; set; }

    /// <summary>What the value is called.</summary>
    public TextField Name { get; set; }

    /// <summary>Where the value sits in the document reported on, as a path.</summary>
    public TextField Location { get; set; }

    /// <summary>Which way the value moved, when the status is about an adjustment.</summary>
    public CodeField AdjustmentDirectionCode { get; set; }

    /// <summary>What the value means, spelled out.</summary>
    public TextField Description { get; set; }

    /// <summary>The value, when it is an amount.</summary>
    public AmountField ValueAmount { get; set; }

    /// <summary>The value, when it is a measured quantity and the unit it was measured in.</summary>
    public QuantityField ValueMeasure { get; set; }

    /// <summary>The value, when it is a moment — the date a payment was made, for instance.</summary>
    public DateTimeField ValueDateTime { get; set; }

    /// <summary>The value, when it is a code.</summary>
    public CodeField ValueCode { get; set; }

    /// <summary>The value, when it is a counted quantity.</summary>
    public QuantityField ValueQuantity { get; set; }

    /// <summary>The value, when it is a bare number.</summary>
    public Field<decimal> ValueNumeric { get; set; }

    /// <summary>The value, when it is a percentage.</summary>
    public Field<decimal> ValuePercent { get; set; }

    /// <summary>The value, when it is text.</summary>
    public TextField ValueText { get; set; }
}
