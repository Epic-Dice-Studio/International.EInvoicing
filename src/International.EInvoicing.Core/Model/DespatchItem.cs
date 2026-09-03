using International.EInvoicing.Values;

namespace International.EInvoicing.Model;

/// <summary>
/// What was sent, as a despatch advice describes it.
/// </summary>
/// <remarks>
/// Not <see cref="Item"/>, though UBL calls both <c>cac:Item</c>. An invoice's item is what is being charged
/// for and carries what EN 16931 needs to charge for it; a despatched item is a physical thing in a box, and
/// carries what a warehouse, a courier and a recall need — which serial numbers went out, which lot they
/// came from, and whether the box is dangerous to carry. Keeping them apart is what stops the invoice model
/// growing a logistics vocabulary no invoice uses.
/// </remarks>
public sealed class DespatchItem : InvoiceNode
{
    /// <summary>What the item is called.</summary>
    public TextField Name { get; set; }

    /// <summary>A fuller description.</summary>
    public TextField Description { get; set; }

    /// <summary>The seller's article number.</summary>
    public IdentifierField SellerIdentifier { get; set; }

    /// <summary>The buyer's article number.</summary>
    public IdentifierField BuyerIdentifier { get; set; }

    /// <summary>The identifier from a standard scheme, a GTIN above all.</summary>
    public IdentifierField StandardIdentifier { get; set; }

    /// <summary>
    /// The extension that qualifies an article number, when the party numbering it uses one.
    /// </summary>
    /// <remarks>
    /// UBL allows it beside any of the three identifiers above — a variant, a revision, a packaging level —
    /// so each keeps its own, and a document is written back with the extension on the identifier it
    /// qualified rather than on whichever one happened to be first.
    /// </remarks>
    public IdentifierField SellerIdentifierExtension { get; set; }

    /// <inheritdoc cref="SellerIdentifierExtension"/>
    public IdentifierField BuyerIdentifierExtension { get; set; }

    /// <inheritdoc cref="SellerIdentifierExtension"/>
    public IdentifierField StandardIdentifierExtension { get; set; }

    /// <summary>How the item is classified, in whichever scheme the code names.</summary>
    public List<ItemClassification> Classifications { get; } = [];

    /// <summary>Named properties of the item — colour, size, anything the parties agreed on.</summary>
    public List<ItemCharacteristic> Characteristics { get; } = [];

    /// <summary>The UN number of the dangerous goods this item is, when it is any.</summary>
    public CodeField DangerousGoodsCode { get; set; }

    /// <summary>The hazard class those goods fall in.</summary>
    public CodeField HazardClass { get; set; }

    /// <summary>Which physical items these are: serial numbers, lots, best-before dates.</summary>
    public List<ItemInstance> Instances { get; } = [];
}
