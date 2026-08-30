using International.EInvoicing.Building;
using International.EInvoicing.Countries.Iceland.Identifiers;
using International.EInvoicing.Model;
using International.EInvoicing.Peppol;
using International.EInvoicing.Profiles;
using International.EInvoicing.Validation;
using International.EInvoicing.Validation.Schematron;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Countries.Iceland.Tests;

/// <summary>
/// What the Icelandic shortcut promises, held to the Icelandic rules themselves.
/// </summary>
/// <remarks>
/// The Icelandic rules are unusually easy to fall foul of: <c>IS-R-002</c> and <c>IS-R-004</c> reject an invoice
/// whose parties are named perfectly but whose legal entity identifiers carry no scheme. An invoice this
/// library writes has to survive them, and that is measured here rather than asserted.
/// </remarks>
public class IcelandicEInvoicingTests
{
    private static readonly IcelandicEInvoicing Island = IcelandicEInvoicing.Create();

    [Fact]
    public void AnInvoiceBuiltHereDeclaresPeppolBillingAndItsBusinessProcess()
    {
        EInvoice invoice = AnInvoice();

        invoice.SpecificationIdentifier.Value.ShouldBe(IsProfiles.PeppolBillingUbl.Id.Value);
        invoice.BusinessProcessType.Value.ShouldBe(PeppolBusinessProcess.Billing);
        invoice.CurrencyCode.Value.ShouldBe("ISK");
    }

    [Fact]
    public void TheLegalEntityIdentifierCarriesTheSchemeTheIcelandicRulesDemand()
    {
        EInvoice invoice = AnInvoice();

        invoice.Seller!.LegalRegistrationIdentifier.SchemeId.ShouldBe(IsKennitala.Scheme);
        invoice.Buyer!.LegalRegistrationIdentifier.SchemeId.ShouldBe(IsKennitala.Scheme);
    }

    [Fact]
    public void ANumberThatIsNotAKennitalaIsRefusedHere() =>
        Should.Throw<FormatException>(
            () => Island.Invoice().From(seller => Island.Describe(seller, "1234567890", "Rangt ehf")));

    /// <summary>The measurement: the Icelandic rules themselves, over an invoice this library wrote.</summary>
    [Fact]
    public void AnInvoiceThisLibraryWritesSatisfiesTheIcelandicRules()
    {
        ValidationReport report = new SchematronValidator().Validate(Island.Write(AnInvoice()), PeppolRules());

        report.IsValid.ShouldBeTrue(
            string.Join(
                Environment.NewLine,
                report.OfAtLeast(RuleSeverity.Error).Select(message => $"  {message.RuleIdentifier}: {message.Message}")));
    }

    /// <summary>And dropping the scheme is exactly what the Icelandic rules reject.</summary>
    [Fact]
    public void DroppingTheSchemeIsWhatTheIcelandicRulesReject()
    {
        string xml = Island.Write(AnInvoice()).Replace(
            $" schemeID=\"{IsKennitala.Scheme}\"",
            string.Empty,
            StringComparison.Ordinal);

        new SchematronValidator().Validate(xml, PeppolRules()).Messages
            .Select(message => message.RuleIdentifier)
            .ShouldContain("IS-R-002");
    }

    [Fact]
    public void WhatItWritesItReadsBack() =>
        Island.Read(Island.Write(AnInvoice())).RequireInvoice().Number.Value.ShouldBe("2026-0001");

    [Fact]
    public void TheWholeLibraryStaysReachable() => Island.Library.Ubl.Syntax.ShouldBe(DocumentSyntax.Ubl);

    private static string Seller => IsKennitalaTests.ValidNumbers[0];

    private static string Buyer => IsKennitalaTests.ValidNumbers[1];

    private static SchematronRuleSet PeppolRules()
    {
        string path = Path.Combine(RepositoryRoot(), "specs", "peppol", "rules", "PEPPOL-EN16931-UBL.sch");

        Assert.SkipWhen(
            !File.Exists(path),
            "The Peppol artefacts are not present; run build/fetch-specs.sh peppol.");

        return SchematronRuleSet.Load(File.ReadAllText(path), "Peppol BIS Billing 3.0 (UBL)", "3.0");
    }

    private static EInvoice AnInvoice() => Island.Invoice()
        .WithNumber("2026-0001")
        .IssuedOn(new DateOnly(2026, 9, 1))
        .DueOn(new DateOnly(2026, 10, 1))
        .WithBuyerReference("REF-2026-0001")
        .From(seller => Island.Describe(seller, Seller, "Seljandi ehf")
            .WithAddress(address =>
            {
                address.Line1 = "Laugavegur 1";
                address.City = "Reykjavík";
                address.PostCode = "101";
                address.CountryCode = "IS";
            }))
        .To(buyer => Island.Describe(buyer, Buyer, "Kaupandi ehf")
            .WithAddress(address =>
            {
                address.Line1 = "Hafnarstræti 2";
                address.City = "Akureyri";
                address.PostCode = "600";
                address.CountryCode = "IS";
            }))
        .AddLine(line => line
            .WithIdentifier("1")
            .WithItem("Ráðgjöf")
            .WithQuantity(3m, "HUR")
            .WithNetPrice(1000m)
            .WithNetAmount(3000m)
            .WithVat("S", 24m))
        .Extend(invoice => invoice.Payment = new PaymentInstructions
        {
            MeansTypeCode = "30",
            CreditTransfers = { new CreditTransfer { AccountIdentifier = "IS14015926076545510730339" } },
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
