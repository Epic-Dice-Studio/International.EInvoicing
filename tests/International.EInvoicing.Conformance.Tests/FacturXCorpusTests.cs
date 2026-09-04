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
    /// Registering plain EN 16931 beside the Factur-X rules rejects invoices Factur-X publishes as valid.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is a trap worth having written down. <c>AddDefaults()</c> registers the EN 16931 rules for every
    /// CII document, and Factur-X EXTENDED is a <em>superset</em> of EN 16931: it allows grouped lines, where
    /// a heading's amount is the sum of its children. EN 16931 has no such concept, so its BR-CO-10 adds the
    /// headings to the details and finds the line total twice what the document says.
    /// </para>
    /// <para>
    /// <c>factur-x-22.xml</c> is exactly that: six lines totalling 3000, of which four are details totalling
    /// 1500 and two are headings summing them. The document is right and the judge is wrong. Eight of the 58
    /// published documents fail this way.
    /// </para>
    /// <para>
    /// So the rule is: a profile that extends EN 16931 is judged by <em>its own</em> rule set, which bundles
    /// the EN 16931 rules it inherits and adapts the ones it changes. That is also how the KoSIT validator is
    /// configured. Whether <c>AddDefaults()</c> should refuse to attach EN 16931 to a document declaring a
    /// profile derived from it is a design question for the library, and is on the roadmap.
    /// </para>
    /// </remarks>
    [Fact]
    public void AndPlainEn16931RulesAreNotTheJudgeOfAProfileThatExtendsThem()
    {
        string path = Files("extended").FirstOrDefault(file => Path.GetFileName(file) == "factur-x-22.xml")
            ?? string.Empty;

        Assert.SkipWhen(path.Length == 0, "run build/fetch-specs.sh national");

        string schematron = Path.Combine(Corpora.RepositoryRoot(), "specs", "national", "zugferd", "schematron");
        string document = File.ReadAllText(path);

        ValidationReport byItsOwnRules = EInvoicing
            .Create(builder => builder.AddFacturX().AddFacturXRulesFrom(schematron))
            .Validate(document);

        ValidationReport withEn16931Added = EInvoicing
            .Create(builder => builder.AddDefaults().AddFacturXRulesFrom(schematron))
            .Validate(document);

        byItsOwnRules.Errors.ShouldBeEmpty("Factur-X publishes this document as a valid EXTENDED invoice");

        withEn16931Added.Errors
            .Select(error => error.RuleIdentifier)
            .ShouldContain(
                "BR-CO-10",
                "adding EN 16931 to a Factur-X EXTENDED document rejects it for arithmetic the document got "
                + "right; if this ever stops being true, the library has been taught about grouped lines and "
                + "this test should become an assertion that both agree");
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
