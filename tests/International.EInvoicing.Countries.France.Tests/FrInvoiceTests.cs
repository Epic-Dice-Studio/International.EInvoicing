using System.Xml.Linq;
using International.EInvoicing.Building;
using International.EInvoicing.Cii.Writing;
using International.EInvoicing.Countries.France.Invoicing;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;
using International.EInvoicing.Ubl;
using International.EInvoicing.Ubl.Writing;
using International.EInvoicing.Validation;
using International.EInvoicing.Validation.En16931;
using International.EInvoicing.Validation.Schematron;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Countries.France.Tests;

/// <summary>
/// What a French invoice needs beyond EN 16931, held to the DGFiP's own rules.
/// </summary>
/// <remarks>
/// Two thousand French assertions ran against this library before any of this existed, and said nothing
/// useful: they measure a document, and nothing helped a caller produce one. These tests are the other
/// direction — an invoice built the short way, put in front of the same rules.
/// </remarks>
public class FrInvoiceTests
{
    public static TheoryData<string> Syntaxes => new("UBL", "CII");

    /// <summary>The rules France publishes, which are fetched rather than redistributed.</summary>
    [Theory]
    [MemberData(nameof(Syntaxes))]
    public void AFrenchInvoiceSatisfiesTheFrenchRules(string syntax)
    {
        string directory = Path.Combine(RepositoryRoot(), "specs", "fr-dse", "rules", "ctc");

        Assert.SkipWhen(
            !Directory.Exists(directory),
            "The French artefacts are not present; run build/fetch-specs.sh france.");

        string xml = Write(AFrenchInvoice(), syntax);
        var checked_ = 0;

        foreach (string path in Directory
            .EnumerateFiles(directory, $"*{syntax}*.sch", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.Ordinal))
        {
            SchematronRuleSet rules = SchematronRuleSet.Load(
                File.ReadAllText(path),
                Path.GetFileNameWithoutExtension(path),
                "1.4.0.03");

            ValidationReport report = new SchematronValidator().Validate(xml, rules);

            report.IsValid.ShouldBeTrue(Describe(Path.GetFileName(path), report));
            checked_++;
        }

        checked_.ShouldBe(2, $"both French {syntax} rule sets should have run");
    }

    /// <summary>France restricts EN 16931; it does not replace it, so both must hold.</summary>
    [Theory]
    [MemberData(nameof(Syntaxes))]
    public void AFrenchInvoiceAlsoSatisfiesEn16931(string syntax)
    {
        DocumentSyntax which = syntax == "UBL" ? DocumentSyntax.Ubl : DocumentSyntax.Cii;

        ValidationReport report = new SchematronValidator()
            .Validate(Write(AFrenchInvoice(), syntax), En16931Rules.For(which));

        report.IsValid.ShouldBeTrue(Describe($"EN 16931 ({syntax})", report));
    }

    /// <summary>The three mentions are not optional, and UBL carries their codes inside the note itself.</summary>
    [Fact]
    public void TheMandatoryMentionsAreCarriedWithTheirCodes()
    {
        EInvoice invoice = AFrenchInvoice();

        invoice.Notes.Select(note => note.SubjectCode.Value).ShouldBe(["PMT", "PMD", "AAB"], ignoreOrder: true);

        XElement written = XElement.Parse(Write(invoice, "UBL"));
        string[] notes = [.. written.Elements(UblNames.Cbc + "Note").Select(note => note.Value)];

        notes.ShouldContain(note => note.StartsWith("#PMT#", StringComparison.Ordinal));
        notes.ShouldContain(note => note.StartsWith("#PMD#", StringComparison.Ordinal));
        notes.ShouldContain(note => note.StartsWith("#AAB#", StringComparison.Ordinal));
    }

    [Fact]
    public void AMentionIsReplacedRatherThanDuplicated()
    {
        EInvoice invoice = EInvoiceBuilder
            .Create(FrProfiles.ExtendedCtcFrUbl)
            .ForFrance()
            .WithFrenchMention(FrInvoiceMention.EarlyPaymentDiscountCode, "Escompte de 2 % sous 10 jours.")
            .Build();

        InvoiceNote discount = invoice.Notes.Single(note => note.SubjectCode.Value == "AAB");

        discount.Text.Value.ShouldBe("Escompte de 2 % sous 10 jours.");
        invoice.Notes.Count.ShouldBe(3);
    }

    [Fact]
    public void AnUnknownInvoicingCaseIsRefusedWithTheListOfRealOnes()
    {
        ArgumentException thrown = Should.Throw<ArgumentException>(
            () => EInvoiceBuilder.Create(FrProfiles.ExtendedCtcFrUbl).InFrenchProcess("X9"));

        thrown.Message.ShouldContain("BR-FR-08");
        thrown.Message.ShouldContain("B1");
    }

    /// <summary>A SIREN carries a check digit, so a typo is caught before the invoice leaves.</summary>
    [Fact]
    public void ASirenIsCheckedBeforeItIsWritten()
    {
        Should.Throw<FormatException>(() => EInvoiceBuilder
            .Create(FrProfiles.ExtendedCtcFrUbl)
            .FromFrenchSeller("Fournisseur SARL", "732829321", "FR32732829320"));

        EInvoice invoice = EInvoiceBuilder
            .Create(FrProfiles.ExtendedCtcFrUbl)
            .FromFrenchSeller("Fournisseur SARL", "732 829 320", "FR32732829320")
            .Build();

        invoice.Seller!.LegalRegistrationIdentifier.Value.ShouldBe("732829320");
        invoice.Seller.LegalRegistrationIdentifier.SchemeId.ShouldBe("0002");
    }

    private static EInvoice AFrenchInvoice() => EInvoiceBuilder
        .Create(FrProfiles.ExtendedCtcFrUbl)
        .WithNumber("FA-2026-001")
        .IssuedOn(new DateOnly(2026, 9, 1))
        .DueOn(new DateOnly(2026, 10, 1))
        .OfType("380")
        .InCurrency("EUR")
        .WithBuyerReference("PO-4417")
        .ForFrance()
        .FromFrenchSeller("Fournisseur SARL", "732829320", "FR32732829320", seller => seller
            .WithElectronicAddress("732829320", "0225")
            .WithAddress(address =>
            {
                address.Line1 = "1 rue de la Facture";
                address.City = "Angers";
                address.PostCode = "49000";
                address.CountryCode = "FR";
            }))
        .ToFrenchBuyer("Client SA", "552081317", "FR89552081317", buyer => buyer
            .WithElectronicAddress("552081317", "0225")
            .WithAddress(address =>
            {
                address.Line1 = "8 avenue des Clients";
                address.City = "Nantes";
                address.PostCode = "44000";
                address.CountryCode = "FR";
            }))
        .AddLine(line => line
            .WithIdentifier("1")
            .WithItem("Conseil")
            .WithQuantity(3m, "HUR")
            .WithNetPrice(150m)
            .WithNetAmount(450m)
            .WithVat("S", 20m))
        .WithComputedVatBreakdown()
        .WithComputedTotals()
        .Build();

    private static string Write(EInvoice invoice, string syntax) =>
        syntax == "UBL"
            ? new UblInvoiceWriter().WriteToString(invoice)
            : new CiiInvoiceWriter().WriteToString(invoice);

    private static string Describe(string what, ValidationReport report) =>
        $"{what} rejected the invoice:{Environment.NewLine}"
        + string.Join(
            Environment.NewLine,
            report.OfAtLeast(RuleSeverity.Error).Select(message => $"  {message.RuleIdentifier}: {message.Message}"));

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, ".git")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}
