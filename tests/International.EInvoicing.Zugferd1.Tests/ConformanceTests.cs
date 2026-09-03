using International.EInvoicing.Validation;
using International.EInvoicing.Validation.Xsd;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Zugferd1.Tests;

/// <summary>
/// What FeRD's own schema and rule set say about the four documents they published.
/// </summary>
/// <remarks>
/// This library does not write ZUGFeRD 1.0, so there is nothing of ours for the schema to judge. What it can
/// say is whether the reader is pointed at documents that are what they claim to be — and whether a document
/// this library <em>reads</em> is one FeRD's own rules accept, which is what tells a caller their archive is
/// what they think it is.
/// </remarks>
public class ConformanceTests
{
    public static TheoryData<string> Corpus() => ReadingTheCorpusTests.Corpus();

    [Theory]
    [MemberData(nameof(Corpus))]
    public void EveryPublishedDocumentSatisfiesItsOwnSchemaAndRules(string name)
    {
        (EInvoicing Library, string Document) fixture = Fixture(name);

        ValidationReport report = fixture.Library.Validate(fixture.Document);

        report.Errors.ShouldBeEmpty(Explain(report));
        report.RuleSets.ShouldContain(ruleSet => ruleSet.Ran, Explain(report));
    }

    /// <summary>
    /// And the schema is judging: one with no declaration for the document's root approves everything, which
    /// reads exactly like a document with nothing wrong with it.
    /// </summary>
    [Fact]
    public void AndTheSchemaIsActuallyJudgingTheDocument()
    {
        (EInvoicing Library, string Document) fixture = Fixture("ZUGFeRD1_COMFORT_Einfach.xml");

        string broken = fixture.Document.Replace(
            "<ram:TypeCode>",
            "<ram:Nonsense>",
            StringComparison.Ordinal)
            .Replace("</ram:TypeCode>", "</ram:Nonsense>", StringComparison.Ordinal);

        ValidationReport report = fixture.Library.Validate(broken);

        report.Errors.ShouldNotBeEmpty("the schema must reject an element it does not declare");
    }

    private static (EInvoicing Library, string Document) Fixture(string name)
    {
        string? path = Zugferd1Corpus.Find(name);
        string schema = Path.Combine(Zugferd1Corpus.Root, "schema");
        string rules = Path.Combine(Zugferd1Corpus.Root, "schematron");

        Assert.SkipWhen(
            path is null || !Directory.Exists(schema) || !Directory.Exists(rules),
            "run build/fetch-specs.sh zugferd1");

        EInvoicing library = EInvoicing.Create(builder => builder
            .AddDefaults()
            .AddZugferd1()
            .AddZugferd1SchemaFrom(schema)
            .AddZugferd1RulesFrom(rules));

        return (library, File.ReadAllText(path!));
    }

    private static string Explain(ValidationReport report) =>
        string.Join(Environment.NewLine, report.Errors.Select(error => error.ToString()))
        + Environment.NewLine
        + string.Join(Environment.NewLine, report.RuleSets.Select(ruleSet => ruleSet.ToString()));
}
