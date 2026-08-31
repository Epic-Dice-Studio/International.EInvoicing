using International.EInvoicing.Building;
using International.EInvoicing.Model;
using International.EInvoicing.Peppol;
using International.EInvoicing.Profiles;
using International.EInvoicing.Validation;
using International.EInvoicing.Validation.Schematron;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Countries.Japan.Tests;

/// <summary>
/// What the Japanese shortcut promises, held to Japan's own published rules.
/// </summary>
/// <remarks>
/// Japan's rules are lighter than its neighbours' but not empty: an invoice must carry a period, which
/// EN 16931 leaves optional. What is measured here is an invoice this library writes, put in front of the
/// Japanese artefacts.
/// </remarks>
public class JapanEInvoicingTests
{
    private static readonly JapanEInvoicing Japan = JapanEInvoicing.Create();

    [Fact]
    public void AnInvoiceBuiltHereDeclaresTheJapanesePintProfile()
    {
        EInvoice invoice = AnInvoice();

        invoice.SpecificationIdentifier.Value.ShouldBe("urn:peppol:pint:billing-1@jp-1");
        invoice.BusinessProcessType.Value.ShouldBe(PeppolBusinessProcess.PintBilling);
        invoice.CurrencyCode.Value.ShouldBe("JPY");
    }

    /// <summary>
    /// The rule that catches people: Japan requires a period, which EN 16931 leaves optional.
    /// </summary>
    [Fact]
    public void AnInvoiceCarriesThePeriodJapanRequires() =>
        AnInvoice().Period.ShouldNotBeNull();

    /// <summary>The measurement: Japan's own base and jurisdiction rules over what we wrote.</summary>
    [Fact]
    public void AnInvoiceThisLibraryWritesSatisfiesTheJapaneseRules()
    {
        string directory = Path.Combine(RepositoryRoot(), "specs", "peppol", "pint", "schematron");

        Assert.SkipWhen(
            !Directory.Exists(directory),
            "The PINT artefacts are not present; run build/fetch-specs.sh pint.");

        IReadOnlyList<SchematronRuleSet> rules = PeppolPintRules.For(JpProfiles.PintBilling, directory);
        rules.Count.ShouldBe(2, "both the base and the jurisdiction rules apply");

        string xml = Japan.Write(AnInvoice());
        var validator = new SchematronValidator();

        foreach (SchematronRuleSet ruleSet in rules)
        {
            ValidationReport report = validator.Validate(xml, ruleSet);

            report.IsValid.ShouldBeTrue(
                $"{ruleSet.Name} rejected the invoice this library wrote:{Environment.NewLine}"
                + string.Join(
                    Environment.NewLine,
                    report.OfAtLeast(RuleSeverity.Error).Select(m => $"  {m.RuleIdentifier} [{m.Message.Length}]: {m.Message}")));
        }
    }

    [Fact]
    public void WhatItWritesItReadsBack() =>
        Japan.Read(Japan.Write(AnInvoice())).RequireInvoice().Number.Value.ShouldBe("2026-0001");

    [Fact]
    public void TheWholeLibraryStaysReachable() => Japan.Library.Ubl.Syntax.ShouldBe(DocumentSyntax.Ubl);

    private static EInvoice AnInvoice() => Japan.Invoice()
        .WithNumber("2026-0001")
        .IssuedOn(new DateOnly(2026, 9, 1))
        .DueOn(new DateOnly(2026, 10, 1))
        .OfType("380")
        .WithBuyerReference("REF-2026-0001")
        .Extend(invoice => invoice.Period = new InvoicingPeriod    // aligned-ibrp-052 wants one
        {
            StartDate = new DateOnly(2026, 9, 1),
            EndDate = new DateOnly(2026, 9, 30),
        })
        .From(seller => Japan.Describe(seller, "T1234567890123", "供給者株式会社")
            .WithLegalRegistration("201912345A")                                            // BR-112-GST-SG
            .WithElectronicAddress("9421023610112", "0088")
            .WithAddress(address =>
            {
                address.Line1 = "1-1 Marunouchi";
                address.City = "Tokyo";
                address.PostCode = "100-0005";
                address.CountryCode = "JP";
            }))
        .To(buyer => Japan.Describe(buyer, "T9876543210987", "顧客株式会社")
            .WithElectronicAddress("9421023610129", "0088")
            .WithAddress(address =>
            {
                address.Line1 = "2-2 Umeda";
                address.City = "Tokyo";
                address.PostCode = "530-0001";
                address.CountryCode = "JP";
            }))
        .AddLine(line => line
            .WithIdentifier("1")
            .WithItem("Consulting")
            .WithQuantity(1m, "C62")
            .WithNetPrice(1000m)
            .WithNetAmount(1000m)
            .WithVat("S", 10m))    // BR-CL-17-GST-SG: "S" is not a Japan code
        .Extend(invoice => invoice.Payment = new PaymentInstructions
        {
            MeansTypeCode = "30",
            CreditTransfers = { new CreditTransfer { AccountIdentifier = "JP1234567890" } },
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
