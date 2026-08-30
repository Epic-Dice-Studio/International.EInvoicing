using International.EInvoicing.Building;
using International.EInvoicing.Countries.Norway.Identifiers;
using International.EInvoicing.Model;
using International.EInvoicing.Peppol;
using International.EInvoicing.Profiles;
using International.EInvoicing.Validation;
using International.EInvoicing.Validation.Schematron;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Countries.Norway.Tests;

/// <summary>
/// What the Norwegian shortcut promises, and the second opinion that makes the promise worth something:
/// Peppol's own rules, run over what this library writes.
/// </summary>
public class NorwegianEInvoicingTests
{
    private static readonly NorwegianEInvoicing Norge = NorwegianEInvoicing.Create();

    [Fact]
    public void AnInvoiceBuiltHereDeclaresEhfAndItsBusinessProcess()
    {
        EInvoice invoice = AnInvoice();

        invoice.SpecificationIdentifier.Value.ShouldBe(NoProfiles.Ehf3Ubl.Id.Value);
        invoice.SpecificationIdentifier.Value.ShouldContain("difi.no:ehf:ver3.0");
        invoice.BusinessProcessType.Value.ShouldBe(PeppolBusinessProcess.Billing);
        invoice.CurrencyCode.Value.ShouldBe("NOK");
    }

    [Fact]
    public void TheOrganisationNumberIsWrittenInTheSchemePeppolReservesForIt()
    {
        EInvoice invoice = AnInvoice();

        invoice.Seller!.ElectronicAddress.SchemeId.ShouldBe("0192");
        invoice.Seller.ElectronicAddress.Value.ShouldBe(Seller);
        invoice.Seller.VatIdentifier.Value.ShouldBe("NO" + Seller + "MVA");

        Should.Throw<FormatException>(
            () => Norge.Invoice().From(seller => Norge.Describe(seller, "123456789", "Feil AS")));
    }

    /// <summary>
    /// The measurement. Our modulo 11 check is a transcription of a rule Peppol publishes, and a
    /// transcription is worth exactly as much as the comparison nobody ran — so run it: every number we
    /// accept must survive <c>PEPPOL-COMMON-R041</c>, and every number we refuse must not.
    /// </summary>
    [Fact]
    public void OurCheckAgreesWithTheRulePeppolPublishes()
    {
        SchematronRuleSet rules = PeppolRules();
        var validator = new SchematronValidator();
        string template = Norge.Write(AnInvoice());

        foreach (string accepted in NoOrganisationNumberTests.ValidNumbers)
        {
            Fires(validator, rules, template, accepted).ShouldBeFalse(
                $"we accept {accepted}; Peppol's own rule rejects it");
        }

        foreach (string refused in Refused)
        {
            NoOrganisationNumber.IsValid(refused).ShouldBeFalse(refused);
            Fires(validator, rules, template, refused).ShouldBeTrue(
                $"we refuse {refused}; Peppol's own rule accepts it");
        }
    }

    /// <summary>And the whole invoice satisfies the Norwegian rules, which travel inside the Peppol ones.</summary>
    [Fact]
    public void AnInvoiceThisLibraryWritesSatisfiesTheNorwegianRules()
    {
        ValidationReport report = new SchematronValidator().Validate(Norge.Write(AnInvoice()), PeppolRules());

        report.IsValid.ShouldBeTrue(
            string.Join(
                Environment.NewLine,
                report.OfAtLeast(RuleSeverity.Error).Select(message => $"  {message.RuleIdentifier}: {message.Message}")));
    }

    [Fact]
    public void WhatItWritesItReadsBack() =>
        Norge.Read(Norge.Write(AnInvoice())).RequireInvoice().Number.Value.ShouldBe("2026-0001");

    [Fact]
    public void TheWholeLibraryStaysReachable() => Norge.Library.Ubl.Syntax.ShouldBe(DocumentSyntax.Ubl);

    /// <summary>Numbers a Norwegian register would not have issued: wrong length, wrong check digit, letters.</summary>
    private static IEnumerable<string> Refused =>
        ["12345678", "1234567890", "910000005", "91000000A"];

    private static string Seller => NoOrganisationNumberTests.ValidNumbers[0];

    private static string Buyer => NoOrganisationNumberTests.ValidNumbers[1];

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
            .Any(message => message.RuleIdentifier == "PEPPOL-COMMON-R041");
    }

    private static SchematronRuleSet PeppolRules()
    {
        string path = Path.Combine(RepositoryRoot(), "specs", "peppol", "rules", "PEPPOL-EN16931-UBL.sch");

        Assert.SkipWhen(
            !File.Exists(path),
            "The Peppol artefacts are not present; run build/fetch-specs.sh peppol.");

        return SchematronRuleSet.Load(File.ReadAllText(path), "Peppol BIS Billing 3.0 (UBL)", "3.0");
    }

    private static EInvoice AnInvoice() => Norge.Invoice()
        .WithNumber("2026-0001")
        .IssuedOn(new DateOnly(2026, 9, 1))
        .DueOn(new DateOnly(2026, 10, 1))
        .WithBuyerReference("REF-2026-0001")            // BT-10, which Peppol requires and EN 16931 does not
        .From(seller => Norge.Describe(seller, Seller, "Leverandør AS")
            .WithAddress(address =>
            {
                address.Line1 = "Karl Johans gate 1";
                address.City = "Oslo";
                address.PostCode = "0154";
                address.CountryCode = "NO";
            }))
        .To(buyer => Norge.Describe(buyer, Buyer, "Kunde AS")
            .WithAddress(address =>
            {
                address.Line1 = "Torgallmenningen 2";
                address.City = "Bergen";
                address.PostCode = "5014";
                address.CountryCode = "NO";
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
            CreditTransfers = { new CreditTransfer { AccountIdentifier = "NO9386011117947" } },
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
