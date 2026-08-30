using International.EInvoicing.Building;
using International.EInvoicing.Cii.Writing;
using International.EInvoicing.Countries.Germany.Identifiers;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;
using International.EInvoicing.Ubl.Writing;
using International.EInvoicing.Validation;
using International.EInvoicing.Validation.En16931;
using International.EInvoicing.Validation.Schematron;
using International.EInvoicing.Validation.XRechnung;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Countries.Germany.Tests;

/// <summary>
/// Documents this library <em>produces</em>, put in front of the German rules.
/// </summary>
/// <remarks>
/// The official corpus proves the engine reads what Germany publishes. It says nothing about what this
/// library writes — and writing is where an implementation is most likely to be wrong on its own terms,
/// because nobody else's document is there to disagree with it.
/// </remarks>
public class GeneratedInvoiceTests
{
    public static TheoryData<string> Syntaxes => new("UBL", "CII");

    [Theory]
    [MemberData(nameof(Syntaxes))]
    public void AnXRechnungInvoiceThisLibraryWritesSatisfiesTheGermanRules(string syntax)
    {
        DocumentSyntax which = syntax == "UBL" ? DocumentSyntax.Ubl : DocumentSyntax.Cii;
        string xml = Write(AnXRechnungInvoice(which), syntax);

        ValidationReport german = new SchematronValidator().Validate(xml, XRechnungRules.For(which));

        german.IsValid.ShouldBeTrue(Describe($"XRechnung ({syntax})", german));
    }

    /// <summary>XRechnung restricts EN 16931 rather than replacing it, so the base rules must hold too.</summary>
    [Theory]
    [MemberData(nameof(Syntaxes))]
    public void AndTheBaseRulesItRestricts(string syntax)
    {
        DocumentSyntax which = syntax == "UBL" ? DocumentSyntax.Ubl : DocumentSyntax.Cii;
        string xml = Write(AnXRechnungInvoice(which), syntax);

        ValidationReport report = new SchematronValidator().Validate(xml, En16931Rules.For(which));

        report.IsValid.ShouldBeTrue(Describe($"EN 16931 ({syntax})", report));
    }

    /// <summary>The routing identifier is what carries a German public-sector invoice to the right desk.</summary>
    [Fact]
    public void TheLeitwegIdTravelsAsTheBuyerReference()
    {
        DeLeitwegId routing = DeLeitwegId.Create("04011000", "1234512345");

        EInvoice invoice = AnXRechnungInvoice(DocumentSyntax.Ubl);

        invoice.BuyerReference.Value.ShouldBe(routing.ToString());
        DeLeitwegId.IsValid(invoice.BuyerReference.Value).ShouldBeTrue();
    }

    private static EInvoice AnXRechnungInvoice(DocumentSyntax syntax) => EInvoiceBuilder
        .Create(syntax == DocumentSyntax.Ubl ? DeProfiles.XRechnungUbl : DeProfiles.XRechnungCii)
        .WithNumber("RE-2026-001")
        .IssuedOn(new DateOnly(2026, 9, 1))
        .DueOn(new DateOnly(2026, 10, 1))
        .OfType("380")
        .InCurrency("EUR")
        .WithBuyerReference(DeLeitwegId.Create("04011000", "1234512345").ToString())   // BT-10
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

    private static string Write(EInvoice invoice, string syntax) =>
        syntax == "UBL"
            ? new UblInvoiceWriter().WriteToString(invoice)
            : new CiiInvoiceWriter().WriteToString(invoice);

    private static string Describe(string what, ValidationReport report) =>
        $"{what} rejected the invoice this library wrote:{Environment.NewLine}"
        + string.Join(
            Environment.NewLine,
            report.OfAtLeast(RuleSeverity.Error).Select(message => $"  {message.RuleIdentifier}: {message.Message}"));
}
