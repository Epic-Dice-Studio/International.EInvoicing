using International.EInvoicing.Profiles;
using International.EInvoicing.Validation;
using International.EInvoicing.Validation.Schematron;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Validation.XRechnung.Tests;

/// <summary>
/// Measured against the corpus KoSIT publishes as conformant. A document the standard calls valid that these
/// rules reject is a defect here, not in the document.
/// </summary>
public class XRechnungRulesTests
{
    public static TheoryData<string> UblDocuments => Corpus("*_ubl.xml");

    public static TheoryData<string> CiiDocuments => Corpus("*_uncefact.xml");

    [Fact]
    public void TheArtefactsAreEmbeddedAndLoadWithTheirSharedVariables()
    {
        XRechnungRules.For(DocumentSyntax.Ubl).AssertionCount.ShouldBeGreaterThan(50);
        XRechnungRules.For(DocumentSyntax.Cii).AssertionCount.ShouldBeGreaterThan(50);
        XRechnungRules.ArtefactVersion.ShouldBe("3.0");
    }

    [Fact]
    public void ASyntaxWithNoArtefactsSaysSoRatherThanPretending()
    {
        XRechnungRules.Covers(DocumentSyntax.Cdar).ShouldBeFalse();
        Should.Throw<NotSupportedException>(() => XRechnungRules.For(DocumentSyntax.Cdar));
    }

    [Theory]
    [MemberData(nameof(UblDocuments))]
    public void EveryOfficialUblDocumentSatisfiesTheGermanRules(string path)
    {
        ValidationReport report = new SchematronValidator()
            .Validate(File.ReadAllText(path), XRechnungRules.For(DocumentSyntax.Ubl));

        report.IsValid.ShouldBeTrue(Describe(path, report));
    }

    [Theory]
    [MemberData(nameof(CiiDocuments))]
    public void EveryOfficialCiiDocumentSatisfiesTheGermanRules(string path)
    {
        ValidationReport report = new SchematronValidator()
            .Validate(File.ReadAllText(path), XRechnungRules.For(DocumentSyntax.Cii));

        report.IsValid.ShouldBeTrue(Describe(path, report));
    }

    [Fact]
    public void NoRuleIsSkippedForWantOfSomethingTheEngineCannotEvaluate()
    {
        foreach (string path in CorpusPaths("*_ubl.xml").Take(5))
        {
            ValidationReport report = new SchematronValidator()
                .Validate(File.ReadAllText(path), XRechnungRules.For(DocumentSyntax.Ubl));

            report.Messages
                .Where(message => message.Message.StartsWith("This rule could not be evaluated", StringComparison.Ordinal))
                .ShouldBeEmpty($"{Path.GetFileName(path)} left rules unevaluated");
        }
    }

    private static TheoryData<string> Corpus(string pattern)
    {
        var data = new TheoryData<string>();

        foreach (string path in CorpusPaths(pattern))
        {
            data.Add(path);
        }

        return data;
    }

    private static IEnumerable<string> CorpusPaths(string pattern) =>
        Directory
            .EnumerateFiles(
                Path.Combine(RepositoryRoot(), "specs", "xrechnung", "testsuite", "src", "test"),
                pattern,
                SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.Ordinal);

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, ".git")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }

    private static string Describe(string path, ValidationReport report) =>
        $"{Path.GetFileName(path)} was rejected:{Environment.NewLine}"
        + string.Join(
            Environment.NewLine,
            report.OfAtLeast(RuleSeverity.Error).Take(6).Select(message => "  " + message));
}
