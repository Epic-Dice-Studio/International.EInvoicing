using International.EInvoicing.Diagnostics;
using International.EInvoicing.Profiles;
using International.EInvoicing.Values;

namespace International.EInvoicing.Model;

/// <summary>
/// An invoice, expressed in the semantic model of EN 16931 rather than in any syntax. UBL, CII and the hybrid
/// formats are projections of this: reading any of them fills this model, and writing any of them reads it.
/// </summary>
/// <remarks>
/// Property names follow the standard's vocabulary, not each syntax's, and every property names the business
/// term it carries so it can be cross-referenced with the norm.
/// </remarks>
public sealed class EInvoice : InvoiceNode
{
    /// <summary>BT-1 — the invoice number.</summary>
    public IdentifierField Number { get; set; }

    /// <summary>BT-2 — the date the invoice was issued.</summary>
    public DateField IssueDate { get; set; }

    /// <summary>BT-3 — invoice type code (UNTDID 1001). It is what tells an invoice from a credit note.</summary>
    public CodeField TypeCode { get; set; }

    /// <summary>
    /// A universally unique identifier for the document, when one is required.
    /// </summary>
    /// <remarks>
    /// EN 16931 has no such term — BT-1, the invoice number, is what identifies an invoice. Some
    /// jurisdictions want a UUID beside it: Singapore's <c>BR-108-GST-SG</c> is fatal without one. It is
    /// carried here rather than in extension data because it is a document identifier, not an extension.
    /// </remarks>
    public IdentifierField DocumentUuid { get; set; }

    /// <summary>BT-5 — the currency the invoice is expressed in (ISO 4217).</summary>
    public CodeField CurrencyCode { get; set; }

    /// <summary>BT-6 — the currency VAT is accounted for in, when it differs from BT-5.</summary>
    public CodeField TaxAccountingCurrencyCode { get; set; }

    /// <summary>
    /// The tax scheme the categories on this invoice belong to. <c>VAT</c> unless said otherwise.
    /// </summary>
    /// <remarks>
    /// EN 16931 is a European standard and its syntax bindings say <c>VAT</c>, which is why this defaults
    /// there. It is not universal: Australia and New Zealand require <c>GST</c>, and their Peppol rules
    /// reject an invoice that says <c>VAT</c> — <c>aligned-ibrp-047-aunz</c> and its neighbours. This is one
    /// value because a document uses one scheme throughout; nothing in the syntax forbids more, and nothing
    /// this library has met needs more.
    /// </remarks>
    public CodeField TaxSchemeIdentifier { get; set; }

    /// <summary>BT-7 — the date VAT becomes accountable.</summary>
    public DateField TaxPointDate { get; set; }

    /// <summary>BT-8 — the code saying how the tax point date is determined.</summary>
    public CodeField TaxPointDateCode { get; set; }

    /// <summary>BT-9 — the date payment is due.</summary>
    public DateField DueDate { get; set; }

    /// <summary>BT-10 — the reference the buyer asked to see on the invoice, such as a Leitweg-ID.</summary>
    public TextField BuyerReference { get; set; }

    /// <summary>BT-11 — the project this invoice relates to.</summary>
    public IdentifierField ProjectReference { get; set; }

    /// <summary>BT-12 — the contract this invoice relates to.</summary>
    public IdentifierField ContractReference { get; set; }

    /// <summary>BT-13 — the buyer's purchase order.</summary>
    public IdentifierField PurchaseOrderReference { get; set; }

    /// <summary>BT-14 — the seller's sales order.</summary>
    public IdentifierField SalesOrderReference { get; set; }

    /// <summary>BT-15 — the receiving advice.</summary>
    public IdentifierField ReceivingAdviceReference { get; set; }

    /// <summary>BT-16 — the despatch advice.</summary>
    public IdentifierField DespatchAdviceReference { get; set; }

    /// <summary>BT-17 — the tender or lot this invoice answers.</summary>
    public IdentifierField TenderOrLotReference { get; set; }

    /// <summary>BT-18 — an identifier for the object the invoice is about, with its scheme.</summary>
    public IdentifierField InvoicedObjectIdentifier { get; set; }

    /// <summary>BT-19 — the buyer's accounting reference for the whole invoice.</summary>
    public TextField BuyerAccountingReference { get; set; }

    /// <summary>BT-20 — the payment terms, in words.</summary>
    public TextField PaymentTerms { get; set; }

    /// <summary>BT-23 — the business process this invoice takes part in.</summary>
    public IdentifierField BusinessProcessType { get; set; }

    /// <summary>
    /// BT-24 — what the invoice claims to conform to. Everything downstream resolves on this: the mapping
    /// applied, the rules that run, and what a validation report can honestly claim.
    /// </summary>
    public ProfileIdentifier SpecificationIdentifier { get; set; }

    /// <summary>BG-1 — free-text notes.</summary>
    public List<InvoiceNote> Notes { get; } = [];

    /// <summary>BG-3 — invoices this one refers to, such as the one it corrects.</summary>
    public List<DocumentReference> PrecedingInvoices { get; } = [];

    /// <summary>BG-4 — the seller.</summary>
    public Party? Seller { get; set; }

    /// <summary>BG-7 — the buyer.</summary>
    public Party? Buyer { get; set; }

    /// <summary>BG-10 — the party to be paid, when it is not the seller.</summary>
    public Party? Payee { get; set; }

    /// <summary>BG-11 — the seller's tax representative.</summary>
    public Party? SellerTaxRepresentative { get; set; }

    /// <summary>BG-13 — where and when delivery took place.</summary>
    public DeliveryInformation? Delivery { get; set; }

    /// <summary>BG-14 — the period the invoice covers.</summary>
    public InvoicingPeriod? Period { get; set; }

    /// <summary>BG-16 — how the invoice is to be paid.</summary>
    public PaymentInstructions? Payment { get; set; }

    /// <summary>BG-20 and BG-21 — allowances and charges applying to the whole invoice.</summary>
    public List<AllowanceCharge> AllowancesAndCharges { get; } = [];

    /// <summary>BG-22 — what the invoice adds up to.</summary>
    public DocumentTotals Totals { get; } = new();

    /// <summary>BG-23 — VAT, broken down by category and rate.</summary>
    public List<VatBreakdownEntry> VatBreakdown { get; } = [];

    /// <summary>BG-24 — supporting documents, referenced or attached.</summary>
    public List<AdditionalDocument> AdditionalDocuments { get; } = [];

    /// <summary>BG-25 — the invoice lines.</summary>
    public List<InvoiceLine> Lines { get; } = [];

    /// <summary>
    /// What was reported while this invoice was read: unknown profiles, values that could not be typed,
    /// elements kept as extension data. Empty for an invoice built in code.
    /// </summary>
    /// <remarks>Set by whichever reader produced the invoice, including a reader you wrote yourself.</remarks>
    public IReadOnlyList<Diagnostic> Diagnostics { get; set; } = [];

    /// <summary>
    /// How the declared specification identifier was resolved, and what was given up along the way.
    /// <c>null</c> for an invoice built in code.
    /// </summary>
    /// <remarks>Set by whichever reader produced the invoice, including a reader you wrote yourself.</remarks>
    public ProfileResolution? Profile { get; set; }
}
