using International.EInvoicing.Values;

namespace International.EInvoicing.Model;

/// <summary>
/// What a buyer is asking for, as an order describes it.
/// </summary>
/// <remarks>
/// A third item type, and for the same reason as the second: an order's item is a thing being chosen from a
/// catalogue, so it carries the manufacturer's article number and the specification the buyer is ordering
/// against — neither of which an invoice's <see cref="Item"/> or a despatch advice's
/// <see cref="DespatchItem"/> has any use for. What the three share is a name and some article numbers.
/// </remarks>
public sealed class OrderItem : InvoiceNode
{
    /// <summary>What the item is called.</summary>
    public TextField Name { get; set; }

    /// <summary>A fuller description.</summary>
    public TextField Description { get; set; }

    /// <summary>The seller's article number.</summary>
    public IdentifierField SellerIdentifier { get; set; }

    /// <summary>The buyer's article number.</summary>
    public IdentifierField BuyerIdentifier { get; set; }

    /// <summary>The manufacturer's article number, which outlives any one seller's catalogue.</summary>
    public IdentifierField ManufacturerIdentifier { get; set; }

    /// <summary>The identifier from a standard scheme, a GTIN above all.</summary>
    public IdentifierField StandardIdentifier { get; set; }

    /// <summary>The specification the buyer is ordering against.</summary>
    public IdentifierField SpecificationReference { get; set; }

    /// <summary>How the item is classified, in whichever scheme the code names.</summary>
    public List<CodeField> ClassificationCodes { get; } = [];

    /// <summary>Named properties of the item — colour, size, anything the parties agreed on.</summary>
    public List<OrderItemProperty> Characteristics { get; } = [];

    /// <summary>The VAT category the buyer expects to be charged.</summary>
    public CodeField VatCategoryCode { get; set; }

    /// <summary>The rate that goes with it.</summary>
    public Field<decimal> VatRate { get; set; }

    /// <summary>Which physical items are wanted: a serial number, a lot.</summary>
    public List<ItemInstance> Instances { get; } = [];
}
