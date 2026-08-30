using International.EInvoicing.Model;

namespace International.EInvoicing.Countries.France.EReporting.Model;

/// <summary>
/// What was sold over a period: invoice by invoice for flux 10.1, or totalled for flux 10.3.
/// </summary>
/// <remarks>
/// The two are not alternatives so much as two levels of detail. A sale to a business abroad is reported as
/// an invoice; a day of counter sales is reported as a total.
/// </remarks>
public sealed class FrTransactionsReport : InvoiceNode
{
    /// <summary>The period covered.</summary>
    public FrReportPeriod Period { get; set; } = new();

    /// <summary>The invoices reported one by one — flux 10.1.</summary>
    public List<FrReportedInvoice> Invoices { get; } = [];

    /// <summary>The transactions reported as daily totals — flux 10.3.</summary>
    public List<FrTransactionSummary> Summaries { get; } = [];
}
