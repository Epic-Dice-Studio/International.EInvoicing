using International.EInvoicing.Cii;
using International.EInvoicing.FacturX;
using International.EInvoicing.Validation;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Conformance.Tests;

/// <summary>
/// Factur-X documents somebody else wrote, judged by the publisher's own rules, one profile at a time.
/// </summary>
/// <remarks>
/// <para>
/// Until this corpus arrived, every Factur-X test in this repository used a document written in this
/// repository — which measures the library against its own idea of what a Factur-X invoice looks like. That
/// is the weakest kind of test there is, and Factur-X is the format both France and Germany run on.
/// </para>
/// <para>
/// The documents come from the ZUGFeRD rule module of phive-rules, at the same release as the rule sets the
/// library registers. Version-matching is not a detail: <c>AddFacturXRulesFrom</c> registers the newest
/// artefacts it finds, so a corpus from an older release would be judged by rules whose wording has moved on.
/// </para>
/// <para>
/// Each profile is judged by its own rule set, because the profiles nest and the wider ones permit what the
/// narrower ones forbid. A MINIMUM document judged by the EXTENDED rules would pass things MINIMUM does not
/// allow.
/// </para>
/// </remarks>
public class FacturXCorpusTests
{
    public static TheoryData<string> Minimum => Corpus("minimum");

    public static TheoryData<string> BasicWithoutLines => Corpus("basicwl");

    public static TheoryData<string> Basic => Corpus("basic");

    public static TheoryData<string> En16931 => Corpus("en16931");

    public static TheoryData<string> Extended => Corpus("extended");

    [Theory]
    [MemberData(nameof(Minimum))]
    public void EveryPublishedMinimumInvoiceSatisfiesTheMinimumRules(string path) => Measure(path);

    [Theory]
    [MemberData(nameof(BasicWithoutLines))]
    public void EveryPublishedBasicWithoutLinesInvoiceSatisfiesItsRules(string path) => Measure(path);

    [Theory]
    [MemberData(nameof(Basic))]
    public void EveryPublishedBasicInvoiceSatisfiesTheBasicRules(string path) => Measure(path);

    [Theory]
    [MemberData(nameof(En16931))]
    public void EveryPublishedEn16931InvoiceSatisfiesTheEn16931Rules(string path) => Measure(path);

    [Theory]
    [MemberData(nameof(Extended))]
    public void EveryPublishedExtendedInvoiceSatisfiesTheExtendedRules(string path) => Measure(path);

    /// <summary>
    /// And the corpus covers every profile, rather than the one profile most documents happen to use.
    /// </summary>
    [Fact]
    public void AndEveryProfileIsExercised()
    {
        Assert.SkipWhen(!Directory.Exists(Root), "run build/fetch-specs.sh national");

        foreach (string profile in (string[])["minimum", "basicwl", "basic", "en16931", "extended"])
        {
            Files(profile).ShouldNotBeEmpty($"no {profile} documents were fetched");
        }
    }

    /// <summary>
    /// The EN 16931 rules stand aside for the profile's own, and say so.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This used to be the other way round, and it is worth remembering why. <c>AddDefaults()</c> registers
    /// the EN 16931 rules for every CII document, and Factur-X EXTENDED is a <em>superset</em>: it allows
    /// grouped lines, where a heading's amount is the sum of its children. EN 16931 has no such concept, so
    /// its BR-CO-10 added the headings to the details and found the line total twice what the document said.
    /// <c>factur-x-22.xml</c> is exactly that — four detail lines totalling 1500 and two headings summing
    /// them — and eight of the 58 published documents were rejected for obeying their own specification.
    /// </para>
    /// <para>
    /// A profile's rule set now supersedes the base it was built on. What must not happen is a silent skip:
    /// the report says the base did not run and why, because "clean" and "never looked at" must not read the
    /// same.
    /// </para>
    /// </remarks>
    [Fact]
    public void TheEn16931RulesStandAsideForTheProfileSOwnAndSaySo()
    {
        string path = Files("extended").FirstOrDefault(file => Path.GetFileName(file) == "factur-x-22.xml")
            ?? string.Empty;

        Assert.SkipWhen(path.Length == 0, "run build/fetch-specs.sh national");

        string schematron = Path.Combine(Corpora.RepositoryRoot(), "specs", "national", "zugferd", "schematron");

        ValidationReport report = EInvoicing
            .Create(builder => builder.AddDefaults().AddFacturXRulesFrom(schematron))
            .Validate(File.ReadAllText(path));

        report.Errors.ShouldBeEmpty(
            "Factur-X publishes this as a valid EXTENDED invoice, and registering EN 16931 as well must not "
            + "change that:" + Environment.NewLine
            + string.Join(Environment.NewLine, report.Errors.Select(error => error.ToString())));

        report.RuleSets.ShouldContain(
            outcome => outcome.Ran && outcome.Name.Contains("Factur-X", StringComparison.Ordinal),
            "the profile's own rules are what judged it");

        report.NotRun.ShouldContain(
            outcome => outcome.Name.Contains("EN 16931", StringComparison.Ordinal)
                && outcome.SkippedBecause!.Contains("superseded", StringComparison.Ordinal),
            "and the base must say it stood aside rather than vanish from the report");
    }

    /// <summary>
    /// And with no profile rules registered the base still runs: standing aside for nobody would mean
    /// checking nothing.
    /// </summary>
    [Fact]
    public void AndWithNoProfileRulesRegisteredTheBaseStillRuns()
    {
        IReadOnlyList<string> documents = Files("en16931");
        string path = documents.Count > 0 ? documents[0] : string.Empty;
        Assert.SkipWhen(path.Length == 0, "run build/fetch-specs.sh national");

        ValidationReport report = EInvoicing.Create(builder => builder.AddDefaults())
            .Validate(File.ReadAllText(path));

        report.RuleSets.ShouldContain(
            outcome => outcome.Ran && outcome.Name.Contains("EN 16931", StringComparison.Ordinal),
            "with nothing more specific registered there is nothing to stand aside for");
    }

    private static void Measure(string path)
    {
        Assert.SkipWhen(path.Length == 0, "run build/fetch-specs.sh national");

        // Only the publisher's own rule sets. Each Factur-X profile bundles the EN 16931 rules it inherits
        // and adapts them, so registering plain EN 16931 alongside is not "more thorough" — it judges an
        // EXTENDED invoice by rules that do not know EXTENDED exists. See AndPlainEn16931RulesAreNotTheJudge.
        EInvoicing library = EInvoicing.Create(builder => builder
            .AddFacturX()
            .AddFacturXRulesFrom(Path.Combine(Corpora.RepositoryRoot(), "specs", "national", "zugferd", "schematron")));

        ValidationReport report = library.Validate(File.ReadAllText(path));

        report.Errors.ShouldBeEmpty(
            $"{Path.GetFileName(path)} is published as conformant:{Environment.NewLine}"
            + string.Join(Environment.NewLine, report.Errors.Select(error => error.ToString())));

        report.RuleSets.ShouldContain(
            ruleSet => ruleSet.Ran,
            $"nothing judged {Path.GetFileName(path)}; a document nobody checked is not a document that passed");
    }

    private static string Root =>
        Path.Combine(Corpora.RepositoryRoot(), "specs", "national", "zugferd", "test-files");

    private static IReadOnlyList<string> Files(string profile)
    {
        if (!Directory.Exists(Root))
        {
            return [];
        }

        return
        [
            .. Directory.EnumerateDirectories(Root, profile, SearchOption.AllDirectories)
                .SelectMany(directory => Directory.EnumerateFiles(directory, "*.xml", SearchOption.AllDirectories))
                .Order(StringComparer.Ordinal),
        ];
    }

    private static TheoryData<string> Corpus(string profile)
    {
        var data = new TheoryData<string>();

        foreach (string path in Files(profile))
        {
            data.Add(path);
        }

        if (data.Count == 0)
        {
            data.Add(string.Empty);
        }

        return data;
    }
}
