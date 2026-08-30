using International.EInvoicing.Building;
using International.EInvoicing.Cdar.Model;
using International.EInvoicing.Configuration;
using International.EInvoicing.Countries.France;
using International.EInvoicing.Countries.France.Lifecycle;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;
using International.EInvoicing.Validation;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Tests;

/// <summary>
/// What integrating this library should feel like, held to it by tests rather than by a guide.
/// </summary>
/// <remarks>
/// Each of these is the shortest honest version of a thing a developer arrives wanting to do. If one of them
/// grows a step, the step was added to their day too.
/// </remarks>
public class IntegrationExperienceTests
{
    [Fact]
    public void OneCallWiresEverythingAndTheFacadeCanBeInjected()
    {
        ServiceProvider provider = new ServiceCollection()
            .AddEInvoicing(einvoicing => einvoicing.AddDefaults().AddFrance())
            .BuildServiceProvider();

        EInvoicing library = provider.GetRequiredService<EInvoicing>();

        library.Ubl.ShouldNotBeNull();
        library.Lifecycle.ShouldNotBeNull();
        provider.GetRequiredService<Ubl.Writing.UblInvoiceWriter>().ShouldNotBeNull();
        provider.GetRequiredService<Cdar.Reading.CdarReader>().ShouldNotBeNull();
        library.RuleSets.ShouldNotBeEmpty();
    }

    [Fact]
    public void TheSameCallsAssembleTheLibraryWithoutAContainer()
    {
        EInvoicing library = EInvoicing.Create(einvoicing => einvoicing.AddDefaults().AddFrance());

        library.Profiles.Resolve(FrProfiles.LifecycleStatusToPartner.Id, DocumentSyntax.Cdar)
            .IsExact.ShouldBeTrue();
    }

    /// <summary>
    /// An invoice reads as the sentence it is: from the supplier, to the customer, these lines, that VAT.
    /// The totals are worked out rather than typed in beside the lines they summarise.
    /// </summary>
    [Fact]
    public void AnInvoiceBuiltTheShortWayIsArithmeticallyConsistent()
    {
        EInvoice invoice = EInvoiceBuilder
            .Create(KnownProfiles.En16931Ubl)
            .WithNumber("FA-2026-001")
            .IssuedOn(new DateOnly(2026, 9, 1))
            .InCurrency("EUR")
            .From("Fournisseur SARL", "FR32100000009")
            .To("Client SA", "FR44200000008")
            .AddLine(line => line
                .WithIdentifier("1")
                .WithQuantity(2m, "HUR")
                .WithNetPrice(500m)
                .WithNetAmount(1000m)
                .WithVat("S", 20m)
                .WithItem("Conseil"))
            .AddLine(line => line
                .WithIdentifier("2")
                .WithNetAmount(200m)
                .WithVat("S", 5.5m)
                .WithItem("Documentation"))
            .WithComputedVatBreakdown()
            .WithComputedTotals()
            .Build();

        invoice.Seller!.Name.Value.ShouldBe("Fournisseur SARL");
        invoice.Buyer!.VatIdentifier.Value.ShouldBe("FR44200000008");

        invoice.VatBreakdown.Count.ShouldBe(2);
        invoice.VatBreakdown.Single(entry => entry.Rate.Value == 20m).TaxAmount.Value.ShouldBe(200m);
        invoice.VatBreakdown.Single(entry => entry.Rate.Value == 5.5m).TaxAmount.Value.ShouldBe(11m);

        invoice.Totals.LineTotalAmount.Value.ShouldBe(1200m);
        invoice.Totals.TaxExclusiveAmount.Value.ShouldBe(1200m);
        invoice.Totals.TaxAmount.Value.ShouldBe(211m);
        invoice.Totals.TaxInclusiveAmount.Value.ShouldBe(1411m);
        invoice.Totals.DuePayableAmount.Value.ShouldBe(1411m);
        invoice.Totals.TaxAmount.CurrencyCode.ShouldBe("EUR");
    }

    /// <summary>A document-level discount reduces the base it was taken from, not every base.</summary>
    [Fact]
    public void ADocumentLevelDiscountLandsOnItsOwnVatBase()
    {
        EInvoice invoice = EInvoiceBuilder
            .Create(KnownProfiles.En16931Ubl)
            .WithNumber("FA-2026-002")
            .InCurrency("EUR")
            .AddLine(line => line.WithNetAmount(1000m).WithVat("S", 20m).WithItem("Conseil"))
            .AddLine(line => line.WithNetAmount(200m).WithVat("S", 5.5m).WithItem("Documentation"))
            .Extend(document => document.AllowancesAndCharges.Add(new AllowanceCharge
            {
                IsCharge = false,
                Amount = new Values.AmountField(100m, "EUR"),
                VatCategoryCode = "S",
                VatRate = 20m,
            }))
            .WithComputedVatBreakdown()
            .WithComputedTotals()
            .Build();

        invoice.VatBreakdown.Single(entry => entry.Rate.Value == 20m).TaxableAmount.Value.ShouldBe(900m);
        invoice.VatBreakdown.Single(entry => entry.Rate.Value == 5.5m).TaxableAmount.Value.ShouldBe(200m);
        invoice.Totals.AllowanceTotalAmount.Value.ShouldBe(100m);
        invoice.Totals.TaxExclusiveAmount.Value.ShouldBe(1100m);
        invoice.Totals.TaxAmount.Value.ShouldBe(191m);
    }

    /// <summary>The syntax comes from the profile the invoice declares, rather than being named twice.</summary>
    [Fact]
    public void WritingPicksTheSyntaxTheProfileIsWrittenIn()
    {
        EInvoicing library = EInvoicing.CreateDefault();

        EInvoice ubl = EInvoiceBuilder.Create(KnownProfiles.PeppolBisBilling3Ubl).WithNumber("FA-1").Build();
        EInvoice cii = EInvoiceBuilder.Create(KnownProfiles.FacturXBasic).WithNumber("FA-1").Build();

        System.Xml.Linq.XElement.Parse(library.Write(ubl)).Name.NamespaceName
            .ShouldBe(Ubl.UblNames.Invoice.NamespaceName);
        System.Xml.Linq.XElement.Parse(library.Write(cii)).Name.LocalName
            .ShouldBe("CrossIndustryInvoice");
    }

    /// <summary>A result that is not what the caller expected says so, with the diagnostics attached.</summary>
    [Fact]
    public void AskingForTheWrongThingFailsWithTheReasonAttached()
    {
        DocumentResult result = EInvoicing.CreateDefault().Read("<nothing/>");

        result.TryGetInvoice(out EInvoice? invoice).ShouldBeFalse();
        invoice.ShouldBeNull();

        Diagnostics.DocumentException thrown =
            Should.Throw<Diagnostics.DocumentException>(result.RequireInvoice);

        thrown.Message.ShouldContain("Unknown");
        thrown.Diagnostics.ShouldNotBeEmpty();
    }

    /// <summary>A lifecycle message reads as the sentence it is: who reports what, through whom, to whom.</summary>
    [Fact]
    public void ALifecycleMessageReadsAsWhoReportsWhatToWhom()
    {
        LifecycleStatusMessage approved = FrCdar
            .FromBuyer("200000008", "ACHETEUR")
            .SentBy("0003", "PA-E Acheteur")
            .ToSeller("100000009", "VENDEUR", "100000009_STATUTS")
            .About("F202500003", new DateOnly(2025, 7, 1))
            .Approved();

        approved.Issuer!.RoleCode.Value.ShouldBe("BY");
        approved.Sender!.RoleCode.Value.ShouldBe("WK");
        approved.Recipients[0].RoleCode.Value.ShouldBe("SE");
        approved.References[0].ProcessConditionCode.Value.ShouldBe("205");
    }

    /// <summary>Filling in the wrong party is refused with the entry point to use instead.</summary>
    [Theory]
    [InlineData("205", "FromBuyer")]
    [InlineData("200", "FromPlatform")]
    public void ReportingAStatusFromTheWrongKindOfPartySaysWhichToUse(string statusCode, string expected)
    {
        FrLifecycleStatus status = FrLifecycleStatus.FromCode(statusCode)!;

        // Deliberately the wrong way round: a platform for a business status, a company for a platform one.
        FrCdar wrong = status.IsBusinessEvent
            ? FrCdar.FromPlatform("0003", "PA-E").ToPublicPortal()
            : FrCdar.FromBuyer("200000008", "ACHETEUR").SentBy("0003", "PA-E").ToPublicPortal();

        Should.Throw<InvalidOperationException>(() => wrong.About("F1", new DateOnly(2026, 1, 1)).With(status))
            .Message.ShouldContain(expected);
    }

    [Fact]
    public void AMessageWithNoDestinationSaysSoBeforeItIsWritten()
    {
        Should.Throw<InvalidOperationException>(() => FrCdar
            .FromPlatform("0003", "PA-E")
            .About("F1", new DateOnly(2026, 1, 1))
            .Filed())
            .Message.ShouldContain("ToSeller");
    }

    /// <summary>
    /// A credit note is a document any real integration receives, and in UBL it is not an invoice with a
    /// different code — it has its own root element.
    /// </summary>
    [Fact]
    public void ACreditNoteIsRecognisedAsOneAndStillValidates()
    {
        EInvoicing library = EInvoicing.CreateDefault();
        string creditNote = File.ReadAllText(Path.Combine(
            RepositoryRoot(), "specs", "en16931", "ubl", "examples", "ubl-tc434-creditnote1.xml"));

        DocumentResult result = library.Read(creditNote);

        result.Kind.ShouldBe(DocumentKind.UblCreditNote);
        result.IsCreditNote.ShouldBeTrue();
        result.RequireInvoice().Lines.ShouldNotBeEmpty();

        // And what we write back is still a document EN 16931 accepts.
        library.Validate(library.Write(result.RequireInvoice())).IsValid.ShouldBeTrue();
    }

    /// <summary>A report a pipeline can act on in one call, rather than five properties to remember.</summary>
    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, ".git")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }

    [Fact]
    public void AReportCanBeInsistedUpon()
    {
        ValidationReport report = EInvoicing.CreateDefault().Validate("<nothing/>");

        report.NotRun.ShouldNotBeEmpty();
        Should.Throw<Diagnostics.DocumentException>(report.EnsureConforming);
    }
}
