using International.EInvoicing.Building;
using International.EInvoicing.Countries.Sweden.Identifiers;
using International.EInvoicing.Model;
using International.EInvoicing.Peppol;
using International.EInvoicing.Profiles;
using International.EInvoicing.Validation;
using International.EInvoicing.Validation.Schematron;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Countries.Sweden.Tests;

/// <summary>
/// What the Swedish shortcut promises, and the second opinion that makes the promise worth something:
/// Peppol's own rules, run over what this library writes.
/// </summary>
public class SwedishEInvoicingTests
{
    private static readonly SwedishEInvoicing Sverige = SwedishEInvoicing.Create();

    [Fact]
    public void AnInvoiceBuiltHereDeclaresPeppolBillingAndItsBusinessProcess()
    {
        EInvoice invoice = AnInvoice();

        invoice.SpecificationIdentifier.Value.ShouldBe(SeProfiles.PeppolBillingUbl.Id.Value);
        invoice.BusinessProcessType.Value.ShouldBe(PeppolBusinessProcess.Billing);
        invoice.CurrencyCode.Value.ShouldBe("SEK");
    }

    [Fact]
    public void TheOrganisationNumberIsWrittenInTheSchemePeppolReservesForIt()
    {
        EInvoice invoice = AnInvoice();

        invoice.Seller!.ElectronicAddress.SchemeId.ShouldBe("0007");
        invoice.Seller.ElectronicAddress.Value.ShouldBe(Seller);
        invoice.Seller.VatIdentifier.Value.ShouldBe("SE" + Seller + "01");

        Should.Throw<FormatException>(
            () => Sverige.Invoice().From(seller => Sverige.Describe(seller, NotAnOrganisationNumber, "Fel AB")));
    }

    /// <summary>
    /// The measurement. Our Luhn check is a transcription of a rule Peppol publishes, and a
    /// transcription is worth exactly as much as the comparison nobody ran — so run it: every number we
    /// accept must survive <c>PEPPOL-COMMON-R049</c>, and every number we refuse must not.
    /// </summary>
    [Fact]
    public void OurCheckAgreesWithTheRulePeppolPublishes()
    {
        SchematronRuleSet rules = PeppolRules();
        var validator = new SchematronValidator();
        string template = Sverige.Write(AnInvoice());

        foreach (string accepted in SeOrganisationNumberTests.ValidNumbers)
        {
            Fires(validator, rules, template, accepted).ShouldBeFalse(
                $"we accept {accepted}; Peppol's own rule rejects it");
        }

        foreach (string refused in Refused)
        {
            SeOrganisationNumber.IsValid(refused).ShouldBeFalse(refused);
            Fires(validator, rules, template, refused).ShouldBeTrue(
                $"we refuse {refused}; Peppol's own rule accepts it");
        }
    }

    /// <summary>And the whole invoice satisfies the Swedish rules, which travel inside the Peppol ones.</summary>
    [Fact]
    public void AnInvoiceThisLibraryWritesSatisfiesTheSwedishRules()
    {
        ValidationReport report = new SchematronValidator().Validate(Sverige.Write(AnInvoice()), PeppolRules());

        report.IsValid.ShouldBeTrue(
            string.Join(
                Environment.NewLine,
                report.OfAtLeast(RuleSeverity.Error).Select(message => $"  {message.RuleIdentifier}: {message.Message}")));
    }

    [Fact]
    public void WhatItWritesItReadsBack() =>
        Sverige.Read(Sverige.Write(AnInvoice())).RequireInvoice().Number.Value.ShouldBe("2026-0001");

    [Fact]
    public void TheWholeLibraryStaysReachable() => Sverige.Library.Ubl.Syntax.ShouldBe(DocumentSyntax.Ubl);

    /// <summary>Numbers a Swedish register would not have issued: wrong length, wrong check digit, letters.</summary>
    private static IEnumerable<string> Refused =>
        ["556000000", "55600000000", NotAnOrganisationNumber, "55600000A0"];

    /// <summary>A number one digit away from a real one — the typo the check digit exists to catch.</summary>
    private static string NotAnOrganisationNumber { get; } =
        SeOrganisationNumberTests.ValidNumbers[0][..^1]
        + (SeOrganisationNumberTests.ValidNumbers[0][^1] == '9' ? '0' : (char)(SeOrganisationNumberTests.ValidNumbers[0][^1] + 1));

    private static string Seller => SeOrganisationNumberTests.ValidNumbers[0];

    private static string Buyer => SeOrganisationNumberTests.ValidNumbers[1];

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
            .Any(message => message.RuleIdentifier == "PEPPOL-COMMON-R049");
    }

    private static SchematronRuleSet PeppolRules()
    {
        string path = Path.Combine(RepositoryRoot(), "specs", "peppol", "rules", "PEPPOL-EN16931-UBL.sch");

        Assert.SkipWhen(
            !File.Exists(path),
            "The Peppol artefacts are not present; run build/fetch-specs.sh peppol.");

        return SchematronRuleSet.Load(File.ReadAllText(path), "Peppol BIS Billing 3.0 (UBL)", "3.0");
    }

    private static EInvoice AnInvoice() => Sverige.Invoice()
        .WithNumber("2026-0001")
        .IssuedOn(new DateOnly(2026, 9, 1))
        .DueOn(new DateOnly(2026, 10, 1))
        .WithBuyerReference("REF-2026-0001")            // BT-10, which Peppol requires and EN 16931 does not
        .From(seller => Sverige.Describe(seller, Seller, "Leverantör AB")
            .WithAddress(address =>
            {
                address.Line1 = "Drottninggatan 1";
                address.City = "Stockholm";
                address.PostCode = "11151";
                address.CountryCode = "SE";
            }))
        .To(buyer => Sverige.Describe(buyer, Buyer, "Kund AB")
            .WithAddress(address =>
            {
                address.Line1 = "Avenyn 2";
                address.City = "Göteborg";
                address.PostCode = "41136";
                address.CountryCode = "SE";
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
            MeansTypeCode = "30",
            CreditTransfers = { new CreditTransfer { AccountIdentifier = "SE4550000000058398257466" } },
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
