using International.EInvoicing.Building;
using International.EInvoicing.Countries.Germany.Identifiers;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;
using International.EInvoicing.Validation;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Countries.Germany.Tests;

/// <summary>
/// What the German shortcut promises: XRechnung out of the box, the German rules already registered, and the
/// Leitweg-ID checked before it reaches a public body rather than after.
/// </summary>
public class GermanEInvoicingTests
{
    private static readonly GermanEInvoicing Germany = GermanEInvoicing.Create();

    [Fact]
    public void AnInvoiceBuiltHereDeclaresXRechnung()
    {
        Germany.Invoice().Build().SpecificationIdentifier.Value.ShouldBe(DeProfiles.XRechnungUbl.Id.Value);
        Germany.Invoice(DocumentSyntax.Cii).Build().SpecificationIdentifier.Value
            .ShouldBe(DeProfiles.XRechnungCii.Id.Value);
    }

    [Fact]
    public void ARoutingIdentifierIsCheckedBeforeItIsWrittenRatherThanAfter()
    {
        EInvoice invoice = Germany.InvoiceToPublicBody("04011000-1234512345-06").Build();

        invoice.BuyerReference.Value.ShouldBe("04011000-1234512345-06");

        Should.Throw<FormatException>(() => Germany.InvoiceToPublicBody("04011000-1234512345-07"));
    }

    /// <summary>The German rules ship with this library, so validating needs nothing fetched.</summary>
    [Fact]
    public void ValidatingRunsTheGermanRulesWithNothingToFetch()
    {
        ValidationReport report = Germany.Validate(Germany.Write(AnInvoice()));

        report.RuleSets.ShouldContain(set => set.Name.Contains("XRechnung", StringComparison.OrdinalIgnoreCase));
        report.IsValid.ShouldBeTrue(Describe(report));
    }

    [Fact]
    public void WhatItWritesItReadsBack()
    {
        DocumentResult read = Germany.Read(Germany.Write(AnInvoice()));

        read.RequireInvoice().Number.Value.ShouldBe("RE-2026-001");
    }

    [Fact]
    public void TheWholeLibraryStaysReachable() => Germany.Library.Ubl.Syntax.ShouldBe(DocumentSyntax.Ubl);

    private static EInvoice AnInvoice() => Germany
        .InvoiceToPublicBody(DeLeitwegId.Create("04011000", "1234512345").ToString())
        .WithNumber("RE-2026-001")
        .IssuedOn(new DateOnly(2026, 9, 1))
        .DueOn(new DateOnly(2026, 10, 1))
        .OfType("380")
        .From(seller => seller
            .Named("Epic Dice Studio GmbH")
            .WithVatIdentifier("DE123456789")
            .WithElectronicAddress("seller@example.de", "EM")
            .WithContact(contact =>
            {
                contact.Name = "Rechnungsstelle";
                contact.Telephone = "+49 30 123456";
                contact.Email = "rechnung@example.de";
            })
            .WithAddress(address =>
            {
                address.Line1 = "Musterstraße 1";
                address.City = "Berlin";
                address.PostCode = "10115";
                address.CountryCode = "DE";
            }))
        .To(buyer => buyer
            .Named("Behörde")
            .WithElectronicAddress("buyer@example.de", "EM")
            .WithAddress(address =>
            {
                address.Line1 = "Amtsweg 2";
                address.City = "Bonn";
                address.PostCode = "53113";
                address.CountryCode = "DE";
            }))
        .AddLine(line => line
            .WithIdentifier("1")
            .WithItem("Beratung")
            .WithQuantity(3m, "HUR")
            .WithNetPrice(150m)
            .WithNetAmount(450m)
            .WithVat("S", 19m))
        .Extend(invoice => invoice.Payment = new PaymentInstructions
        {
            MeansTypeCode = "58",
            CreditTransfers = { new CreditTransfer { AccountIdentifier = "DE02120300000000202051" } },
        })
        .WithComputedVatBreakdown()
        .WithComputedTotals()
        .Build();

    private static string Describe(ValidationReport report) =>
        string.Join(
            Environment.NewLine,
            report.OfAtLeast(RuleSeverity.Error).Select(message => $"  {message.RuleIdentifier}: {message.Message}"));
}
