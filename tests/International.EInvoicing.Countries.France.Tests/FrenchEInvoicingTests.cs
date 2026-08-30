using International.EInvoicing.Building;
using International.EInvoicing.Cdar.Model;
using International.EInvoicing.Countries.France.EReporting;
using International.EInvoicing.Countries.France.EReporting.Model;
using International.EInvoicing.Countries.France.Invoicing;
using International.EInvoicing.Countries.France.Lifecycle;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Countries.France.Tests;

/// <summary>
/// France exchanges four documents and a French integration receives all four through one channel. What this
/// file defends is that one object reads all four, without the caller having had to say which arrived.
/// </summary>
public class FrenchEInvoicingTests
{
    private static readonly FrenchEInvoicing France = FrenchEInvoicing.Create();

    private static readonly DateTimeOffset Moment = new(2026, 9, 4, 9, 30, 0, TimeSpan.Zero);

    [Fact]
    public void AnInvoiceComesBackAsAnInvoice()
    {
        string xml = France.Write(AnInvoice());

        FrenchDocument document = France.Read(xml);

        document.Kind.ShouldBe(FrenchDocumentKind.Invoice);
        document.TryGetInvoice(out EInvoice? invoice).ShouldBeTrue();
        invoice.Number.Value.ShouldBe("F202600001");
    }

    [Fact]
    public void ACreditNoteIsToldApartFromAnInvoice()
    {
        EInvoice creditNote = France.CreditNote()
            .WithNumber("A202600001")
            .IssuedOn(new DateOnly(2026, 9, 4))
            .InCurrency("EUR")
            .From("Vendeur SARL", "FR32732829320")
            .To("Acheteur SA", "FR89552081317")
            .AddLine(line => line.WithItem("Remise").WithNetAmount(100m).WithVat("S", 20m))
            .WithComputedVatBreakdown()
            .WithComputedTotals()
            .Build();

        France.Read(France.Write(creditNote)).Kind.ShouldBe(FrenchDocumentKind.CreditNote);
    }

    [Fact]
    public void ALifecycleStatusComesBackAsALifecycleStatus()
    {
        LifecycleStatusMessage status = France
            .StatusFromBuyer("200000008", "ACHETEUR")
            .SentBy("0003", "PA-E Acheteur")
            .ToSeller("100000009", "VENDEUR", "100000009_STATUTS")
            .About("F202600001", new DateOnly(2026, 9, 4))
            .Approved(Moment);

        FrenchDocument document = France.Read(France.Write(status));

        document.Kind.ShouldBe(FrenchDocumentKind.LifecycleStatus);
        document.TryGetLifecycleStatus(out LifecycleStatusMessage? read).ShouldBeTrue();
        read.References.ShouldHaveSingleItem().StatusCode.Value.ShouldBe("1");
    }

    /// <summary>
    /// The one that would be missed: <em>flux 10</em> carries no XML namespace, so nothing but its root name
    /// says what it is.
    /// </summary>
    [Fact]
    public void AnEReportComesBackAsAnEReport()
    {
        FrEReport report = France
            .ReportTransactions(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30))
            .From("0003", "PA-E Vendeur")
            .For("100000009", "VENDEUR")
            .Day(new DateOnly(2026, 9, 4), FrEReportCodes.RetailTransactions, day => day.At(20m, 1000m, 200m))
            .Build();

        FrenchDocument document = France.Read(France.Write(report));

        document.Kind.ShouldBe(FrenchDocumentKind.EReport);
        document.TryGetEReport(out FrEReport? read).ShouldBeTrue();
        read.Transactions!.Summaries.ShouldHaveSingleItem().TaxAmount.Value.ShouldBe(200m);
    }

    [Fact]
    public void SomethingElseEntirelyIsSaidToBeUnknownRatherThanThrown()
    {
        FrenchDocument document = France.Read("<Autre>rien du tout</Autre>");

        document.Kind.ShouldBe(FrenchDocumentKind.Unknown);
        document.IsUsable.ShouldBeFalse();
        document.Errors.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task ReadingAsynchronouslyGivesWhatReadingSynchronouslyGives()
    {
        string xml = France.Write(AnInvoice());

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(xml));
        FrenchDocument asynchronous = await France.ReadAsync(stream, TestContext.Current.CancellationToken);

        asynchronous.Kind.ShouldBe(France.Read(xml).Kind);
    }

    /// <summary>The shortcut is a shortcut, not a fence: everything underneath stays reachable.</summary>
    [Fact]
    public void TheWholeLibraryStaysReachable()
    {
        France.Library.ShouldNotBeNull();
        France.Library.Ubl.Syntax.ShouldBe(DocumentSyntax.Ubl);
    }

    [Fact]
    public void AnInvoiceBuiltHereDeclaresTheFrenchProfileAndBusinessProcess()
    {
        EInvoice invoice = AnInvoice();

        invoice.SpecificationIdentifier.Value.ShouldBe(FrProfiles.ExtendedCtcFrUbl.Id.Value);
        invoice.BusinessProcessType.Value.ShouldBe(FrBusinessProcess.Invoice);
    }

    private static EInvoice AnInvoice() => France.Invoice()
        .WithNumber("F202600001")
        .IssuedOn(new DateOnly(2026, 9, 4))
        .InCurrency("EUR")
        .From("Vendeur SARL", "FR32732829320")
        .To("Acheteur SA", "FR89552081317")
        .AddLine(line => line.WithItem("Conseil").WithNetAmount(1000m).WithVat("S", 20m))
        .WithComputedVatBreakdown()
        .WithComputedTotals()
        .Build();
}
