using International.EInvoicing.Building;
using International.EInvoicing.Countries.NewZealand.Identifiers;
using International.EInvoicing.Model;
using International.EInvoicing.Peppol;
using International.EInvoicing.Profiles;
using International.EInvoicing.Validation;
using International.EInvoicing.Validation.Schematron;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Countries.NewZealand.Tests;

/// <summary>
/// What the NewZealand shortcut promises, and the second opinion behind it.
/// </summary>
/// <remarks>
/// New Zealand shares Australia's PINT specialisation, so the document is the same one; what differs is the
/// identifier, and an NZBN is a GS1 location number rather than a national scheme of its own — which is what
/// the measurement below holds it to.
/// </remarks>
public class NewZealandEInvoicingTests
{
    private static readonly NewZealandEInvoicing NewZealand = NewZealandEInvoicing.Create();

    [Fact]
    public void AnInvoiceBuiltHereDeclaresThePintProfileAndThePintProcess()
    {
        EInvoice invoice = AnInvoice();

        invoice.SpecificationIdentifier.Value.ShouldBe("urn:peppol:pint:billing-1@aunz-1");
        invoice.BusinessProcessType.Value.ShouldBe(PeppolBusinessProcess.PintBilling);
        invoice.BusinessProcessType.Value.ShouldNotBe(PeppolBusinessProcess.Billing);
        invoice.CurrencyCode.Value.ShouldBe("NZD");
    }

    [Fact]
    public void TheNzbnIsWrittenInTheGlnSchemePeppolRoutesItBy()
    {
        EInvoice invoice = AnInvoice();

        invoice.Seller!.ElectronicAddress.SchemeId.ShouldBe("0088");
        invoice.Seller.ElectronicAddress.Value.ShouldBe(Seller);

        Should.Throw<FormatException>(
            () => NewZealand.Invoice().From(seller => NewZealand.Describe(seller, "9429040009598", "Wrong Ltd")));
    }

    /// <summary>
    /// The measurement. Our GS1 check is a transcription of a rule Peppol publishes, and a
    /// transcription is worth what the comparison nobody ran is worth — so run it: every ABN we accept must
    /// survive <c>PEPPOL-COMMON-R040</c>, and every one we refuse must not.
    /// </summary>
    [Fact]
    public void OurCheckAgreesWithTheRulePeppolPublishes()
    {
        SchematronRuleSet rules = PeppolRules();
        var validator = new SchematronValidator();
        string template = Template();

        foreach (string accepted in NzNzbnTests.ValidNumbers)
        {
            Fires(validator, rules, template, accepted).ShouldBeFalse(
                $"we accept {accepted}; Peppol's own rule rejects it");
        }

        foreach (string refused in Refused)
        {
            NzNzbn.IsValid(refused).ShouldBeFalse(refused);
            Fires(validator, rules, template, refused).ShouldBeTrue(
                $"we refuse {refused}; Peppol's own rule accepts it");
        }
    }

    [Fact]
    public void WhatItWritesItReadsBack() =>
        NewZealand.Read(NewZealand.Write(AnInvoice())).RequireInvoice().Number.Value.ShouldBe("2026-0001");

    [Fact]
    public void TheWholeLibraryStaysReachable() => NewZealand.Library.Ubl.Syntax.ShouldBe(DocumentSyntax.Ubl);

    /// <summary>Numbers the NewZealand Business Register would not have issued.</summary>
    private static IEnumerable<string> Refused =>
        ["9429040009598", "9429040001374", "942904000959", "94290400095970"];

    private static string Seller => NzNzbnTests.ValidNumbers[0];

    private static string Buyer => NzNzbnTests.ValidNumbers[1];

    /// <summary>
    /// A BIS Billing document, deliberately: <c>PEPPOL-COMMON-R040</c> lives in the BIS rule set, which is
    /// the only artefact this repository can run — the PINT rules are pre-compiled XSLT. The rule tests the
    /// endpoint identifier, which is the same element either way, so it is the ABN that is being measured.
    /// </summary>
    private static string Template() => NewZealandEInvoicing
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
            .Any(message => message.RuleIdentifier == "PEPPOL-COMMON-R040");
    }

    private static SchematronRuleSet PeppolRules()
    {
        string path = Path.Combine(RepositoryRoot(), "specs", "peppol", "rules", "PEPPOL-EN16931-UBL.sch");

        Assert.SkipWhen(
            !File.Exists(path),
            "The Peppol artefacts are not present; run build/fetch-specs.sh peppol.");

        return SchematronRuleSet.Load(File.ReadAllText(path), "Peppol BIS Billing 3.0 (UBL)", "3.0");
    }

    private static EInvoice AnInvoice() => Fill(NewZealand.Invoice());

    private static EInvoice BisInvoice() =>
        Fill(EInvoiceBuilder.Create(PeppolProfiles.BillingUbl).InCurrency("NZD").ForPeppol());

    private static EInvoice Fill(EInvoiceBuilder builder) => builder
        .WithNumber("2026-0001")
        .IssuedOn(new DateOnly(2026, 9, 1))
        .DueOn(new DateOnly(2026, 10, 1))
        .OfType("380")
        .WithBuyerReference("REF-2026-0001")
        .From(seller => NewZealand.Describe(seller, Seller, "Supplier Ltd")
            .WithVatIdentifier("NZ" + Seller)
            .WithAddress(address =>
            {
                address.Line1 = "1 Queen Street";
                address.City = "Auckland";
                address.PostCode = "1010";
                address.CountryCode = "NZ";
            }))
        .To(buyer => NewZealand.Describe(buyer, Buyer, "Customer Ltd")
            .WithAddress(address =>
            {
                address.Line1 = "2 Lambton Quay";
                address.City = "Wellington";
                address.PostCode = "6011";
                address.CountryCode = "NZ";
            }))
        .AddLine(line => line
            .WithIdentifier("1")
            .WithItem("Consulting")
            .WithQuantity(1m, "C62")
            .WithNetPrice(1000m)
            .WithNetAmount(1000m)
            .WithVat("S", 15m))
        .Extend(invoice => invoice.Payment = new PaymentInstructions
        {
            MeansTypeCode = "30",
            CreditTransfers = { new CreditTransfer { AccountIdentifier = "NZ1234567890" } },
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
