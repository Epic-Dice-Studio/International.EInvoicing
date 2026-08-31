using International.EInvoicing.Building;
using International.EInvoicing.Configuration;
using International.EInvoicing.Countries.Netherlands.Identifiers;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;
using International.EInvoicing.Validation;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Countries.Netherlands.Tests;

/// <summary>
/// NLCIUS — the Dutch national CIUS — and the rules that judge it.
/// </summary>
/// <remarks>
/// This package shipped without NLCIUS at first, on the grounds that its specification identifier was in no
/// artefact the repository held. That was true of the artefacts it held <em>then</em>. The identifier is in
/// the published Dutch rule set, which is now fetched, so it is read from there like every other identifier
/// here — and the rules that come with it are run.
/// </remarks>
public class NlciusTests
{
    private static readonly string Artefacts =
        Path.Combine(RepositoryRoot(), "specs", "national", "simplerinvoicing", "schematron");

    [Fact]
    public void TheIdentifierIsTheOneThePublishedRulesTest()
    {
        Assert.SkipWhen(!Directory.Exists(Artefacts), "run build/fetch-specs.sh national");

        string rules = File.ReadAllText(Directory
            .EnumerateFiles(Path.Combine(Artefacts, "simplerinvoicing"), "si-ubl-2.0*.xslt")
            .Order(StringComparer.Ordinal)
            .Last());

        rules.ShouldContain(NlProfiles.NlciusUbl.Id.Value);
        NlProfiles.NlciusUbl.Id.Value.ShouldBe("urn:cen.eu:en16931:2017#compliant#urn:fdc:nen.nl:nlcius:v1.0");
    }

    /// <summary>The measurement: an NLCIUS invoice this library writes, judged by the Dutch rules.</summary>
    [Fact]
    public void AnInvoiceThisLibraryWritesSatisfiesTheDutchRules()
    {
        Assert.SkipWhen(!Directory.Exists(Artefacts), "run build/fetch-specs.sh national");

        EInvoicing library = EInvoicing.Create(dutch => dutch
            .AddDefaults()
            .AddNetherlands()
            .AddNlciusRulesFrom(Artefacts));

        ValidationReport report = library.Validate(library.Write(AnInvoice()));

        report.RuleSets.ShouldContain(outcome => outcome.Name.StartsWith("NLCIUS", StringComparison.Ordinal) && outcome.Ran);
        report.IsValid.ShouldBeTrue(
            string.Join(
                Environment.NewLine,
                report.Errors.Select(message => $"  {message.RuleIdentifier}: {message.Message}")));
    }

    private static EInvoice AnInvoice() => EInvoiceBuilder
        .Create(NlProfiles.NlciusUbl)
        .WithNumber("2026-0001")
        .IssuedOn(new DateOnly(2026, 9, 1))
        .DueOn(new DateOnly(2026, 10, 1))
        .OfType("380")
        .InCurrency("EUR")
        .WithBuyerReference("REF-2026-0001")
        .From(seller => seller
            .Named("Leverancier BV")
            .WithVatIdentifier("NL123456789B01")
            .WithLegalRegistration("12345678", NlLegalIdentifier.Kvk)
            .WithElectronicAddress("12345678", NlLegalIdentifier.Kvk)
            .WithAddress(address =>
            {
                address.Line1 = "Damrak 1";
                address.City = "Amsterdam";
                address.PostCode = "1012LG";
                address.CountryCode = "NL";
            }))
        .To(buyer => buyer
            .Named("Klant BV")
            .WithLegalRegistration("87654321", NlLegalIdentifier.Kvk)
            .WithElectronicAddress("87654321", NlLegalIdentifier.Kvk)
            .WithAddress(address =>
            {
                address.Line1 = "Coolsingel 2";
                address.City = "Rotterdam";
                address.PostCode = "3011AD";
                address.CountryCode = "NL";
            }))
        .AddLine(line => line
            .WithIdentifier("1")
            .WithItem("Advies")
            .WithQuantity(1m, "C62")
            .WithNetPrice(1000m)
            .WithNetAmount(1000m)
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
