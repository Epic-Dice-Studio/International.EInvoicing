using International.EInvoicing.Diagnostics;
using International.EInvoicing.Profiles;
using International.EInvoicing.Values;

namespace International.EInvoicing.Model;

/// <summary>
/// The seller's answer to an order: accepted, rejected, or accepted with changes.
/// </summary>
/// <remarks>
/// Without it a buyer who has sent an order knows nothing until goods arrive or do not — the same gap on the
/// pre-award side that the Invoice Response closes after the invoice. What makes it more than a yes or no is
/// that a seller may accept a line on different terms: a different quantity, a later date, or a substitute
/// product altogether, each of which the buyer has to see before the goods turn up.
/// </remarks>
public sealed class OrderResponse : InvoiceNode
{
    /// <summary>The response's own number.</summary>
    public IdentifierField Number { get; set; }

    /// <summary>The seller's number for the order being answered.</summary>
    public IdentifierField SalesOrderNumber { get; set; }

    /// <summary>When the response was issued.</summary>
    public DateTimeField IssuedAt { get; set; }

    /// <summary>
    /// The answer itself, from UNCL 4343 as Peppol restricts it — accepted, rejected, or with changes.
    /// </summary>
    public CodeField ResponseCode { get; set; }

    /// <summary>Free-text notes about the response.</summary>
    public List<InvoiceNote> Notes { get; } = [];

    /// <summary>What kind of document this is.</summary>
    public CodeField TypeCode { get; set; }

    /// <summary>What the sender calls this document, when they name it at all.</summary>
    public TextField Name { get; set; }

    /// <summary>Whether the document is a copy of one already sent.</summary>
    public IndicatorField IsCopy { get; set; }

    /// <summary>Whether the document is a test rather than a real response.</summary>
    public IndicatorField IsTest { get; set; }

    /// <summary>Why the document was sent — an original, a replacement, a duplicate.</summary>
    public CodeField PurposeCode { get; set; }

    /// <summary>What answer the sender wants back, and whether one is wanted at all.</summary>
    public CodeField RequestedResponseTypeCode { get; set; }

    /// <summary>How long the response stands.</summary>
    public InvoicingPeriod? ValidityPeriod { get; set; }

    /// <summary>The currency the response is expressed in.</summary>
    public CodeField CurrencyCode { get; set; }

    /// <summary>The buyer's reference, carried back from the order.</summary>
    public TextField BuyerReference { get; set; }

    /// <summary>Which order is being answered.</summary>
    public IdentifierField OrderReference { get; set; }

    /// <summary>
    /// Which order <em>change</em> is being answered, when the response follows an amendment.
    /// </summary>
    /// <remarks>
    /// A buyer who has changed an order needs to know which version the seller answered; without this a
    /// response to the amendment is indistinguishable from a late response to the original.
    /// </remarks>
    public IdentifierField OrderChangeReference { get; set; }

    /// <summary>What the document claims to conform to.</summary>
    public ProfileIdentifier SpecificationIdentifier { get; set; }

    /// <summary>The business process this document takes part in.</summary>
    public IdentifierField BusinessProcessType { get; set; }

    /// <summary>Who placed the order.</summary>
    public Party? Buyer { get; set; }

    /// <summary>Who is answering it.</summary>
    public Party? Seller { get; set; }

    /// <summary>When the seller undertakes to deliver the order as a whole.</summary>
    public OrderDelivery? Delivery { get; set; }

    /// <summary>Who asked for the order, when a third party did.</summary>
    public Party? Originator { get; set; }

    /// <summary>Who will be invoiced, when that is not the buyer.</summary>
    public Party? Invoicee { get; set; }

    /// <summary>The originator's own document reference.</summary>
    public IdentifierField OriginatorReference { get; set; }

    /// <summary>The contract the order was placed under.</summary>
    public IdentifierField ContractReference { get; set; }

    /// <summary>The quotation the order accepted.</summary>
    public IdentifierField QuotationReference { get; set; }

    /// <summary>The catalogue the items were chosen from.</summary>
    public IdentifierField CatalogueReference { get; set; }

    /// <summary>The blanket order the order draws down against.</summary>
    public IdentifierField BlanketOrderReference { get; set; }

    /// <summary>An earlier response this one supersedes.</summary>
    public IdentifierField PreviousOrderResponseReference { get; set; }

    /// <summary>The project the order belongs to.</summary>
    public IdentifierField ProjectReference { get; set; }

    /// <summary>What that project is called.</summary>
    public TextField ProjectName { get; set; }

    /// <summary>The delivery terms the parties agreed.</summary>
    public IdentifierField DeliveryTermsCode { get; set; }

    /// <summary>Those terms in words.</summary>
    public TextField DeliveryTerms { get; set; }

    /// <summary>Which side of the delivery terms is being named — the place of delivery, of despatch.</summary>
    public CodeField DeliveryTermsFunctionCode { get; set; }

    /// <summary>The place those terms name.</summary>
    public IdentifierField DeliveryTermsLocation { get; set; }

    /// <summary>What that place is called.</summary>
    public TextField DeliveryTermsLocationName { get; set; }

    /// <summary>What the parties agreed about payment.</summary>
    public TextField PaymentTerms { get; set; }

    /// <summary>How the buyer means to pay, when the response says so.</summary>
    public PaymentInstructions? Payment { get; set; }

    /// <summary>The seller's accounting cost centre.</summary>
    public TextField AccountingReference { get; set; }

    /// <summary>Documents sent with the response.</summary>
    public List<AdditionalDocument> AdditionalDocuments { get; } = [];

    /// <summary>
    /// Allowances and charges applying to the whole order, as agreed.
    /// </summary>
    /// <remarks>
    /// Carried by the order agreement, which is this document restating the whole order as the two parties
    /// settled it, rather than by the plain response, which only answers.
    /// </remarks>
    public List<AllowanceCharge> AllowancesAndCharges { get; } = [];

    /// <summary>The tax the parties have agreed.</summary>
    public AmountField TaxAmount { get; set; }

    /// <summary>That tax broken down by category and rate.</summary>
    public List<VatBreakdownEntry> VatBreakdown { get; } = [];

    /// <summary>What the order comes to, as agreed.</summary>
    public DocumentTotals Totals { get; } = new();

    /// <summary>The answer line by line, when it differs from the answer as a whole.</summary>
    public List<OrderResponseLine> Lines { get; } = [];

    /// <summary>What was reported while this was read. Empty for a response built in code.</summary>
    public IReadOnlyList<Diagnostic> Diagnostics { get; set; } = [];

    /// <summary>How the declared specification identifier was resolved. <c>null</c> for one built in code.</summary>
    public ProfileResolution? Profile { get; set; }
}
