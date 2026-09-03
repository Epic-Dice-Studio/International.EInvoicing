using International.EInvoicing.Diagnostics;
using International.EInvoicing.Profiles;
using International.EInvoicing.Values;

namespace International.EInvoicing.Model;

/// <summary>
/// An order: what a buyer asked for, before anything was sent or charged.
/// </summary>
/// <remarks>
/// The first document of the post-award chain, and the one the other two are answered against — a despatch
/// advice says what was sent of it, and an invoice says what is owed for it. Its amounts are
/// <em>anticipated</em> rather than due: an order commits to a price, not to a debt, which is why the totals
/// are named apart from an invoice's.
/// </remarks>
public sealed class Order : InvoiceNode
{
    /// <summary>The buyer's order number.</summary>
    public IdentifierField Number { get; set; }

    /// <summary>The seller's own number for the same order, when they have quoted one.</summary>
    public IdentifierField SalesOrderNumber { get; set; }

    /// <summary>When the order was issued.</summary>
    public DateTimeField IssuedAt { get; set; }

    /// <summary>What kind of order this is.</summary>
    public CodeField TypeCode { get; set; }

    /// <summary>A free-text note about the order as a whole.</summary>
    public TextField Note { get; set; }

    /// <summary>The currency the order is expressed in.</summary>
    public CodeField CurrencyCode { get; set; }

    /// <summary>The buyer's reference, quoted back on the invoice.</summary>
    public TextField BuyerReference { get; set; }

    /// <summary>The buyer's accounting cost centre.</summary>
    public TextField AccountingReference { get; set; }

    /// <summary>How long the order stands.</summary>
    public InvoicingPeriod? ValidityPeriod { get; set; }

    /// <summary>What the document claims to conform to.</summary>
    public ProfileIdentifier SpecificationIdentifier { get; set; }

    /// <summary>The business process this document takes part in.</summary>
    public IdentifierField BusinessProcessType { get; set; }

    /// <summary>The quotation this order accepts.</summary>
    public IdentifierField QuotationReference { get; set; }

    /// <summary>An earlier order this one relates to.</summary>
    public IdentifierField OrderReference { get; set; }

    /// <summary>
    /// Which amendment this is, when the document is an order change rather than an order.
    /// </summary>
    /// <remarks>
    /// A seller who receives two amendments to the same order has no other way to tell which supersedes
    /// which — they may not arrive in the order they were sent.
    /// </remarks>
    public IdentifierField SequenceNumber { get; set; }

    /// <summary>The originator's own document reference.</summary>
    public IdentifierField OriginatorReference { get; set; }

    /// <summary>The catalogue the items were chosen from.</summary>
    public IdentifierField CatalogueReference { get; set; }

    /// <summary>The contract the order is placed under.</summary>
    public IdentifierField ContractReference { get; set; }

    /// <summary>The project the order belongs to.</summary>
    public IdentifierField ProjectReference { get; set; }

    /// <summary>Documents sent with the order.</summary>
    public List<AdditionalDocument> AdditionalDocuments { get; } = [];

    /// <summary>Who is ordering.</summary>
    public Party? Buyer { get; set; }

    /// <summary>Who is being ordered from.</summary>
    public Party? Seller { get; set; }

    /// <summary>Who asked for the order, when that is a third party.</summary>
    public Party? Originator { get; set; }

    /// <summary>Who will be invoiced, when that is not the buyer.</summary>
    public Party? Invoicee { get; set; }

    /// <summary>Where and when the goods are wanted.</summary>
    public OrderDelivery? Delivery { get; set; }

    /// <summary>The delivery terms agreed — an Incoterm, and what it applies to.</summary>
    public IdentifierField DeliveryTermsCode { get; set; }

    /// <summary>The delivery terms in words.</summary>
    public TextField DeliveryTerms { get; set; }

    /// <summary>Where the delivery terms take effect.</summary>
    public IdentifierField DeliveryTermsLocation { get; set; }

    /// <summary>The payment terms the buyer expects.</summary>
    public TextField PaymentTerms { get; set; }

    /// <summary>Allowances and charges applying to the whole order.</summary>
    public List<AllowanceCharge> AllowancesAndCharges { get; } = [];

    /// <summary>The tax the buyer anticipates.</summary>
    public AmountField TaxAmount { get; set; }

    /// <summary>What the order is expected to come to.</summary>
    public DocumentTotals Totals { get; } = new();

    /// <summary>What was ordered.</summary>
    public List<OrderLine> Lines { get; } = [];

    /// <summary>What was reported while this was read. Empty for an order built in code.</summary>
    public IReadOnlyList<Diagnostic> Diagnostics { get; set; } = [];

    /// <summary>How the declared specification identifier was resolved. <c>null</c> for one built in code.</summary>
    public ProfileResolution? Profile { get; set; }
}
