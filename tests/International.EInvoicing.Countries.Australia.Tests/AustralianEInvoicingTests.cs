using International.EInvoicing.Building;
using International.EInvoicing.Countries.Australia.Identifiers;
using International.EInvoicing.Model;
using International.EInvoicing.Peppol;
using International.EInvoicing.Profiles;
using International.EInvoicing.Validation;
using International.EInvoicing.Validation.Schematron;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Countries.Australia.Tests;

/// <summary>
/// What the Australian shortcut promises, and the second opinion behind it.
/// </summary>
/// <remarks>
/// Australia is the first country here on <b>PINT</b> rather than BIS Billing, so what matters most is that
/// it declares the right family: the profile and the business process are both different strings, and
/// getting one right with the other wrong is the failure mode.
/// </remarks>
public class AustralianEInvoicingTests
{
    private static readonly AustralianEInvoicing Australia = AustralianEInvoicing.Create();

    [Fact]
    public void AnInvoiceBuiltHereDeclaresThePintProfileAndThePintProcess()
    {
        EInvoice invoice = AnInvoice();

        invoice.SpecificationIdentifier.Value.ShouldBe("urn:peppol:pint:billing-1@aunz-1");
        invoice.BusinessProcessType.Value.ShouldBe(PeppolBusinessProcess.PintBilling);
        invoice.BusinessProcessType.Value.ShouldNotBe(PeppolBusinessProcess.Billing);
        invoice.CurrencyCode.Value.ShouldBe("AUD");
    }

    [Fact]
    public void TheAbnIsWrittenInTheSchemePeppolReservesForIt()
    {
        EInvoice invoice = AnInvoice();

        invoice.Seller!.ElectronicAddress.SchemeId.ShouldBe("0151");
        invoice.Seller.ElectronicAddress.Value.ShouldBe(Seller);

        Should.Throw<FormatException>(
            () => Australia.Invoice().From(seller => Australia.Describe(seller, "12345678901", "Wrong Pty Ltd")));
    }

    /// <summary>
    /// The measurement. Our modulo 89 check is a transcription of a rule Peppol publishes, and a
    /// transcription is worth what the comparison nobody ran is worth — so run it: every ABN we accept must
    /// survive <c>PEPPOL-COMMON-R050</c>, and every one we refuse must not.
    /// </summary>
    [Fact]
    public void OurCheckAgreesWithTheRulePeppolPublishes()
    {
        SchematronRuleSet rules = PeppolRules();
        var validator = new SchematronValidator();
        string template = Template();

        foreach (string accepted in AuAbnTests.ValidNumbers)
        {
            Fires(validator, rules, template, accepted).ShouldBeFalse(
                $"we accept {accepted}; Peppol's own rule rejects it");
        }

        foreach (string refused in Refused)
        {
            AuAbn.IsValid(refused).ShouldBeFalse(refused);
            Fires(validator, rules, template, refused).ShouldBeTrue(
                $"we refuse {refused}; Peppol's own rule accepts it");
        }
    }

    [Fact]
    public void WhatItWritesItReadsBack() =>
        Australia.Read(Australia.Write(AnInvoice())).RequireInvoice().Number.Value.ShouldBe("2026-0001");

    [Fact]
    public void TheWholeLibraryStaysReachable() => Australia.Library.Ubl.Syntax.ShouldBe(DocumentSyntax.Ubl);

    /// <summary>Numbers the Australian Business Register would not have issued.</summary>
    private static IEnumerable<string> Refused =>
        ["51824753557", "51824753565", "5182475355", "518247535561"];

    private static string Seller => AuAbnTests.ValidNumbers[0];

    private static string Buyer => AuAbnTests.ValidNumbers[1];

    /// <summary>
    /// A BIS Billing document, deliberately: <c>PEPPOL-COMMON-R050</c> lives in the BIS rule set, which is
    /// the only artefact this repository can run — the PINT rules are pre-compiled XSLT. The rule tests the
    /// endpoint identifier, which is the same element either way, so it is the ABN that is being measured.
    /// </summary>
    private static string Template() => AustralianEInvoicing
        .Over(EInvoicing.Create(library => library.AddDefaults().AddPeppol()))
        .Library
        .Write(BisInvoice(), DocumentFormat.Ubl);

    private static bool Fires(
        SchematronValidator validator,
        SchematronRuleSet rules,
        string template,
        string abn)
    {
        string xml = template.Replace($">{Seller}<", $">{abn}<", StringComparison.Ordinal);

        return validator.Validate(xml, rules).Messages
            .Any(message => message.RuleIdentifier == "PEPPOL-COMMON-R050");
    }

    private static SchematronRuleSet PeppolRules()
    {
        string path = Path.Combine(RepositoryRoot(), "specs", "peppol", "rules", "PEPPOL-EN16931-UBL.sch");

        Assert.SkipWhen(
            !File.Exists(path),
            "The Peppol artefacts are not present; run build/fetch-specs.sh peppol.");

        return SchematronRuleSet.Load(File.ReadAllText(path), "Peppol BIS Billing 3.0 (UBL)", "3.0");
    }

    private static EInvoice AnInvoice() => Fill(Australia.Invoice());

    private static EInvoice BisInvoice() =>
        Fill(EInvoiceBuilder.Create(PeppolProfiles.BillingUbl).InCurrency("AUD").ForPeppol());

    private static EInvoice Fill(EInvoiceBuilder builder) => builder
        .WithNumber("2026-0001")
        .IssuedOn(new DateOnly(2026, 9, 1))
        .DueOn(new DateOnly(2026, 10, 1))
        .OfType("380")
        .WithBuyerReference("REF-2026-0001")
        .From(seller => Australia.Describe(seller, Seller, "Supplier Pty Ltd")
            .WithVatIdentifier("AU" + Seller)
            .WithAddress(address =>
            {
                address.Line1 = "1 George Street";
                address.City = "Sydney";
                address.PostCode = "2000";
                address.CountryCode = "AU";
            }))
        .To(buyer => Australia.Describe(buyer, Buyer, "Customer Pty Ltd")
            .WithAddress(address =>
            {
                address.Line1 = "2 Collins Street";
                address.City = "Melbourne";
                address.PostCode = "3000";
                address.CountryCode = "AU";
            }))
        .AddLine(line => line
            .WithIdentifier("1")
            .WithItem("Consulting")
            .WithQuantity(1m, "C62")
            .WithNetPrice(1000m)
            .WithNetAmount(1000m)
            .WithVat("S", 10m))
        .Extend(invoice => invoice.Payment = new PaymentInstructions
        {
            MeansTypeCode = "30",
            CreditTransfers = { new CreditTransfer { AccountIdentifier = "AU1234567890" } },
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
