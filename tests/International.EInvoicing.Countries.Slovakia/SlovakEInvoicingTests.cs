using International.EInvoicing.Building;
using International.EInvoicing.Countries.Slovakia.TaxData.Model;
using International.EInvoicing.Countries.Slovakia.Validation;
using International.EInvoicing.Model;
using International.EInvoicing.Peppol;
using International.EInvoicing.Profiles;
using International.EInvoicing.Validation;
using International.EInvoicing.Validation.Schematron;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Countries.Slovakia.Tests;

/// <summary>
/// The Slovak shortcut: a Peppol invoice, and the tax data document that reports it.
/// </summary>
/// <remarks>
/// There is no Slovak CIUS in any artefact published so far, so what an invoice is measured against here is
/// the layer the mandate builds on — Peppol BIS Billing, as OpenPeppol publishes it beside the tax data rules.
/// </remarks>
public class SlovakEInvoicingTests
{
    private static readonly SlovakEInvoicing Slovensko = SlovakEInvoicing.Create();

    [Fact]
    public void AnInvoiceBuiltHereDeclaresPeppolBillingAndItsBusinessProcess()
    {
        EInvoice invoice = AnInvoice();

        invoice.SpecificationIdentifier.Value.ShouldBe(SkProfiles.PeppolBillingUbl.Id.Value);
        invoice.BusinessProcessType.Value.ShouldBe(PeppolBusinessProcess.Billing);
        invoice.CurrencyCode.Value.ShouldBe("EUR");
    }

    [Fact]
    public void AnInvoiceThisLibraryWritesSatisfiesTheRulesTheMandateBuildsOn()
    {
        string path = Path.Combine(
            RepositoryRoot(), "specs", "peppol", "rules", "PEPPOL-EN16931-UBL.sch");

        Assert.SkipWhen(!File.Exists(path), "run build/fetch-specs.sh peppol");

        ValidationReport report = new SchematronValidator().Validate(
            Slovensko.Write(AnInvoice()),
            SchematronRuleSet.Load(File.ReadAllText(path), "Peppol BIS Billing 3.0 (UBL)", "3.0"));

        report.IsValid.ShouldBeTrue(string.Join(
            Environment.NewLine,
            report.OfAtLeast(RuleSeverity.Error).Select(message => $"  {message.RuleIdentifier}: {message.Message}")));
    }

    /// <summary>
    /// The report the shortcut builds is the invoice plus what the rules decide, and nothing invented: the
    /// authority and the two endpoints are the network's business, and are left for the caller to fill in.
    /// </summary>
    [Fact]
    public void TheTaxDataDocumentIsBuiltFromTheInvoiceItReports()
    {
        EInvoice invoice = AnInvoice();

        SkTaxData taxData = Slovensko.TaxDataFor(invoice, "report-1", "document-1");

        taxData.ReportedDocument.ShouldBeSameAs(invoice);
        taxData.Uuid.ShouldBe("report-1");
        taxData.ReportedDocumentUuid.ShouldBe("document-1");
        taxData.TaxDataTypeCode.ShouldBe("S");
        taxData.DocumentScope.ShouldBe("D");
        taxData.ReporterRole.ShouldBe("C2");
        taxData.Authority.Id.ShouldBeEmpty();

        Should.Throw<ArgumentException>(() => Slovensko.TaxDataFor(invoice, " ", "document-1"));
    }

    [Fact]
    public void WhatItWritesItReadsBack() =>
        Slovensko.Read(Slovensko.Write(AnInvoice())).RequireInvoice().Number.Value.ShouldBe("2026-0001");

    [Fact]
    public void TheWholeLibraryStaysReachable() => Slovensko.Library.Ubl.Syntax.ShouldBe(DocumentSyntax.Ubl);

    [Fact]
    public void RulesAskedForWhereThereAreNoneSayWhereToGetThem() =>
        Should.Throw<DirectoryNotFoundException>(() => SkTaxDataValidator.LoadFrom(
            Path.Combine(Path.GetTempPath(), "no-slovak-rules-here")))
            .Message.ShouldContain("fetch-specs.sh");

    private static EInvoice AnInvoice() => Slovensko.Invoice()
        .WithNumber("2026-0001")
        .IssuedOn(new DateOnly(2026, 9, 1))
        .DueOn(new DateOnly(2026, 10, 1))
        .WithBuyerReference("REF-2026-0001")
        .From(seller => seller
            .Named("Dodávateľ s.r.o.")
            .WithVatIdentifier("SK2020123456")
            .WithElectronicAddress("2020123456", "9944")
            .WithAddress(address =>
            {
                address.Line1 = "Hlavná 1";
                address.City = "Bratislava";
                address.PostCode = "81101";
                address.CountryCode = "SK";
            }))
        .To(buyer => buyer
            .Named("Odberateľ s.r.o.")
            .WithVatIdentifier("SK2020654321")
            .WithElectronicAddress("2020654321", "9944")
            .WithAddress(address =>
            {
                address.Line1 = "Štúrova 2";
                address.City = "Košice";
                address.PostCode = "04001";
                address.CountryCode = "SK";
            }))
        .AddLine(line => line
            .WithIdentifier("1")
            .WithItem("Poradenstvo")
            .WithQuantity(3m, "HUR")
            .WithNetPrice(100m)
            .WithNetAmount(300m)
            .WithVat("S", 23m))
        .Extend(invoice => invoice.Payment = new PaymentInstructions
        {
            MeansTypeCode = "30",
            CreditTransfers = { new CreditTransfer { AccountIdentifier = "SK3112000000198742637541" } },
        })
        .WithComputedVatBreakdown()
        .WithComputedTotals()
        .Build();

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "International.EInvoicing.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("The repository root was not found.");
    }
}
