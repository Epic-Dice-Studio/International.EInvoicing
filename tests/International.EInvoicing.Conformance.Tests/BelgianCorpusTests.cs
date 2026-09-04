using International.EInvoicing.Countries.Belgium;
using International.EInvoicing.Ubl;
using International.EInvoicing.Validation;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Conformance.Tests;

/// <summary>
/// Belgian invoices somebody else wrote, judged by <c>GLOBALUBL.BE</c>.
/// </summary>
/// <remarks>
/// Belgium had no document corpus in this repository at all: every Belgian test used an invoice written
/// here, which measures the library against its own idea of a Belgian invoice. These are the 36 the
/// publisher ships, at the same version as the rule set the library registers.
/// </remarks>
public class BelgianCorpusTests
{
    public static TheoryData<string> Corpus()
    {
        var data = new TheoryData<string>();

        foreach (string path in Documents())
        {
            data.Add(path);
        }

        if (data.Count == 0)
        {
            data.Add(string.Empty);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Corpus))]
    public void EveryPublishedBelgianInvoiceSatisfiesTheBelgianRules(string path)
    {
        Assert.SkipWhen(path.Length == 0, "run build/fetch-specs.sh national");

        ValidationReport report = Library().Validate(File.ReadAllText(path));

        report.Errors.ShouldBeEmpty(
            $"{Path.GetFileName(path)} is published as conformant:{Environment.NewLine}"
            + string.Join(Environment.NewLine, report.Errors.Select(error => error.ToString())));

        report.RuleSets.ShouldContain(
            ruleSet => ruleSet.Ran,
            $"nothing judged {Path.GetFileName(path)}");
    }

    /// <summary>And there are enough of them to be worth running.</summary>
    [Fact]
    public void AndTheCorpusIsTheWholePublishedSet()
    {
        Assert.SkipWhen(Documents().Count == 0, "run build/fetch-specs.sh national");

        Documents().Count.ShouldBeGreaterThan(30, "the publisher ships 36");
    }

    /// <summary>
    /// And registering EN 16931 as well changes nothing, because GLOBALUBL.BE supersedes it.
    /// </summary>
    /// <remarks>
    /// The same correction as Factur-X EXTENDED, in a second country and a second syntax. GLOBALUBL.BE
    /// bundles the EN 16931 rules and <em>adapts</em> several of them; registering the unmodified originals
    /// alongside used to re-impose exactly what Belgium relaxed, and seventeen of these 36 published invoices
    /// were rejected for BR-CL-22, BR-DEC-23, UBL-DT-01, BR-S-08 and BR-E-08.
    /// </remarks>
    [Fact]
    public void AndRegisteringEn16931AsWellChangesNothing()
    {
        IReadOnlyList<string> documents = Documents();
        Assert.SkipWhen(documents.Count == 0, "run build/fetch-specs.sh national");

        EInvoicing withEn16931Added = BelgianEInvoicing.Create(builder => builder
            .AddDefaults()
            .AddBelgium()
            .AddBelgianRulesFrom(Rules())).Library;

        string[] rejected =
        [
            .. documents
                .Where(path => withEn16931Added.Validate(File.ReadAllText(path)).Errors.Any())
                .Select(Path.GetFileName)
                .Select(name => name!),
        ];

        rejected.ShouldBeEmpty(
            "Belgium publishes all of these as valid; adding the rules its own rule set already adapts must "
            + "not reject them: " + string.Join(", ", rejected));
    }

    private static string Rules() =>
        Path.Combine(Corpora.RepositoryRoot(), "specs", "national", "ublbe", "schematron");

    /// <summary>
    /// Belgium's own rule set and nothing else on top: GLOBALUBL.BE already bundles the EN 16931 rules that
    /// still apply, adapted where Belgium adapts them.
    /// </summary>
    private static EInvoicing Library() =>
        BelgianEInvoicing.Create(builder => builder
            .AddUbl()
            .AddBelgium()
            .AddBelgianRulesFrom(Rules())).Library;

    private static IReadOnlyList<string> Documents()
    {
        string root = Path.Combine(Corpora.RepositoryRoot(), "specs", "national", "ublbe", "test-files");

        return Directory.Exists(root)
            ? [.. Directory.EnumerateFiles(root, "*.xml", SearchOption.AllDirectories).Order(StringComparer.Ordinal)]
            : [];
    }
}
