using International.EInvoicing.Countries.France.EReporting;
using International.EInvoicing.Countries.France.EReporting.Building;
using International.EInvoicing.Countries.France.EReporting.Model;
using International.EInvoicing.Countries.France.EReporting.Reading;
using International.EInvoicing.Countries.France.EReporting.Writing;

namespace International.EInvoicing.Samples.Chapters;

/// <summary>
/// French e-reporting — <em>flux 10</em>, the report that accompanies invoicing rather than replacing it.
/// </summary>
/// <remarks>
/// It carries what invoicing does not: sales to consumers, transactions with parties abroad, and when the
/// money actually arrived. A different document — neither UBL nor CII, and no XML namespace at all.
/// </remarks>
internal static class FrenchEReporting
{
    private static readonly DateOnly From = new(2026, 9, 1);
    private static readonly DateOnly To = new(2026, 9, 30);

    public static void Run()
    {
        Report.Chapter("French e-reporting (flux 10)");

        ADayOfCounterSales();
        AnInvoiceToABuyerAbroad();
        WhenTheMoneyArrived();
    }

    /// <summary>Flux 10.3 — sales to consumers, totalled by day rather than listed.</summary>
    private static void ADayOfCounterSales()
    {
        FrEReport report = FrEReporting
            .Transactions(From, To)
            .From("0003", "PA-E Vendeur")            // the platform transmitting
            .For("100000009", "VENDEUR")             // the company being reported on
            .Day(From, FrEReportCodes.RetailTransactions, split => split
                .At(20m, 1000m)                      // €1000 at 20%, the VAT worked out
                .At(5.5m, 200m))
            .Counting(42)
            .Build();

        FrTransactionSummary day = report.Transactions!.Summaries[0];

        Report.Fact("kind of transmission", report.Document.TypeCode.Value);
        Report.Fact("identifier derived for you", report.Document.Identifier.Value);
        Report.Fact("total excluding VAT", day.TaxExclusiveAmount.Value);
        Report.Fact("VAT, added up from the split", day.TaxAmount.Value);
        Report.Fact("transactions covered", day.TransactionCount.Value);
        Report.Note("Totals come from the split; the published rules check that they agree.");
    }

    /// <summary>Flux 10.1 — a sale reported invoice by invoice.</summary>
    private static void AnInvoiceToABuyerAbroad()
    {
        FrEReport report = FrEReporting
            .Transactions(From, To)
            .From("0003", "PA-E Vendeur")
            .For("100000009", "VENDEUR")
            .Invoice(invoice => invoice
                .Numbered("F202600001", new DateOnly(2026, 9, 4))
                .InProcess("B1")
                .DueOn(new DateOnly(2026, 10, 4))
                .SoldBy("100000009", "FR32100000009")
                .BoughtAbroadBy("0223", "DE811569869", "DE", vatNumber: "DE811569869")
                .Taxed(20m, 1000m, 200m)
                .Exempt(500m, "VATEX-EU-IC", "Livraison intracommunautaire"))
            .Build();

        FrReportedInvoice reported = report.Transactions!.Invoices[0];

        Report.Fact("profile every reported invoice declares", reported.BusinessProcess.ProfileIdentifier.Value);
        Report.Fact("total excluding VAT", reported.Totals.TaxExclusiveAmount.Value);
        Report.Fact("VAT", reported.Totals.TaxAmount.Value);
        Report.Fact("VAT breakdown entries", reported.TaxSubtotals.Count);
        Report.Note("An exempt share carries its reason; the rules require both the code and the words.");
    }

    /// <summary>Flux 10.2 and 10.4 — collection, which is when VAT falls due on services.</summary>
    private static void WhenTheMoneyArrived()
    {
        FrEReport report = FrEReporting
            .Payments(From, To)
            .From("0003", "PA-E Vendeur")
            .For("100000009", "VENDEUR")
            .ForInvoice("F202600001", new DateOnly(2026, 9, 4), new DateOnly(2026, 9, 20),
                split => split.At(20m, 1200m))
            .ForTransactions(new DateOnly(2026, 9, 21), split => split.At(5.5m, 211m))
            .Build();

        string xml = new FrEReportWriter().WriteToString(report);
        FrEReport read = new FrEReportReader().Read(xml).Value!;

        Report.Fact("payments against an invoice", read.Payments!.Invoices.Count);
        Report.Fact("payments with no invoice behind them", read.Payments.Transactions.Count);
        Report.Fact("collected on the invoice", read.Payments.Invoices[0].Payment.Subtotals[0].Amount.Value);
        Report.Fact("carries no XML namespace", System.Xml.Linq.XElement.Parse(xml).Name.NamespaceName.Length == 0);
        Report.Snippet(xml, lines: 6);
    }
}
