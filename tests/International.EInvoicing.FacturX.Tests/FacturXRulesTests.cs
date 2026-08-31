using International.EInvoicing.Building;
using International.EInvoicing.Configuration;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;
using International.EInvoicing.Validation;
using Shouldly;
using Xunit;

namespace International.EInvoicing.FacturX.Tests;

/// <summary>
/// The Factur-X rule sets, one per profile, over documents this library writes.
/// </summary>
/// <remarks>
/// The support matrix said "planned" for Factur-X validation while the engine was perfectly able to run the
/// rules — they were simply never wired. They are published as compiled XSLT, which is why: reading that
/// came later.
/// </remarks>
public class FacturXRulesTests
{
    private static readonly string Artefacts =
        Path.Combine(RepositoryRoot(), "specs", "national", "zugferd", "schematron");

    public static TheoryData<string> Profiles => new("BASIC", "EN16931", "EXTENDED");

    [Theory]
    [MemberData(nameof(Profiles))]
    public void AnInvoiceThisLibraryWritesSatisfiesTheProfilesOwnRules(string profile)
    {
        Assert.SkipWhen(!Directory.Exists(Artefacts), "run build/fetch-specs.sh national");

        EInvoicing library = EInvoicing.Create(facturx => facturx
            .AddDefaults()
            .AddFacturXRulesFrom(Artefacts));

        Profile chosen = profile switch
        {
            "BASIC" => FacturXProfiles.Basic,
            "EXTENDED" => FacturXProfiles.Extended,
            _ => FacturXProfiles.En16931,
        };

        ValidationReport report = library.Validate(library.Write(AnInvoice(chosen), DocumentFormat.Cii));

        report.RuleSets.ShouldContain(
            outcome => outcome.Name.StartsWith("Factur-X", StringComparison.Ordinal) && outcome.Ran,
            $"the {profile} rules should have run");

        report.IsValid.ShouldBeTrue(
            string.Join(
                Environment.NewLine,
                report.Errors.Take(6).Select(message => $"  {message.RuleIdentifier} at {message.Location}: {message.Message}")));
    }

    /// <summary>
    /// MINIMUM is not an EN 16931 invoice, and now something judges it: before this, nothing did.
    /// </summary>
    [Fact]
    public void AndTheProfilesThatAreNotEn16931AreJudgedToo()
    {
        Assert.SkipWhen(!Directory.Exists(Artefacts), "run build/fetch-specs.sh national");

        EInvoicing library = EInvoicing.Create(facturx => facturx
            .AddDefaults()
            .AddFacturXRulesFrom(Artefacts));

        ValidationReport report = library.Validate(
            library.Write(AnInvoice(FacturXProfiles.Minimum), DocumentFormat.Cii));

        report.RuleSets.ShouldContain(outcome => outcome.Name.Contains("MINIMUM", StringComparison.Ordinal));
    }

    private static EInvoice AnInvoice(Profile profile) => EInvoiceBuilder
        .Create(profile)
        .WithNumber("FX-2026-001")
        .IssuedOn(new DateOnly(2026, 9, 1))
        .DueOn(new DateOnly(2026, 10, 1))
        .OfType("380")
        .InCurrency("EUR")
        .From(seller => seller
            .Named("Fournisseur SARL")
            .WithVatIdentifier("FR32732829320")
            .WithAddress(address =>
            {
                address.Line1 = "1 rue de la Facture";
                address.City = "Angers";
                address.PostCode = "49000";
                address.CountryCode = "FR";
            }))
        .To(buyer => buyer
            .Named("Client SA")
            .WithVatIdentifier("FR89552081317")
            .WithAddress(address =>
            {
                address.Line1 = "2 rue du Client";
                address.City = "Nantes";
                address.PostCode = "44000";
                address.CountryCode = "FR";
            }))
        .AddLine(line => line
            .WithIdentifier("1")
            .WithItem("Conseil")
            .WithQuantity(1m, "C62")
            .WithNetPrice(1000m)
            .WithNetAmount(1000m)
            .WithVat("S", 20m))
        .Extend(invoice => invoice.Payment = new PaymentInstructions
        {
            MeansTypeCode = "30",
            CreditTransfers = { new CreditTransfer { AccountIdentifier = "FR7630001007941234567890185" } },
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
