using International.EInvoicing.Building;
using International.EInvoicing.Model;
using International.EInvoicing.Peppol;
using International.EInvoicing.Profiles;
using International.EInvoicing.Validation;
using International.EInvoicing.Validation.Schematron;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Countries.Malaysia.Tests;

/// <summary>
/// What the Malaysian shortcut promises, held to Malaysia's own published rules.
/// </summary>
/// <remarks>
/// Malaysia's rules want two registrations EN 16931 leaves optional — the BRN of both parties and the
/// supplier's TIN — and category codes of its own. What is measured here is an invoice this library writes,
/// put in front of the Malaysian artefacts.
/// </remarks>
public class MalaysiaEInvoicingTests
{
    private static readonly MalaysiaEInvoicing Malaysia = MalaysiaEInvoicing.Create();

    [Fact]
    public void AnInvoiceBuiltHereDeclaresTheMalaysianPintProfile()
    {
        EInvoice invoice = AnInvoice();

        invoice.SpecificationIdentifier.Value.ShouldBe("urn:peppol:pint:billing-1@my-1");
        invoice.BusinessProcessType.Value.ShouldBe(PeppolBusinessProcess.PintBilling);
        invoice.CurrencyCode.Value.ShouldBe("MYR");
    }

    /// <summary>Malaysia's category codes are its own: SA, not S.</summary>
    [Fact]
    public void AndUsesMalaysiasOwnTaxCategoryCodes()
    {
        MyTaxCategory.IsAllowed("S").ShouldBeFalse();
        AnInvoice().Lines[0].VatCategoryCode.Value.ShouldBe(MyTaxCategory.SalesTax);
    }

    /// <summary>The measurement: Malaysia's own base and jurisdiction rules over what we wrote.</summary>
    [Fact]
    public void AnInvoiceThisLibraryWritesSatisfiesTheMalaysianRules()
    {
        string directory = Path.Combine(RepositoryRoot(), "specs", "peppol", "pint", "schematron");

        Assert.SkipWhen(
            !Directory.Exists(directory),
            "The PINT artefacts are not present; run build/fetch-specs.sh pint.");

        IReadOnlyList<SchematronRuleSet> rules = PeppolPintRules.For(MyProfiles.PintBilling, directory);
        rules.Count.ShouldBe(2, "both the base and the jurisdiction rules apply");

        string xml = Malaysia.Write(AnInvoice());
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
        Malaysia.Read(Malaysia.Write(AnInvoice())).RequireInvoice().Number.Value.ShouldBe("2026-0001");

    [Fact]
    public void TheWholeLibraryStaysReachable() => Malaysia.Library.Ubl.Syntax.ShouldBe(DocumentSyntax.Ubl);

    private static EInvoice AnInvoice() => Malaysia.Invoice()
        .WithNumber("2026-0001")
        .IssuedOn(new DateOnly(2026, 9, 1))
        .DueOn(new DateOnly(2026, 10, 1))
        .OfType("380")
        .WithBuyerReference("REF-2026-0001")
        .Extend(invoice => invoice.DocumentUuid = "d0f4a1c2-6b3e-4a9d-8f21-0c5b7e9a1234")   // BR-108-GST-SG
        .From(seller => Malaysia.Describe(seller, "202001234567", "Pembekal Sdn Bhd", "C12345678901")
            .WithLegalRegistration("201912345A")                                            // BR-112-GST-SG
            .WithElectronicAddress("9421023610112", "0088")
            .WithAddress(address =>
            {
                address.Line1 = "1 Jalan Ampang";
                address.City = "Kuala Lumpur";
                address.PostCode = "50450";
                address.CountryCode = "MY";
            }))
        .To(buyer => Malaysia.Describe(buyer, "202101234567", "Pelanggan Sdn Bhd")
            .WithElectronicAddress("9421023610129", "0088")
            .WithAddress(address =>
            {
                address.Line1 = "2 Jalan Tun Razak";
                address.City = "Kuala Lumpur";
                address.PostCode = "50400";
                address.CountryCode = "MY";
            }))
        .AddLine(line => line
            .WithIdentifier("1")
            .WithItem("Consulting")
            .WithQuantity(1m, "C62")
            .WithNetPrice(1000m)
            .WithNetAmount(1000m)
            .WithVat(MyTaxCategory.SalesTax, 10m))    // BR-CL-17-GST-SG: "S" is not a Malaysia code
        .Extend(invoice => invoice.Payment = new PaymentInstructions
        {
            MeansTypeCode = "30",
            CreditTransfers = { new CreditTransfer { AccountIdentifier = "MY1234567890" } },
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
