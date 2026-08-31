using International.EInvoicing.Building;
using International.EInvoicing.Model;
using International.EInvoicing.Peppol;
using International.EInvoicing.Profiles;
using International.EInvoicing.Validation;
using International.EInvoicing.Validation.Schematron;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Countries.Singapore.Tests;

/// <summary>
/// What the Singaporean shortcut promises, held to Singapore's own published rules.
/// </summary>
/// <remarks>
/// Singapore's PINT specialisation is unusual in what it constrains: not identifiers, but arithmetic,
/// decimal places and the word GST, which appears in the business terms themselves (BT-109-GST). So that is
/// what is measured here — an invoice this library writes, put in front of the SG artefacts.
/// </remarks>
public class SingaporeEInvoicingTests
{
    private static readonly SingaporeEInvoicing Singapura = SingaporeEInvoicing.Create();

    [Fact]
    public void AnInvoiceBuiltHereDeclaresTheSingaporeanPintProfile()
    {
        EInvoice invoice = AnInvoice();

        invoice.SpecificationIdentifier.Value.ShouldBe("urn:peppol:pint:billing-1@sg-1");
        invoice.BusinessProcessType.Value.ShouldBe(PeppolBusinessProcess.PintBilling);
        invoice.CurrencyCode.Value.ShouldBe("SGD");
    }

    /// <summary>Singapore taxes in GST, and its rules are written in those terms throughout.</summary>
    [Fact]
    public void AndTaxesInGstRatherThanVat()
    {
        Singapura.Write(AnInvoice()).ShouldContain("<cbc:ID>GST</cbc:ID>");
        AnInvoice().TaxSchemeIdentifier.Value.ShouldBe("GST");
    }

    /// <summary>The measurement: Singapore's own base and jurisdiction rules over what we wrote.</summary>
    [Fact]
    public void AnInvoiceThisLibraryWritesSatisfiesTheSingaporeanRules()
    {
        string directory = Path.Combine(RepositoryRoot(), "specs", "peppol", "pint", "schematron");

        Assert.SkipWhen(
            !Directory.Exists(directory),
            "The PINT artefacts are not present; run build/fetch-specs.sh pint.");

        IReadOnlyList<SchematronRuleSet> rules = PeppolPintRules.For(SgProfiles.PintBilling, directory);
        rules.Count.ShouldBe(2, "both the base and the jurisdiction rules apply");

        string xml = Singapura.Write(AnInvoice());
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
        Singapura.Read(Singapura.Write(AnInvoice())).RequireInvoice().Number.Value.ShouldBe("2026-0001");

    [Fact]
    public void TheWholeLibraryStaysReachable() => Singapura.Library.Ubl.Syntax.ShouldBe(DocumentSyntax.Ubl);

    private static EInvoice AnInvoice() => Singapura.Invoice()
        .WithNumber("2026-0001")
        .IssuedOn(new DateOnly(2026, 9, 1))
        .DueOn(new DateOnly(2026, 10, 1))
        .OfType("380")
        .WithBuyerReference("REF-2026-0001")
        .Extend(invoice => invoice.DocumentUuid = "d0f4a1c2-6b3e-4a9d-8f21-0c5b7e9a1234")   // BR-108-GST-SG
        .From(seller => seller
            .Named("Supplier Pte Ltd")
            .WithVatIdentifier("SG12345678A")
            .WithLegalRegistration("201912345A")                                            // BR-112-GST-SG
            .WithElectronicAddress("9421023610112", "0088")     // ibr-cl-25: PINT's endpoint list is the numeric EAS one
            .WithAddress(address =>
            {
                address.Line1 = "1 Raffles Place";
                address.City = "Singapore";
                address.PostCode = "048616";
                address.CountryCode = "SG";
            }))
        .To(buyer => buyer
            .Named("Customer Pte Ltd")
            .WithElectronicAddress("9421023610129", "0088")
            .WithAddress(address =>
            {
                address.Line1 = "2 Marina Boulevard";
                address.City = "Singapore";
                address.PostCode = "018987";
                address.CountryCode = "SG";
            }))
        .AddLine(line => line
            .WithIdentifier("1")
            .WithItem("Consulting")
            .WithQuantity(1m, "C62")
            .WithNetPrice(1000m)
            .WithNetAmount(1000m)
            .WithVat(SgTaxCategory.StandardRated, 9m))    // BR-CL-17-GST-SG: "S" is not a Singapore code
        .Extend(invoice => invoice.Payment = new PaymentInstructions
        {
            MeansTypeCode = "30",
            CreditTransfers = { new CreditTransfer { AccountIdentifier = "SG1234567890" } },
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
