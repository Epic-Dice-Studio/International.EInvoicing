using International.EInvoicing.Building;
using International.EInvoicing.Countries.Netherlands.Identifiers;
using International.EInvoicing.Model;
using International.EInvoicing.Peppol;
using International.EInvoicing.Profiles;
using International.EInvoicing.Validation;
using International.EInvoicing.Validation.Schematron;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Countries.Netherlands.Tests;

/// <summary>
/// What the Dutch shortcut promises, held to the Dutch rules themselves.
/// </summary>
/// <remarks>
/// The Dutch rules are unusually easy to fall foul of: <c>NL-R-003</c> and <c>NL-R-005</c> reject an invoice
/// whose parties are named perfectly but whose legal entity identifiers carry no scheme. An invoice this
/// library writes has to survive them, and that is measured here rather than asserted.
/// </remarks>
public class DutchEInvoicingTests
{
    private static readonly DutchEInvoicing Nederland = DutchEInvoicing.Create();

    [Fact]
    public void AnInvoiceBuiltHereDeclaresPeppolBillingAndItsBusinessProcess()
    {
        EInvoice invoice = AnInvoice();

        invoice.SpecificationIdentifier.Value.ShouldBe(NlProfiles.PeppolBillingUbl.Id.Value);
        invoice.BusinessProcessType.Value.ShouldBe(PeppolBusinessProcess.Billing);
        invoice.CurrencyCode.Value.ShouldBe("EUR");
    }

    [Fact]
    public void TheLegalEntityIdentifierCarriesTheSchemeTheDutchRulesDemand()
    {
        EInvoice invoice = AnInvoice();

        invoice.Seller!.LegalRegistrationIdentifier.SchemeId.ShouldBe(NlLegalIdentifier.Kvk);
        invoice.Buyer!.LegalRegistrationIdentifier.SchemeId.ShouldBe(NlLegalIdentifier.Kvk);
    }

    [Fact]
    public void ASchemeTheDutchRulesDoNotAcceptIsRefusedHere()
    {
        Should.Throw<ArgumentException>(
            () => Nederland.Invoice().From(seller => Nederland.Describe(seller, "12345678", "0088", "Fout BV")));

        NlLegalIdentifier.IsAccepted(NlLegalIdentifier.Oin).ShouldBeTrue();
        NlLegalIdentifier.IsAccepted("0088").ShouldBeFalse();
    }

    /// <summary>The measurement: the Dutch rules themselves, over an invoice this library wrote.</summary>
    [Fact]
    public void AnInvoiceThisLibraryWritesSatisfiesTheDutchRules()
    {
        ValidationReport report = new SchematronValidator().Validate(Nederland.Write(AnInvoice()), PeppolRules());

        report.IsValid.ShouldBeTrue(
            string.Join(
                Environment.NewLine,
                report.OfAtLeast(RuleSeverity.Error).Select(message => $"  {message.RuleIdentifier}: {message.Message}")));
    }

    /// <summary>And dropping the scheme is exactly what the Dutch rules reject.</summary>
    [Fact]
    public void DroppingTheSchemeIsWhatTheDutchRulesReject()
    {
        string xml = Nederland.Write(AnInvoice()).Replace(
            $" schemeID=\"{NlLegalIdentifier.Kvk}\"",
            string.Empty,
            StringComparison.Ordinal);

        new SchematronValidator().Validate(xml, PeppolRules()).Messages
            .Select(message => message.RuleIdentifier)
            .ShouldContain("NL-R-003");
    }

    [Fact]
    public void WhatItWritesItReadsBack() =>
        Nederland.Read(Nederland.Write(AnInvoice())).RequireInvoice().Number.Value.ShouldBe("2026-0001");

    [Fact]
    public void TheWholeLibraryStaysReachable() => Nederland.Library.Ubl.Syntax.ShouldBe(DocumentSyntax.Ubl);

    private static SchematronRuleSet PeppolRules()
    {
        string path = Path.Combine(RepositoryRoot(), "specs", "peppol", "rules", "PEPPOL-EN16931-UBL.sch");

        Assert.SkipWhen(
            !File.Exists(path),
            "The Peppol artefacts are not present; run build/fetch-specs.sh peppol.");

        return SchematronRuleSet.Load(File.ReadAllText(path), "Peppol BIS Billing 3.0 (UBL)", "3.0");
    }

    private static EInvoice AnInvoice() => Nederland.Invoice()
        .WithNumber("2026-0001")
        .IssuedOn(new DateOnly(2026, 9, 1))
        .DueOn(new DateOnly(2026, 10, 1))
        .WithBuyerReference("REF-2026-0001")
        .From(seller => Nederland.Describe(seller, "12345678", "Leverancier BV")
            .WithVatIdentifier("NL123456789B01")
            .WithAddress(address =>
            {
                address.Line1 = "Damrak 1";                    // NL-R-002 wants street, city and postcode
                address.City = "Amsterdam";
                address.PostCode = "1012LG";
                address.CountryCode = "NL";
            }))
        .To(buyer => Nederland.Describe(buyer, "87654321", "Klant BV")
            .WithAddress(address =>
            {
                address.Line1 = "Coolsingel 2";                // NL-R-004, the same for the customer
                address.City = "Rotterdam";
                address.PostCode = "3011AD";
                address.CountryCode = "NL";
            }))
        .AddLine(line => line
            .WithIdentifier("1")
            .WithItem("Advies")
            .WithQuantity(3m, "HUR")
            .WithNetPrice(1000m)
            .WithNetAmount(3000m)
            .WithVat("S", 21m))
        .Extend(invoice => invoice.Payment = new PaymentInstructions
        {
            MeansTypeCode = "30",
            CreditTransfers = { new CreditTransfer { AccountIdentifier = "NL91ABNA0417164300" } },
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
