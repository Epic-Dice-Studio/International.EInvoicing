using System.Xml.Linq;
using International.EInvoicing.Validation;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Validation.Schematron.Tests;

/// <summary>
/// The engine measured against the documents the standard publishes as correct. An example the norm calls
/// valid that this engine rejects is a defect in the engine, not in the example.
/// </summary>
public class EN16931ConformanceTests
{
    private static readonly SchematronRuleSet UblRules =
        SchematronRuleSet.Load(File.ReadAllText(Artefacts.UblRules), "EN 16931 (UBL)", "1.3.13");

    private static readonly SchematronRuleSet CiiRules =
        SchematronRuleSet.Load(File.ReadAllText(Artefacts.CiiRules), "EN 16931 (CII)", "1.3.13");

    public static TheoryData<string> UblExamples => Examples("ubl");

    public static TheoryData<string> CiiExamples => Examples("cii");

    [Fact]
    public void TheRuleSetsLoadInFull()
    {
        UblRules.AssertionCount.ShouldBeGreaterThan(900);
        CiiRules.AssertionCount.ShouldBeGreaterThan(700);
    }

    [Theory]
    [MemberData(nameof(UblExamples))]
    public void EveryOfficialUblExampleIsAccepted(string path)
    {
        ValidationReport report = new SchematronValidator().Validate(File.ReadAllText(path), UblRules);

        report.IsValid.ShouldBeTrue(Describe(path, report));
    }

    [Theory]
    [MemberData(nameof(CiiExamples))]
    public void EveryOfficialCiiExampleIsAccepted(string path)
    {
        ValidationReport report = new SchematronValidator().Validate(File.ReadAllText(path), CiiRules);

        report.IsValid.ShouldBeTrue(Describe(path, report));
    }

    [Fact]
    public void AnInvoiceMissingItsIssueDateIsRejected()
    {
        XDocument invoice = XDocument.Load(FirstUblExample());
        XNamespace cbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
        invoice.Root!.Element(cbc + "IssueDate")!.Remove();

        ValidationReport report = new SchematronValidator().Validate(invoice, UblRules);

        report.IsValid.ShouldBeFalse("an invoice with no issue date breaks BR-03");
        report.Messages.ShouldContain(message => message.RuleIdentifier == "BR-03");
    }

    [Fact]
    public void AFailedRuleSaysWhichBusinessTermItIsAbout()
    {
        XDocument invoice = XDocument.Load(FirstUblExample());
        XNamespace cbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
        invoice.Root!.Element(cbc + "IssueDate")!.Remove();

        ValidationMessage message = new SchematronValidator()
            .Validate(invoice, UblRules)
            .Messages
            .First(m => m.RuleIdentifier == "BR-03");

        message.BusinessTerm.ShouldBe("BT-2");
        message.Location.ShouldNotBeNullOrEmpty();
        message.RuleSet.ShouldBe("EN 16931 (UBL)");
    }

    [Fact]
    public void AReportSaysWhichRuleSetRan()
    {
        ValidationReport report = new SchematronValidator()
            .Validate(File.ReadAllText(FirstUblExample()), UblRules);

        RuleSetOutcome outcome = report.RuleSets.ShouldHaveSingleItem();
        outcome.Name.ShouldBe("EN 16931 (UBL)");
        outcome.Version.ShouldBe("1.3.13");
        outcome.Ran.ShouldBeTrue();
        report.IsComplete.ShouldBeTrue();
    }

    private static string FirstUblExample() =>
        Directory
            .EnumerateFiles(
                Path.Combine(Artefacts.RepositoryRoot, "specs", "en16931", "ubl", "examples"),
                "*.xml")
            .OrderBy(path => path, StringComparer.Ordinal)
            .First();

    private static TheoryData<string> Examples(string syntax)
    {
        var data = new TheoryData<string>();
        string directory = Path.Combine(Artefacts.RepositoryRoot, "specs", "en16931", syntax, "examples");

        foreach (string path in Directory.EnumerateFiles(directory, "*.xml").OrderBy(p => p, StringComparer.Ordinal))
        {
            data.Add(path);
        }

        return data;
    }

    private static string Describe(string path, ValidationReport report) =>
        $"{Path.GetFileName(path)} was rejected:{Environment.NewLine}"
        + string.Join(
            Environment.NewLine,
            report.OfAtLeast(RuleSeverity.Error).Take(8).Select(message => "  " + message));
}
