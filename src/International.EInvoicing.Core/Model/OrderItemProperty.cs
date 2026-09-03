using International.EInvoicing.Values;

namespace International.EInvoicing.Model;

/// <summary>
/// A named property of an ordered item, which may be a measurement rather than words.
/// </summary>
/// <remarks>
/// An order says "length: 3 metres" where an invoice says "colour: blue", so this carries a quantity and the
/// qualifier that says what kind of value it is — which <see cref="ItemCharacteristic"/>, written for the
/// invoice's text-only form, does not.
/// </remarks>
public sealed class OrderItemProperty : InvoiceNode
{
    /// <summary>The property's identifier in whatever scheme the parties use.</summary>
    public IdentifierField Identifier { get; set; }

    /// <summary>What the property is called.</summary>
    public TextField Name { get; set; }

    /// <summary>The same, as a code from whichever list the parties use.</summary>
    public CodeField NameCode { get; set; }

    /// <summary>The value, as words.</summary>
    public TextField Value { get; set; }

    /// <summary>What kind of value it is.</summary>
    public TextField ValueQualifier { get; set; }

    /// <summary>The value, when it is a measurement.</summary>
    public QuantityField ValueQuantity { get; set; }
}
