using System.Text.RegularExpressions;
using International.EInvoicing.Building;
using International.EInvoicing.Cii.Writing;
using International.EInvoicing.Countries.Germany.Identifiers;
using International.EInvoicing.Countries.Germany.Payment;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;
using International.EInvoicing.Ubl.Writing;
using International.EInvoicing.Validation;
using International.EInvoicing.Validation.Schematron;
using International.EInvoicing.Validation.XRechnung;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Countries.Germany.Tests;

/// <summary>
/// The examples are the ones <c>BR-DE-18</c> is written around, and the statements this reads are compared
/// against the published expression itself rather than against a copy of it kept here.
/// </summary>
public class DeSkontoTests
{
    public static TheoryData<string> Statements => new(
        "#SKONTO#TAGE=7#PROZENT=2.00#",
        "#SKONTO#TAGE=14#PROZENT=1.00#",
        "#SKONTO#TAGE=7#PROZENT=2.00#BASISBETRAG=100.00#",
        "#SKONTO#TAGE=7#PROZENT=2.00#BASISBETRAG=-100.00#",
        "#SKONTO#TAGE=0#PROZENT=0.00#",
        "#SKONTO#TAGE=7#PROZENT=2.0#",
        "#SKONTO#TAGE=7#PROZENT=2.000#",
        "#SKONTO#TAGE=7#PROZENT=2#",
        "#SKONTO#TAGE=7.5#PROZENT=2.00#",
        "#SKONTO#TAGE=7#PROZENT=-2.00#",
        "#skonto#TAGE=7#PROZENT=2.00#",
        "#SKONTO#TAGE=7#PROZENT=2.00",
        "#SKONTO# TAGE=7#PROZENT=2.00#",
        "#SKONTO#TAGE=7#PROZENT=2.00#BASISBETRAG=100#",
        "#SKONTO#TAGE=7#PROZENT=2.00#RABATT=1.00#",
        "Zahlbar innerhalb von 30 Tagen ohne Abzug.");

    /// <summary>
    /// What this reads is what the rule accepts, statement by statement, and the rule is the artefact under
    /// <c>specs/</c> rather than an expression transcribed into this test.
    /// </summary>
    [Theory]
    [MemberData(nameof(Statements))]
    public void AStatementIsReadExactlyWhenTheRuleAcceptsIt(string line)
    {
        bool ruleAccepts = XRechnungArtefacts.SkontoExpression.IsMatch(line);

        DeSkontoTerms.Parse(line).Count.ShouldBe(
            ruleAccepts ? 1 : 0,
            $"BR-DE-18 {(ruleAccepts ? "accepts" : "refuses")} {line}");
    }

    /// <summary>A statement the rule accepts is one this writes back character for character.</summary>
    [Theory]
    [MemberData(nameof(Statements))]
    public void AndWhatItReadsItWritesBackUnchanged(string line)
    {
        foreach (DeSkonto term in DeSkontoTerms.Parse(line))
        {
            term.ToString().ShouldBe(line);
        }
    }

    [Fact]
    public void TheDiscountIsReadOutOfTheNoteRatherThanOutOfASentence()
    {
        IReadOnlyList<DeSkonto> terms = DeSkontoTerms.Parse(
            "#SKONTO#TAGE=7#PROZENT=2.00#\n"
            + "#SKONTO#TAGE=14#PROZENT=1.00#BASISBETRAG=500.00#\n"
            + "Bei Zahlung innerhalb von 7 Tagen 2% Skonto.");

        terms.Count.ShouldBe(2);
        terms[0].ShouldBe(new DeSkonto(7, 2.00m));
        terms[1].ShouldBe(new DeSkonto(14, 1.00m, 500.00m));
    }

    /// <summary>
    /// <c>BR-DE-18</c> fails a note whose last statement is not followed by a line break, which is the part a
    /// hand-rolled writer forgets.
    /// </summary>
    [Fact]
    public void EveryStatementWrittenIsFollowedByALineBreak()
    {
        DeSkontoTerms.Write([new DeSkonto(7, 2.00m)])
            .ShouldBe("#SKONTO#TAGE=7#PROZENT=2.00#\n");

        DeSkontoTerms.Write([new DeSkonto(7, 2.00m), new DeSkonto(14, 1.00m)], "Zahlbar ohne Abzug.")
            .ShouldBe("#SKONTO#TAGE=7#PROZENT=2.00#\n#SKONTO#TAGE=14#PROZENT=1.00#\nZahlbar ohne Abzug.");
    }

    [Fact]
    public void PuttingDiscountsOnAnInvoiceKeepsWhatTheNoteSaidAndReplacesWhatItClaimed()
    {
        EInvoice invoice = AnXRechnungInvoice(DocumentSyntax.Ubl);
        invoice.PaymentTerms = "#SKONTO#TAGE=3#PROZENT=5.00#\nZahlbar ohne Abzug.";

        invoice.WithSkonto(new DeSkonto(7, 2.00m));

        invoice.PaymentTerms.Value.ShouldBe("#SKONTO#TAGE=7#PROZENT=2.00#\nZahlbar ohne Abzug.");
        invoice.SkontoTerms().ShouldBe([new DeSkonto(7, 2.00m)]);
    }

    [Theory]
    [MemberData(nameof(GeneratedInvoiceTests.Syntaxes), MemberType = typeof(GeneratedInvoiceTests))]
    public void AnInvoiceCarryingDiscountsThisWroteSatisfiesTheGermanRules(string syntax)
    {
        DocumentSyntax which = syntax == "UBL" ? DocumentSyntax.Ubl : DocumentSyntax.Cii;
        EInvoice invoice = AnXRechnungInvoice(which)
            .WithSkonto(new DeSkonto(7, 2.00m), new DeSkonto(14, 1.00m, 500.00m));

        ValidationReport report = Validate(invoice, which);

        report.IsValid.ShouldBeTrue(Describe(report));
    }

    /// <summary>
    /// The proof that the rule above actually ran: the same invoice, with the note a hand-rolled writer
    /// produces, is rejected — and by <c>BR-DE-18</c> rather than by something else.
    /// </summary>
    [Theory]
    [MemberData(nameof(GeneratedInvoiceTests.Syntaxes), MemberType = typeof(GeneratedInvoiceTests))]
    public void WhereAHandWrittenNoteIsRejected(string syntax)
    {
        DocumentSyntax which = syntax == "UBL" ? DocumentSyntax.Ubl : DocumentSyntax.Cii;
        EInvoice invoice = AnXRechnungInvoice(which);
        invoice.PaymentTerms = "#SKONTO#TAGE=7#PROZENT=2.0#";

        ValidationReport report = Validate(invoice, which);

        report.OfAtLeast(RuleSeverity.Error)
            .ShouldContain(message => message.RuleIdentifier == "BR-DE-18", Describe(report));
    }

    private static ValidationReport Validate(EInvoice invoice, DocumentSyntax syntax) =>
        new SchematronValidator().Validate(
            syntax == DocumentSyntax.Ubl
                ? new UblInvoiceWriter().WriteToString(invoice)
                : new CiiInvoiceWriter().WriteToString(invoice),
            XRechnungRules.For(syntax));

    private static EInvoice AnXRechnungInvoice(DocumentSyntax syntax) => EInvoiceBuilder
        .Create(syntax == DocumentSyntax.Ubl ? DeProfiles.XRechnungUbl : DeProfiles.XRechnungCii)
        .WithNumber("RE-2026-001")
        .IssuedOn(new DateOnly(2026, 9, 1))
        .DueOn(new DateOnly(2026, 10, 1))
        .OfType("380")
        .InCurrency("EUR")
        .WithBuyerReference(DeLeitwegId.Create("04011000", "1234512345").ToString())
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

/// <summary>The published XRechnung rules under <c>specs/</c>, read rather than transcribed.</summary>
internal static partial class XRechnungArtefacts
{
    /// <summary>
    /// The expression <c>BR-DE-18</c> tests each statement line against, taken out of <c>common.sch</c>.
    /// </summary>
    public static Regex SkontoExpression { get; } = new(
        SkontoExpressionValue(),
        RegexOptions.CultureInvariant);

    private static string SkontoExpressionValue()
    {
        string common = Path.Combine(
            RepositoryRoot(), "specs", "xrechnung", "schematron", "src", "validation", "schematron", "common.sch");

        Match declaration = SkontoDeclaration().Match(File.ReadAllText(common));

        return declaration.Success
            ? declaration.Groups["expression"].Value
            : throw new InvalidOperationException($"XR-SKONTO-REGEX not found in {common}.");
    }

    [GeneratedRegex("""<let\s+name="XR-SKONTO-REGEX"\s+value="'(?<expression>[^']*)'"\s*/>""")]
    private static partial Regex SkontoDeclaration();

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
