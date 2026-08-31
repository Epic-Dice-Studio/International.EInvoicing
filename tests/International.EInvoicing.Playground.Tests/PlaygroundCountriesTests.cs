using International.EInvoicing.Building;
using International.EInvoicing.Model;
using International.EInvoicing.Peppol;
using International.EInvoicing.Playground.Services;
using International.EInvoicing.Profiles;
using International.EInvoicing.Validation;
using International.EInvoicing.Validation.XRechnung;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Playground.Tests;

/// <summary>
/// The published demo, held to the same standard as the library it demonstrates.
/// </summary>
/// <remarks>
/// A playground that hands a visitor an invoice their own country would reject teaches them the wrong thing,
/// and nobody would notice for months. So every country the site offers, in every profile it offers, builds
/// a document here and is put in front of the rules — on every commit.
/// </remarks>
public class PlaygroundCountriesTests
{
    private static readonly EInvoicing Library =
        EInvoicing.Create(library => library.AddDefaults().AddPeppol().AddXRechnungRules());

    public static TheoryData<string, string> Combinations
    {
        get
        {
            var data = new TheoryData<string, string>();

            foreach (PlaygroundCountry country in PlaygroundCountries.All)
            {
                foreach (PlaygroundProfile profile in country.Profiles)
                {
                    data.Add(country.Code, profile.Label);
                }
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(Combinations))]
    public void EveryCountryAndProfileTheSiteOffersProducesAConformingInvoice(string code, string label)
    {
        PlaygroundCountry country = PlaygroundCountries.ByCode(code);
        Profile profile = country.Profiles.First(candidate => candidate.Label == label).Profile;

        string xml = Library.Write(
            AnInvoice(country, profile),
            profile.Syntax == DocumentSyntax.Cii ? DocumentFormat.Cii : DocumentFormat.Ubl);

        ValidationReport report = Library.Validate(xml);

        // A country whose rules cannot run must not pass this vacuously: say so, and check what can be
        // checked — that the document was written at all, and that Describe accepted its identifiers.
        if (report.RuleSets.All(outcome => !outcome.Ran))
        {
            country.RuleSets.ShouldAllBe(
                ruleSet => !ruleSet.Embedded,
                $"{country.Name} claims a rule set this build carries, yet none ran over its invoice");
            xml.ShouldContain(profile.Id.Value);
            return;
        }

        // Describe throws before writing when an identifier fails its country's check, so a sample
        // identifier that stopped satisfying its own check digits fails here too.
        report.IsValid.ShouldBeTrue(
            $"{country.Name} / {label} produced an invoice the rules refuse:{Environment.NewLine}"
            + string.Join(
                Environment.NewLine,
                report.Errors.Select(message => $"  {message.RuleIdentifier}: {message.Message}")));
    }

    private static EInvoice AnInvoice(PlaygroundCountry country, Profile profile) => country
        .Prepare(EInvoiceBuilder.Create(profile))
        .WithNumber("FA-2026-001")
        .IssuedOn(new DateOnly(2026, 9, 1))
        .DueOn(new DateOnly(2026, 10, 1))
        .OfType("380")
        .InCurrency(country.Currency)
        .WithBuyerReference("FA-2026-001")
        .From(seller => WithVat(country.Describe(seller, country.SellerIdentifier, "Seller"), country.SellerVat)
            .WithAddress(address => Fill(address, country, "1 Main Street", "Somewhere", "00000")))
        .To(buyer => WithVat(country.Describe(buyer, country.BuyerIdentifier, "Buyer"), country.BuyerVat)
            .WithAddress(address => Fill(address, country, "2 Other Street", "Elsewhere", "11111")))
        .AddLine(line => line
            .WithIdentifier("1")
            .WithItem("Item")
            .WithQuantity(1m, "C62")
            .WithNetPrice(1000m)
            .WithNetAmount(1000m)
            .WithVat("S", 20m))
        .Extend(invoice => invoice.Payment = new PaymentInstructions
        {
            MeansTypeCode = country.Code == "DK" ? "58" : "30",
            CreditTransfers = { new CreditTransfer { AccountIdentifier = "FR7630001007941234567890185" } },
        })
        .WithComputedVatBreakdown()
        .WithComputedTotals()
        .Build();

    private static PartyBuilder WithVat(PartyBuilder party, string vat) =>
        string.IsNullOrEmpty(vat) ? party : party.WithVatIdentifier(vat);

    private static void Fill(PostalAddress address, PlaygroundCountry country, string line, string city, string post)
    {
        address.Line1 = line;
        address.City = city;
        address.PostCode = post;
        address.CountryCode = country.Code == "--" ? "FR" : country.Code;
    }
}
