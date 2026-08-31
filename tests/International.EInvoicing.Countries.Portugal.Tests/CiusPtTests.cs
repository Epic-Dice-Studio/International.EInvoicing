using International.EInvoicing.Building;
using International.EInvoicing.Configuration;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;
using International.EInvoicing.Validation;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Countries.Portugal.Tests;

/// <summary>
/// CIUS-PT, the national CIUS the Portuguese <em>e-Factura</em> mandate exchanges, and the rules that judge it.
/// </summary>
/// <remarks>
/// Portugal publishes the largest artefact this library has met — over two thousand assertions, because
/// CIUS-PT bundles the EN 16931 UBL rules alongside its own. What is measured here is that an invoice this
/// library writes survives all of them.
/// </remarks>
public class CiusPtTests
{
    private static readonly string Artefacts =
        Path.Combine(RepositoryRoot(), "specs", "national", "cius-pt", "schematron");

    [Fact]
    public void TheIdentifierIsTheOneThePublishedRulesTest()
    {
        Assert.SkipWhen(!Directory.Exists(Artefacts), "run build/fetch-specs.sh national");

        string rules = string.Concat(Directory
            .EnumerateFiles(Artefacts, "*.xslt", SearchOption.AllDirectories)
            .Select(File.ReadAllText));

        rules.ShouldContain("urn:feap.gov.pt:CIUS-PT:");
        PtProfiles.CiusPtUbl.Id.Value
            .ShouldBe("urn:cen.eu:en16931:2017#compliant#urn:feap.gov.pt:CIUS-PT:2.1.1");
    }

    [Fact]
    public void AnInvoiceThisLibraryWritesSatisfiesThePortugueseRules()
    {
        Assert.SkipWhen(!Directory.Exists(Artefacts), "run build/fetch-specs.sh national");

        EInvoicing library = EInvoicing.Create(romania => romania
            .AddDefaults()
            .AddPortugal()
            .AddPortugueseRulesFrom(Artefacts));

        ValidationReport report = library.Validate(library.Write(AnInvoice()));

        report.RuleSets.ShouldContain(outcome => outcome.Name.StartsWith("CIUS-PT", StringComparison.Ordinal) && outcome.Ran);
        report.IsValid.ShouldBeTrue(
            string.Join(
                Environment.NewLine,
                report.Errors.Take(8).Select(message => $"  {message.RuleIdentifier}: {message.Message}")));
    }

    private static EInvoice AnInvoice() => EInvoiceBuilder
        .Create(PtProfiles.CiusPtUbl)
        .WithNumber("2026-0001")
        .IssuedOn(new DateOnly(2026, 9, 1))
        .DueOn(new DateOnly(2026, 10, 1))
        .OfType("380")
        .InCurrency("EUR")
        .WithBuyerReference("REF-2026-0001")
        .From(seller => seller
            .Named("Fornecedor Lda")
            .WithVatIdentifier("PT123456789")
            .WithLegalRegistration("12345678")
            .WithElectronicAddress("PT123456789", "9946")
            .WithAddress(address =>
            {
                address.Line1 = "Rua Augusta 1";
                address.City = "Lisboa";
                address.PostCode = "1100-053";
                address.CountryCode = "PT";
            }))
        .To(buyer => buyer
            .Named("Cliente Lda")
            .WithVatIdentifier("PT987654321")
            .WithElectronicAddress("PT987654321", "9946")
            .WithAddress(address =>
            {
                address.Line1 = "Rua de Santa Catarina 2";
                address.City = "Porto";
                address.PostCode = "4000-447";
                address.CountryCode = "PT";
            }))
        .AddLine(line => line
            .WithIdentifier("1")
            .WithItem("Consultoria")
            .WithQuantity(1m, "C62")
            .WithNetPrice(1000m)
            .WithNetAmount(1000m)
            .WithVat("S", 23m))
        // BR-CIUS-PT-66: Portugal requires a delivery address, which EN 16931 leaves optional.
        .Extend(invoice => invoice.Delivery = new DeliveryInformation
        {
            Address = new PostalAddress
            {
                Line1 = "Rua Augusta 1",
                City = "Lisboa",
                PostCode = "1100-053",
                CountryCode = "PT",
            },
        })
        .Extend(invoice => invoice.Payment = new PaymentInstructions
        {
            MeansTypeCode = "30",
            CreditTransfers = { new CreditTransfer { AccountIdentifier = "PT50000201231234567890154" } },
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
