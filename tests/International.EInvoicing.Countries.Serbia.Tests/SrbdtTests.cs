using International.EInvoicing.Building;
using International.EInvoicing.Configuration;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;
using International.EInvoicing.Validation;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Countries.Serbia.Tests;

/// <summary>
/// SRBDT, the national CIUS the Serbian <em>e-Factura</em> mandate exchanges, and the rules that judge it.
/// </summary>
/// <remarks>
/// Serbia publishes 134 assertions on top of EN 16931, covering the CIUS and its extension in one artefact.
/// What is measured here is that an invoice this library writes survives them.
/// </remarks>
public class SrbdtTests
{
    private static readonly string Artefacts =
        Path.Combine(RepositoryRoot(), "specs", "national", "serbia", "schematron");

    [Fact]
    public void TheIdentifierIsTheOneThePublishedRulesTest()
    {
        Assert.SkipWhen(!Directory.Exists(Artefacts), "run build/fetch-specs.sh national");

        string rules = string.Concat(Directory
            .EnumerateFiles(Artefacts, "*.xslt", SearchOption.AllDirectories)
            .Select(File.ReadAllText));

        rules.ShouldContain("urn:mfin.gov.rs:srbdt:2022");
        RsProfiles.SrbdtUbl.Id.Value
            .ShouldBe("urn:cen.eu:en16931:2017#compliant#urn:mfin.gov.rs:srbdt:2022");
    }

    [Fact]
    public void AnInvoiceThisLibraryWritesSatisfiesTheSerbianRules()
    {
        Assert.SkipWhen(!Directory.Exists(Artefacts), "run build/fetch-specs.sh national");

        EInvoicing library = EInvoicing.Create(romania => romania
            .AddDefaults()
            .AddSerbia()
            .AddSerbianRulesFrom(Artefacts));

        ValidationReport report = library.Validate(library.Write(AnInvoice()));

        report.RuleSets.ShouldContain(outcome => outcome.Name.StartsWith("SRBDT", StringComparison.Ordinal) && outcome.Ran);
        report.IsValid.ShouldBeTrue(
            string.Join(
                Environment.NewLine,
                report.Errors.Take(8).Select(message => $"  {message.RuleIdentifier}: {message.Message}")));
    }

    private static EInvoice AnInvoice() => EInvoiceBuilder
        .Create(RsProfiles.SrbdtUbl)
        .WithNumber("2026-0001")
        .IssuedOn(new DateOnly(2026, 9, 1))
        .DueOn(new DateOnly(2026, 10, 1))
        .OfType("380")
        .InCurrency("RSD")
        .WithBuyerReference("REF-2026-0001")
        // RSR-05: Serbia requires the tax point date code (BT-8), which EN 16931 leaves optional.
        .Extend(invoice => invoice.TaxPointDateCode = "35")
        .From(seller => seller
            .Named("Dobavljač d.o.o.")
            .WithVatIdentifier("RS123456789")
            .WithLegalRegistration("12345678")
            .WithElectronicAddress("12345678", "9948")
            .WithAddress(address =>
            {
                address.Line1 = "Knez Mihailova 1";
                address.City = "Beograd";
                address.PostCode = "11000";
                address.CountryCode = "RS";
            }))
        .To(buyer => buyer
            .Named("Kupac d.o.o.")
            .WithVatIdentifier("RS987654321")
            .WithElectronicAddress("87654321", "9948")
            .WithAddress(address =>
            {
                address.Line1 = "Zmaj Jovina 2";
                address.City = "Novi Sad";
                address.PostCode = "21000";
                address.CountryCode = "RS";
            }))
        .AddLine(line => line
            .WithIdentifier("1")
            .WithItem("Konsalting")
            .WithQuantity(1m, "C62")
            .WithNetPrice(1000m)
            .WithNetAmount(1000m)
            .WithVat("S", 20m))
        .Extend(invoice => invoice.Payment = new PaymentInstructions
        {
            MeansTypeCode = "30",
            CreditTransfers = { new CreditTransfer { AccountIdentifier = "RS35260005601001611379" } },
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
