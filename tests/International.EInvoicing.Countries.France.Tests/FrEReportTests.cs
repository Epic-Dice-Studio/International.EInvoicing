using System.Xml.Linq;
using International.EInvoicing.Countries.France.EReporting;
using International.EInvoicing.Countries.France.EReporting.Building;
using International.EInvoicing.Countries.France.EReporting.Model;
using International.EInvoicing.Countries.France.EReporting.Reading;
using International.EInvoicing.Countries.France.EReporting.Writing;
using International.EInvoicing.Diagnostics;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Countries.France.Tests;

/// <summary>
/// E-reporting is a different document from an invoice, with its own model. These tests check that the
/// builder fills in what a report implies, and that reading one back loses nothing.
/// </summary>
public class FrEReportTests
{
    private static readonly DateOnly From = new(2026, 9, 1);
    private static readonly DateOnly To = new(2026, 9, 30);

    [Fact]
    public void ADayOfSalesIsTotalledFromItsVatSplit()
    {
        FrEReport report = Daily();

        FrTransactionSummary day = report.Transactions!.Summaries.ShouldHaveSingleItem();
        day.TaxExclusiveAmount.Value.ShouldBe(1200m);
        day.TaxAmount.Value.ShouldBe(211m);
        day.TaxSubtotals.Count.ShouldBe(2);
        day.TaxSubtotals[1].TaxAmount.Value.ShouldBe(11m);
        day.CategoryCode.Value.ShouldBe(FrEReportCodes.RetailTransactions);
    }

    [Fact]
    public void AReportedInvoiceIsTotalledFromItsVatBreakdown()
    {
        FrEReport report = FrEReporting
            .Transactions(From, To)
            .From("0003", "PA-E Vendeur")
            .For("100000009", "VENDEUR")
            .Invoice(invoice => invoice
                .Numbered("F202600001", new DateOnly(2026, 9, 4))
                .InProcess("B1")
                .SoldBy("100000009", "FR32100000009")
                .BoughtAbroadBy("0223", "DE811569869", "DE", "DE811569869")
                .Taxed(20m, 1000m, 200m)
                .Exempt(500m, "VATEX-EU-IC", "Livraison intracommunautaire"))
            .Build();

        FrReportedInvoice invoice = report.Transactions!.Invoices.ShouldHaveSingleItem();
        invoice.Totals.TaxExclusiveAmount.Value.ShouldBe(1500m);
        invoice.Totals.TaxAmount.Value.ShouldBe(200m);
        invoice.BusinessProcess.ProfileIdentifier.Value.ShouldBe(FrEReportCodes.ProfileIdentifier);
        invoice.TaxSubtotals[1].CategoryCode.Value.ShouldBe("E");
    }

    [Fact]
    public void ATransmissionNamesWhoSentItAndWhoItIsAbout()
    {
        Should.Throw<InvalidOperationException>(() => FrEReporting
            .Transactions(From, To)
            .For("100000009", "VENDEUR")
            .Day(From, FrEReportCodes.RetailTransactions, split => split.At(20m, 100m))
            .Build())
            .Message.ShouldContain("From(");

        Should.Throw<InvalidOperationException>(() => FrEReporting
            .Transactions(From, To)
            .From("0003", "PA-E Vendeur")
            .Day(From, FrEReportCodes.RetailTransactions, split => split.At(20m, 100m))
            .Build())
            .Message.ShouldContain("For(");
    }

    [Fact]
    public void APeriodEndsAfterItStarts()
    {
        Should.Throw<ArgumentException>(() => FrEReporting.Transactions(To, From));
        Should.Throw<ArgumentException>(() => FrEReporting.Payments(From, From));
    }

    [Fact]
    public void AReportSaysNothingWithoutBeingAskedTo()
    {
        Should.Throw<ArgumentException>(() => FrEReporting
            .Transactions(From, To)
            .From("0003", "PA-E Vendeur")
            .For("100000009", "VENDEUR")
            .Day(From, FrEReportCodes.RetailTransactions, _ => { }));
    }

    /// <summary>Flux 10 carries no XML namespace at all, on the root or below it.</summary>
    [Fact]
    public void TheDocumentCarriesNoNamespace()
    {
        XElement written = XElement.Parse(new FrEReportWriter().WriteToString(Daily()));

        written.Name.ShouldBe((XName)"Report");
        written.DescendantsAndSelf().ShouldAllBe(element => element.Name.NamespaceName == string.Empty);
    }

    [Fact]
    public void ATransmissionReadsBackAsItWasBuilt()
    {
        FrEReport read = new FrEReportReader().Read(new FrEReportWriter().WriteToString(Daily())).Value!;

        read.Document.TypeCode.Value.ShouldBe(FrEReportCodes.InitialTransmission);
        read.Document.Sender!.Identifier.SchemeId.ShouldBe("0238");
        read.Document.Issuer!.Identifier.Value.ShouldBe("100000009");
        read.Document.IssuedAt.Value.ShouldBe(new DateTimeOffset(2026, 10, 1, 8, 0, 0, TimeSpan.Zero));
        read.Payments.ShouldBeNull();

        FrTransactionSummary day = read.Transactions!.Summaries.ShouldHaveSingleItem();
        day.Date.Value.ShouldBe(From);
        day.TransactionCount.Value.ShouldBe(42);
        day.TaxSubtotals[0].Percent.Value.ShouldBe(20m);
    }

    [Fact]
    public void APaymentReportReadsBackAsItWasBuilt()
    {
        FrEReport built = FrEReporting
            .Payments(From, To)
            .From("0003", "PA-E Vendeur")
            .For("100000009", "VENDEUR")
            .ForInvoice("F202600001", new DateOnly(2026, 9, 4), new DateOnly(2026, 9, 20), split => split.At(20m, 1200m))
            .ForTransactions(new DateOnly(2026, 9, 21), split => split.At(5.5m, 211m))
            .Build();

        FrEReport read = new FrEReportReader().Read(new FrEReportWriter().WriteToString(built)).Value!;

        read.Transactions.ShouldBeNull();
        FrReportedInvoicePayment invoice = read.Payments!.Invoices.ShouldHaveSingleItem();
        invoice.InvoiceIdentifier.Value.ShouldBe("F202600001");
        invoice.Payment.Date.Value.ShouldBe(new DateOnly(2026, 9, 20));
        invoice.Payment.Subtotals.ShouldHaveSingleItem().Amount.Value.ShouldBe(1200m);
        read.Payments.Transactions.ShouldHaveSingleItem().Subtotals[0].TaxPercent.Value.ShouldBe(5.5m);
    }

    /// <summary>A value the reader cannot interpret keeps its raw text and says why, rather than throwing.</summary>
    [Fact]
    public void AnUnreadableValueKeepsItsRawTextAndSaysWhy()
    {
        string xml = new FrEReportWriter()
            .WriteToString(Daily())
            .Replace("<Date>20260901</Date>", "<Date>1 septembre</Date>", StringComparison.Ordinal);

        FrEReport read = new FrEReportReader().Read(xml).Value!;

        FrTransactionSummary day = read.Transactions!.Summaries.ShouldHaveSingleItem();
        day.Date.HasValue.ShouldBeFalse();
        day.Date.IsRawOnly.ShouldBeTrue();
        day.Date.Raw.ShouldBe("1 septembre");
        day.Date.Diagnostic!.Code.ShouldBe("EIV2001");
    }

    /// <summary>An element the model does not describe is kept on the node that carried it.</summary>
    [Fact]
    public void AnUnknownElementIsKeptRatherThanDropped()
    {
        string xml = new FrEReportWriter()
            .WriteToString(Daily())
            .Replace("<TypeCode>IN</TypeCode>", "<TypeCode>IN</TypeCode><Nouveaute>1</Nouveaute>", StringComparison.Ordinal);

        FrEReport read = new FrEReportReader().Read(xml).Value!;

        read.Document.Extensions.ShouldHaveSingleItem().LocalName.ShouldBe("Nouveaute");
        read.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "EIV2020");
    }

    [Fact]
    public void MalformedXmlIsReportedRatherThanThrown()
    {
        ParseResult<FrEReport> result = new FrEReportReader().Read("<Report><ReportDocument>");

        result.IsUsable.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "EIV5001");
    }

    private static FrEReport Daily() => FrEReporting
        .Transactions(From, To)
        .From("0003", "PA-E Vendeur")
        .For("100000009", "VENDEUR")
        .Transmission(transmission => transmission.At(new DateTimeOffset(2026, 10, 1, 8, 0, 0, TimeSpan.Zero)))
        .Day(From, FrEReportCodes.RetailTransactions, split => split
            .At(20m, 1000m)
            .At(5.5m, 200m))
        .Counting(42)
        .Build();
}
