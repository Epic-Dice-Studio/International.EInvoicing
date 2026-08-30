using International.EInvoicing.Building;
using International.EInvoicing.Countries.Denmark.Identifiers;
using International.EInvoicing.Model;
using International.EInvoicing.Peppol;
using International.EInvoicing.Profiles;
using International.EInvoicing.Validation;
using International.EInvoicing.Validation.Schematron;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Countries.Denmark.Tests;

/// <summary>
/// What the Danish shortcut promises, and the second opinion that makes the promise worth something:
/// Peppol's own rules, run over what this library writes.
/// </summary>
public class DanishEInvoicingTests
{
    private static readonly DanishEInvoicing Danmark = DanishEInvoicing.Create();

    [Fact]
    public void AnInvoiceBuiltHereDeclaresPeppolBillingAndItsBusinessProcess()
    {
        EInvoice invoice = AnInvoice();

        invoice.SpecificationIdentifier.Value.ShouldBe(DkProfiles.PeppolBillingUbl.Id.Value);
        invoice.BusinessProcessType.Value.ShouldBe(PeppolBusinessProcess.Billing);
        invoice.CurrencyCode.Value.ShouldBe("DKK");
    }

    [Fact]
    public void TheOrganisationNumberIsWrittenInTheSchemePeppolReservesForIt()
    {
        EInvoice invoice = AnInvoice();

        invoice.Seller!.ElectronicAddress.SchemeId.ShouldBe("0184");
        invoice.Seller.ElectronicAddress.Value.ShouldBe(Seller);
        invoice.Seller.VatIdentifier.Value.ShouldBe("DK" + Seller);

        Should.Throw<FormatException>(
            () => Danmark.Invoice().From(seller => Danmark.Describe(seller, "1234567", "Forkert ApS")));
    }

    /// <summary>
    /// The measurement. Our format check is a transcription of a rule Peppol publishes, and a
    /// transcription is worth exactly as much as the comparison nobody ran — so run it: every number we
    /// accept must survive <c>PEPPOL-COMMON-R042</c>, and every number we refuse must not.
    /// </summary>
    [Fact]
    public void OurCheckAgreesWithTheRulePeppolPublishes()
    {
        SchematronRuleSet rules = PeppolRules();
        var validator = new SchematronValidator();
        string template = Danmark.Write(AnInvoice());

        foreach (string accepted in DkCvrNumberTests.ValidNumbers)
        {
            Fires(validator, rules, template, accepted).ShouldBeFalse(
                $"we accept {accepted}; Peppol's own rule rejects it");
        }

        foreach (string refused in Refused)
        {
            DkCvrNumber.IsValid(refused).ShouldBeFalse(refused);
            Fires(validator, rules, template, refused).ShouldBeTrue(
                $"we refuse {refused}; Peppol's own rule accepts it");
        }
    }

    /// <summary>And the whole invoice satisfies the Danish rules, which travel inside the Peppol ones.</summary>
    [Fact]
    public void AnInvoiceThisLibraryWritesSatisfiesTheDanishRules()
    {
        ValidationReport report = new SchematronValidator().Validate(Danmark.Write(AnInvoice()), PeppolRules());

        report.IsValid.ShouldBeTrue(
            string.Join(
                Environment.NewLine,
                report.OfAtLeast(RuleSeverity.Error).Select(message => $"  {message.RuleIdentifier}: {message.Message}")));
    }

    [Fact]
    public void WhatItWritesItReadsBack() =>
        Danmark.Read(Danmark.Write(AnInvoice())).RequireInvoice().Number.Value.ShouldBe("2026-0001");

    [Fact]
    public void TheWholeLibraryStaysReachable() => Danmark.Library.Ubl.Syntax.ShouldBe(DocumentSyntax.Ubl);

    /// <summary>Numbers a Danish register would not have issued: wrong length, wrong check digit, letters.</summary>
    private static IEnumerable<string> Refused =>
        ["1234567", "123456789", "1234567A"];

    private static string Seller => DkCvrNumberTests.ValidNumbers[0];

    private static string Buyer => DkCvrNumberTests.ValidNumbers[1];

    private static bool Fires(
        SchematronValidator validator,
        SchematronRuleSet rules,
        string template,
        string organisationNumber)
    {
        string xml = template.Replace(
            $">{Seller}<",
            $">{organisationNumber}<",
            StringComparison.Ordinal);

        return validator.Validate(xml, rules).Messages
            .Any(message => message.RuleIdentifier == "PEPPOL-COMMON-R042");
    }

    private static SchematronRuleSet PeppolRules()
    {
        string path = Path.Combine(RepositoryRoot(), "specs", "peppol", "rules", "PEPPOL-EN16931-UBL.sch");

        Assert.SkipWhen(
            !File.Exists(path),
            "The Peppol artefacts are not present; run build/fetch-specs.sh peppol.");

        return SchematronRuleSet.Load(File.ReadAllText(path), "Peppol BIS Billing 3.0 (UBL)", "3.0");
    }

    private static EInvoice AnInvoice() => Danmark.Invoice()
        .WithNumber("2026-0001")
        .IssuedOn(new DateOnly(2026, 9, 1))
        .DueOn(new DateOnly(2026, 10, 1))
        .WithBuyerReference("REF-2026-0001")            // BT-10, which Peppol requires and EN 16931 does not
        .From(seller => Danmark.Describe(seller, Seller, "Leverandør ApS")
            .WithAddress(address =>
            {
                address.Line1 = "Vesterbrogade 1";
                address.City = "København";
                address.PostCode = "1620";
                address.CountryCode = "DK";
            }))
        .To(buyer => Danmark.Describe(buyer, Buyer, "Kunde A/S")
            .WithAddress(address =>
            {
                address.Line1 = "Store Torv 2";
                address.City = "Aarhus";
                address.PostCode = "8000";
                address.CountryCode = "DK";
            }))
        .AddLine(line => line
            .WithIdentifier("1")
            .WithItem("Rådgivning")
            .WithQuantity(3m, "HUR")
            .WithNetPrice(1000m)
            .WithNetAmount(3000m)
            .WithVat("S", 25m))
        .Extend(invoice => invoice.Payment = new PaymentInstructions
        {
            MeansTypeCode = DkPaymentMeans.SepaCreditTransfer,   // 30 is valid EN 16931 and refused by DK-R-005
            CreditTransfers = { new CreditTransfer { AccountIdentifier = "DK5000400440116243" } },
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
