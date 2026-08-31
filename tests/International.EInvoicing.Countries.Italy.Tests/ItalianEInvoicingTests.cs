using International.EInvoicing.Building;
using International.EInvoicing.Countries.Italy.Identifiers;
using International.EInvoicing.Model;
using International.EInvoicing.Peppol;
using International.EInvoicing.Profiles;
using International.EInvoicing.Validation;
using International.EInvoicing.Validation.Schematron;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Countries.Italy.Tests;

/// <summary>
/// What the Italian shortcut promises, and the second opinion behind it.
/// </summary>
/// <remarks>
/// Italy is the first country here on <b>PINT</b> rather than BIS Billing, so what matters most is that
/// it declares the right family: the profile and the business process are both different strings, and
/// getting one right with the other wrong is the failure mode.
/// </remarks>
public class ItalianEInvoicingTests
{
    private static readonly ItalianEInvoicing Italy = ItalianEInvoicing.Create();

    [Fact]
    public void AnInvoiceBuiltHereDeclaresPeppolBilling()
    {
        EInvoice invoice = AnInvoice();

        invoice.SpecificationIdentifier.Value.ShouldBe(ItProfiles.PeppolBillingUbl.Id.Value);
        invoice.BusinessProcessType.Value.ShouldBe(PeppolBusinessProcess.Billing);
        invoice.CurrencyCode.Value.ShouldBe("EUR");
    }

    [Fact]
    public void ThePartitaIvaIsWrittenInTheSchemePeppolReservesForIt()
    {
        EInvoice invoice = AnInvoice();

        invoice.Seller!.ElectronicAddress.SchemeId.ShouldBe("0211");
        invoice.Seller.ElectronicAddress.Value.ShouldBe(Seller);

        Should.Throw<FormatException>(
            () => Italy.Invoice().From(seller => Italy.Describe(seller, "12345678901", "Sbagliato Srl")));
    }

    /// <summary>
    /// The measurement. Our check-digit implementation is a transcription of a rule Peppol publishes, and a
    /// transcription is worth what the comparison nobody ran is worth — so run it: every ABN we accept must
    /// survive <c>PEPPOL-COMMON-R047</c>, and every one we refuse must not.
    /// </summary>
    [Fact]
    public void OurCheckAgreesWithTheRulePeppolPublishes()
    {
        SchematronRuleSet rules = PeppolRules();
        var validator = new SchematronValidator();

        foreach (string accepted in ItPartitaIvaTests.ValidNumbers)
        {
            Fires(validator, rules, "IT" + accepted).ShouldBeFalse(
                $"we accept {accepted}; Peppol's own rule rejects it");
        }

        foreach (string refused in Refused)
        {
            ItPartitaIva.IsValid(refused).ShouldBeFalse(refused);
            Fires(validator, rules, "IT" + refused).ShouldBeTrue(
                $"we refuse {refused}; Peppol's own rule accepts it");
        }
    }

    /// <summary>The whole invoice, in front of the Peppol rules the Italian ones travel inside.</summary>
    [Fact]
    public void AnInvoiceThisLibraryWritesSatisfiesTheItalianRules()
    {
        ValidationReport report = new SchematronValidator()
            .Validate(Italy.Write(AnInvoice(), DocumentFormat.Ubl), PeppolRules());

        report.IsValid.ShouldBeTrue(
            string.Join(
                Environment.NewLine,
                report.OfAtLeast(RuleSeverity.Error).Select(m => $"  {m.RuleIdentifier}: {m.Message}")));
    }

    [Fact]
    public void WhatItWritesItReadsBack() =>
        Italy.Read(Italy.Write(AnInvoice())).RequireInvoice().Number.Value.ShouldBe("2026-0001");

    [Fact]
    public void TheWholeLibraryStaysReachable() => Italy.Library.Ubl.Syntax.ShouldBe(DocumentSyntax.Ubl);

    /// <summary>Numbers the Italian Business Register would not have issued.</summary>
    private static IEnumerable<string> Refused =>
        ["12345678901", "00000000001", "1234567890", "123456789012"];

    /// <summary>
    /// The seller, with an endpoint the caller may override so a candidate number can be measured where the
    /// rule actually looks at it.
    /// </summary>
    private static PartyBuilder Describe(PartyBuilder party, string? endpoint) =>
        endpoint is null
            ? Italy.Describe(party, Seller, "Fornitore Srl")
            : Italy.Describe(party, Seller, "Fornitore Srl")
                .WithElectronicAddress(endpoint, ItPartitaIva.Scheme);

    private static string Seller => ItPartitaIvaTests.ValidNumbers[0];

    private static string Buyer => ItPartitaIvaTests.ValidNumbers[1];

    /// <summary>
    /// Whether Peppol's Italian rule objects to a value, judged on a document built to carry exactly it.
    /// </summary>
    private static bool Fires(SchematronValidator validator, SchematronRuleSet rules, string endpoint)
    {
        string xml = Italy.Write(Fill(Italy.Invoice(), endpoint), DocumentFormat.Ubl);

        return validator.Validate(xml, rules).Messages
            .Any(message => message.RuleIdentifier == "PEPPOL-COMMON-R047");
    }

    private static SchematronRuleSet PeppolRules()
    {
        string path = Path.Combine(RepositoryRoot(), "specs", "peppol", "rules", "PEPPOL-EN16931-UBL.sch");

        Assert.SkipWhen(
            !File.Exists(path),
            "The Peppol artefacts are not present; run build/fetch-specs.sh peppol.");

        return SchematronRuleSet.Load(File.ReadAllText(path), "Peppol BIS Billing 3.0 (UBL)", "3.0");
    }

    private static EInvoice AnInvoice() => Fill(Italy.Invoice(), endpoint: null);

    private static EInvoice Fill(EInvoiceBuilder builder, string? endpoint) => builder
        .WithNumber("2026-0001")
        .IssuedOn(new DateOnly(2026, 9, 1))
        .DueOn(new DateOnly(2026, 10, 1))
        .OfType("380")
        .WithBuyerReference("REF-2026-0001")
        .From(seller => Describe(seller, endpoint)
            .WithVatIdentifier("AU" + Seller)
            .WithAddress(address =>
            {
                address.Line1 = "Via Roma 1";
                address.City = "Milano";
                address.PostCode = "20121";
                address.CountryCode = "IT";
            }))
        .To(buyer => Italy.Describe(buyer, Buyer, "Cliente Srl")
            .WithAddress(address =>
            {
                address.Line1 = "Via Dante 2";
                address.City = "Torino";
                address.PostCode = "10122";
                address.CountryCode = "IT";
            }))
        .AddLine(line => line
            .WithIdentifier("1")
            .WithItem("Consulting")
            .WithQuantity(1m, "C62")
            .WithNetPrice(1000m)
            .WithNetAmount(1000m)
            .WithVat("S", 22m))
        .Extend(invoice => invoice.Payment = new PaymentInstructions
        {
            MeansTypeCode = "30",
            CreditTransfers = { new CreditTransfer { AccountIdentifier = "IT60X0542811101000000123456" } },
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
