using International.EInvoicing.Model;
using International.EInvoicing.Values;

namespace International.EInvoicing.Countries.France.EReporting.Model;

/// <summary>An invoice reported to the tax administration rather than sent to a buyer.</summary>
/// <remarks>
/// It carries much of what an EN 16931 invoice does, but it is not one: fewer fields, its own codes, and a
/// buyer that may be identified only by country.
/// </remarks>
public sealed class FrReportedInvoice : InvoiceNode
{
    /// <summary>The invoice number, at most 35 characters.</summary>
    public IdentifierField Identifier { get; set; }

    /// <summary>When the invoice was issued.</summary>
    public DateField IssueDate { get; set; }

    /// <summary>What kind of invoice this is, from UNTDID 1001 as the DGFiP restricts it.</summary>
    public CodeField TypeCode { get; set; }

    /// <summary>The invoice currency, ISO 4217.</summary>
    public CodeField CurrencyCode { get; set; }

    /// <summary>When payment is due.</summary>
    public DateField DueDate { get; set; }

    /// <summary>When VAT becomes chargeable — on the debit, on collection, and so on.</summary>
    public CodeField TaxDueDateTypeCode { get; set; }

    /// <summary>Notes carried on the invoice.</summary>
    public List<FrReportedNote> Notes { get; } = [];

    /// <summary>The business process and the profile this report follows.</summary>
    public FrReportedBusinessProcess BusinessProcess { get; set; } = new();

    /// <summary>Earlier invoices this one corrects or credits.</summary>
    public List<FrReportedDocumentReference> ReferencedDocuments { get; } = [];

    /// <summary>Who sold.</summary>
    public FrReportedParty Seller { get; set; } = new();

    /// <summary>Who bought, when they are identified at all.</summary>
    public FrReportedParty? Buyer { get; set; }

    /// <summary>The seller's tax representative, when one is appointed.</summary>
    public FrReportedTaxRegistration? SellerTaxRepresentative { get; set; }

    /// <summary>Where and when the goods or services were delivered.</summary>
    public List<FrReportedDelivery> Deliveries { get; } = [];

    /// <summary>The period the invoice covers.</summary>
    public FrReportPeriod? InvoicePeriod { get; set; }

    /// <summary>Discounts and charges at document level.</summary>
    public List<FrReportedAllowanceCharge> AllowancesAndCharges { get; } = [];

    /// <summary>The invoice totals.</summary>
    public FrReportedTotals Totals { get; set; } = new();

    /// <summary>The VAT breakdown. At least one is required.</summary>
    public List<FrReportedTaxSubtotal> TaxSubtotals { get; } = [];

    /// <summary>The invoice lines, when the report carries them.</summary>
    public List<FrReportedInvoiceLine> Lines { get; } = [];
}
