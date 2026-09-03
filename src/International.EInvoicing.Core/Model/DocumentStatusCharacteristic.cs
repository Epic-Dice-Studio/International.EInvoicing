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

    /// <summary>The value, when it is an amount.</summary>
    public AmountField ValueAmount { get; set; }

    /// <summary>The value, when it is a percentage.</summary>
    public Field<decimal> ValuePercent { get; set; }

    /// <summary>The value, when it is text.</summary>
    public TextField ValueText { get; set; }
}
