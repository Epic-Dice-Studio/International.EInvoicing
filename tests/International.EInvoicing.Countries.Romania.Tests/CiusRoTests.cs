using International.EInvoicing.Building;
using International.EInvoicing.Configuration;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;
using International.EInvoicing.Validation;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Countries.Romania.Tests;

/// <summary>
/// CIUS-RO, the national CIUS the Romanian <em>e-Factura</em> mandate exchanges, and the rules that judge it.
/// </summary>
/// <remarks>
/// Romania publishes 244 assertions on top of EN 16931 — the largest national rule set this library has met
/// after Germany's. What is measured here is that an invoice this library writes survives them.
/// </remarks>
public class CiusRoTests
{
    private static readonly string Artefacts =
        Path.Combine(RepositoryRoot(), "specs", "national", "cius-ro", "schematron");

    [Fact]
    public void TheIdentifierIsTheOneThePublishedRulesTest()
    {
        Assert.SkipWhen(!Directory.Exists(Artefacts), "run build/fetch-specs.sh national");

        string rules = string.Concat(Directory
            .EnumerateFiles(Artefacts, "*.xslt", SearchOption.AllDirectories)
            .Select(File.ReadAllText));

        rules.ShouldContain("urn:efactura.mfinante.ro:CIUS-RO:");
        RoProfiles.CiusRoUbl.Id.Value
            .ShouldBe("urn:cen.eu:en16931:2017#compliant#urn:efactura.mfinante.ro:CIUS-RO:1.0.1");
    }

    /// <summary>The rule nobody expects: Bucharest is addressed by sector, not by name.</summary>
    [Fact]
    public void BucharestIsAddressedBySector()
    {
        RoBucharestSector.IsSector("Bucuresti").ShouldBeFalse();
        RoBucharestSector.IsSector(RoBucharestSector.Of(3)).ShouldBeTrue();
        RoBucharestSector.All.Count.ShouldBe(6);
        Should.Throw<ArgumentOutOfRangeException>(() => RoBucharestSector.Of(7));
    }

    [Fact]
    public void AnInvoiceThisLibraryWritesSatisfiesTheRomanianRules()
    {
        Assert.SkipWhen(!Directory.Exists(Artefacts), "run build/fetch-specs.sh national");

        EInvoicing library = EInvoicing.Create(romania => romania
            .AddDefaults()
            .AddRomania()
            .AddRomanianRulesFrom(Artefacts));

        ValidationReport report = library.Validate(library.Write(AnInvoice()));

        report.RuleSets.ShouldContain(outcome => outcome.Name.StartsWith("CIUS-RO", StringComparison.Ordinal) && outcome.Ran);
        report.IsValid.ShouldBeTrue(
            string.Join(
                Environment.NewLine,
                report.Errors.Take(8).Select(message => $"  {message.RuleIdentifier}: {message.Message}")));
    }

    private static EInvoice AnInvoice() => EInvoiceBuilder
        .Create(RoProfiles.CiusRoUbl)
        .WithNumber("2026-0001")
        .IssuedOn(new DateOnly(2026, 9, 1))
        .DueOn(new DateOnly(2026, 10, 1))
        .OfType("380")
        .InCurrency("RON")
        .WithBuyerReference("REF-2026-0001")
        .From(seller => seller
            .Named("Furnizor SRL")
            .WithVatIdentifier("RO12345678")
            .WithLegalRegistration("J40/1234/2020")
            .WithElectronicAddress("RO12345678", "9947")
            .WithAddress(address =>
            {
                address.Line1 = "Strada Victoriei 1";
                // BR-RO-100: in Bucharest the city name must be the sector, not the city.
                address.City = RoBucharestSector.Of(1);
                address.CountrySubdivision = RoBucharestSector.Subdivision;
                address.PostCode = "010061";
                address.CountryCode = "RO";
            }))
        .To(buyer => buyer
            .Named("Client SRL")
            .WithVatIdentifier("RO87654321")
            .WithElectronicAddress("RO87654321", "9947")
            .WithAddress(address =>
            {
                address.Line1 = "Strada Republicii 2";
                address.City = "Cluj-Napoca";
                address.CountrySubdivision = "RO-CJ";
                address.PostCode = "400015";
                address.CountryCode = "RO";
            }))
        .AddLine(line => line
            .WithIdentifier("1")
            .WithItem("Consultanta")
            .WithQuantity(1m, "C62")
            .WithNetPrice(1000m)
            .WithNetAmount(1000m)
            .WithVat("S", 19m))
        .Extend(invoice => invoice.Payment = new PaymentInstructions
        {
            MeansTypeCode = "30",
            CreditTransfers = { new CreditTransfer { AccountIdentifier = "RO49AAAA1B31007593840000" } },
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
