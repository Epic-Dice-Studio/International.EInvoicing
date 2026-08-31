using System.Globalization;
using International.EInvoicing.Building;
using International.EInvoicing.Configuration;
using International.EInvoicing.Countries.Greece.Identifiers;
using International.EInvoicing.Model;
using International.EInvoicing.Peppol;
using International.EInvoicing.Profiles;
using International.EInvoicing.Validation;
using International.EInvoicing.Validation.Schematron;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Countries.Greece.Tests;

/// <summary>
/// Greece asks for two things nothing else here does: a nine-digit AFM with a checksum of its own, and an
/// invoice number made of six segments.
/// </summary>
public class GreekEInvoicingTests
{
    /// <summary>AFMs computed from the formula, since a real one belongs to a real business.</summary>
    internal static IReadOnlyList<string> ValidAfm { get; } = [.. Enumerable
        .Range(0, 400)
        .Select(offset => WithCheckDigit(10_000_000 + (offset * 7_919)))
        .OfType<string>()
        .Take(6)];

    public static TheoryData<string> Valid => [.. ValidAfm];

    [Theory]
    [MemberData(nameof(Valid))]
    public void AnAfmSatisfyingTheFormulaIsAccepted(string afm)
    {
        GrTaxIdentifier.IsValid(afm).ShouldBeTrue(afm);
        GrTaxIdentifier.Parse("EL" + afm).Value.ShouldBe(afm);
        GrTaxIdentifier.Parse(afm).VatNumber.ShouldBe("EL" + afm);
    }

    [Theory]
    [InlineData("12345678")]         // eight digits
    [InlineData("1234567890")]       // ten
    [InlineData("12345678A")]
    [InlineData("")]
    [InlineData(null)]
    public void AnythingElseIsRefused(string? value) => GrTaxIdentifier.IsValid(value).ShouldBeFalse();

    /// <summary>
    /// There is no isolated cross-check against Peppol's own <c>u:TinVerification</c> here, unlike the
    /// Nordic identifiers.
    /// </summary>
    /// <remarks>
    /// <c>GR-R-009</c> tests <c>@schemeID = '9933' and u:TinVerification(.)</c> together, over every party
    /// identifier — so it cannot be aimed at one number the way <c>PEPPOL-COMMON-R041</c> can, and a test
    /// that substituted numbers into a document would be measuring the scheme attribute as much as the
    /// checksum. What stands instead is the whole-invoice test below, which puts a document carrying these
    /// AFMs in front of the real Greek rules.
    /// </remarks>
    private static void WhyThereIsNoIsolatedCrossCheck()
    {
        // Intentionally empty: this is documentation that lives next to the tests it explains.
    }

    /// <summary>The whole invoice, in front of the Greek rules.</summary>
    [Fact]
    public void AnInvoiceThisLibraryWritesSatisfiesTheGreekRules()
    {
        ValidationReport report = new SchematronValidator().Validate(Greece.Write(AnInvoice(), DocumentFormat.Ubl), PeppolRules());

        report.IsValid.ShouldBeTrue(
            string.Join(
                Environment.NewLine,
                report.OfAtLeast(RuleSeverity.Error).Select(m => $"  {m.RuleIdentifier}: {m.Message}")));
    }

    /// <summary>BT-1 is a compound key in Greece, and an ordinary invoice number is refused.</summary>
    [Fact]
    public void TheInvoiceNumberHasSixSegments()
    {
        GrInvoiceNumber.IsValid("2026-0001").ShouldBeFalse();
        GrInvoiceNumber.IsValid(SixSegments).ShouldBeTrue();
        GrInvoiceNumber.Split(SixSegments).Count.ShouldBe(6);

        // Every part is checked where it can be: an unknown document type and an empty series both fail.
        Should.Throw<ArgumentException>(
            () => GrInvoiceNumber.For(Seller, new DateOnly(2026, 9, 1), 0, "9.9", "A", "1"));
        Should.Throw<ArgumentException>(
            () => GrInvoiceNumber.For(Seller, new DateOnly(2026, 9, 1), 0, "1.1", "", "1"));
        Should.Throw<FormatException>(
            () => GrInvoiceNumber.For("123456789", new DateOnly(2026, 9, 1), 0, "1.1", "A", "1"));
    }

    private static string SixSegments =>
        GrInvoiceNumber.For(Seller, new DateOnly(2026, 9, 1), branch: 0, "1.1", "A", "0001");

    private static string Seller => ValidAfm[0];

    private static string Buyer => ValidAfm[1];

    private static readonly EInvoicing Greece =
        EInvoicing.Create(greece => greece.AddDefaults().AddGreece());

    private static SchematronRuleSet PeppolRules()
    {
        string path = Path.Combine(RepositoryRoot(), "specs", "peppol", "rules", "PEPPOL-EN16931-UBL.sch");

        Assert.SkipWhen(!File.Exists(path), "run build/fetch-specs.sh peppol");

        return SchematronRuleSet.Load(File.ReadAllText(path), "Peppol BIS Billing 3.0 (UBL)", "3.0");
    }

    private static EInvoice AnInvoice() => EInvoiceBuilder
        .Create(GrProfiles.PeppolBillingUbl)
        .ForPeppol()
        .WithNumber(SixSegments)
        .IssuedOn(new DateOnly(2026, 9, 1))
        .DueOn(new DateOnly(2026, 10, 1))
        .OfType("380")
        .InCurrency("EUR")
        .WithBuyerReference("REF-2026-0001")
        .From(seller => seller
            .Named("Προμηθευτής ΑΕ")
            .TradingAs("Προμηθευτής ΑΕ")
            .WithVatIdentifier("EL" + Seller)
            .WithElectronicAddress(Seller, GrTaxIdentifier.Scheme)
            .WithAddress(address =>
            {
                address.Line1 = "Ερμού 1";
                address.City = "Αθήνα";
                address.PostCode = "10563";
                address.CountryCode = "GR";
            }))
        .To(buyer => buyer
            .Named("Πελάτης ΑΕ")
            .TradingAs("Πελάτης ΑΕ")
            .WithVatIdentifier("EL" + Buyer)
            .WithElectronicAddress(Buyer, GrTaxIdentifier.Scheme)
            .WithAddress(address =>
            {
                address.Line1 = "Τσιμισκή 2";
                address.City = "Θεσσαλονίκη";
                address.PostCode = "54624";
                address.CountryCode = "GR";
            }))
        .AddLine(line => line
            .WithIdentifier("1")
            .WithItem("Συμβουλευτική")
            .WithQuantity(1m, "C62")
            .WithNetPrice(1000m)
            .WithNetAmount(1000m)
            .WithVat("S", 24m))
        // GR-R-004-1: exactly one MARK number, the myDATA registration, as an additional document.
        .Extend(invoice => invoice.AdditionalDocuments.Add(new AdditionalDocument
        {
            Identifier = "400001234567890",
            Description = "##M.AR.K##",
        }))
        .Extend(invoice => invoice.Payment = new PaymentInstructions
        {
            MeansTypeCode = "30",
            CreditTransfers = { new CreditTransfer { AccountIdentifier = "GR1601101250000000012300695" } },
        })
        .WithComputedVatBreakdown()
        .WithComputedTotals()
        .Build();

    /// <summary>The ninth digit, under the weighted checksum Greece uses.</summary>
    private static string? WithCheckDigit(int eightDigits)
    {
        string body = eightDigits.ToString("D8", CultureInfo.InvariantCulture);
        int sum = 0;
        int weight = 256;

        for (int index = 0; index < 8; index++)
        {
            sum += (body[index] - '0') * weight;
            weight /= 2;
        }

        return body + (sum % 11 % 10).ToString(CultureInfo.InvariantCulture);
    }

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
