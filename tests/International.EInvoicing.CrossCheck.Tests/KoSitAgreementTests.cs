using International.EInvoicing.Countries.Germany;
using International.EInvoicing.Validation;
using International.EInvoicing.Validation.XRechnung;
using International.EInvoicing.Validation.Xsd;
using Shouldly;
using Xunit;

namespace International.EInvoicing.CrossCheck.Tests;

/// <summary>
/// This library's engine against the KoSIT validator, document by document and rule by rule.
/// </summary>
/// <remarks>
/// <para>
/// Every other check in this repository compares this library against expected <em>results</em> — a corpus
/// its publisher calls conformant, a unit case naming how many times a rule should fire. All of it shares a
/// blind spot: a rule that this library and the corpus author read the same wrong way passes everywhere.
/// </para>
/// <para>
/// Comparing against another engine is the only thing that sees it, and KoSIT's is the one German
/// authorities actually run. Agreement on acceptance is the weak half — both accepting a valid document
/// proves little. The half that bites is which rules each engine <em>fires</em>: a rule KoSIT reports and
/// this library does not is one this library reads more permissively than the reference does.
/// </para>
/// </remarks>
public class KoSitAgreementTests : IClassFixture<Comparison>
{
    private readonly Comparison _comparison;

    public KoSitAgreementTests(Comparison comparison) => _comparison = comparison;

    /// <summary>
    /// Every rule the reference rejects a document for, this library rejects it for too — and the other way
    /// round.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Not "both engines accept the same documents", which is the comparison this started as and which
    /// compares two different questions. KoSIT's acceptance is decided by its scenario's own
    /// <c>acceptMatch</c>, and the XRechnung scenarios accept a document that broke EN 16931 rules:
    /// <c>05.01a-INVOICE_ubl</c> violates BR-CO-16 — its payable amount is thirty euros more than its total
    /// — and the reference reports the error and accepts the document anyway. This library's
    /// <c>IsValid</c> means "nothing was reported as an error", which is a different thing, and comparing
    /// the two made four documents look like disagreements when the two engines had in fact reported
    /// exactly the same rules.
    /// </para>
    /// <para>
    /// What is comparable is the findings, and both directions matter: a rule the reference fires and this
    /// library does not is one read too permissively, and a rule this library fires and the reference does
    /// not is a document rejected for something the authorities would have passed.
    /// </para>
    /// </remarks>
    [Fact]
    public void TheTwoEnginesRejectDocumentsForTheSameRules()
    {
        Assert.SkipUnless(_comparison.Ran, _comparison.WhyNot);

        string[] differences =
        [
            .. _comparison.Results
                .SelectMany(result => result.KoSitErrors
                    .Except(result.WeErrors)
                    .Select(code => $"{result.Name}: {code} — the reference errors, we do not")
                    .Concat(result.WeErrors
                        .Except(result.KoSitErrors)
                        .Select(code => $"{result.Name}: {code} — we error, the reference does not")))
                .Order(StringComparer.Ordinal),
        ];

        differences.ShouldBeEmpty(
            $"{differences.Length} error-level disagreements:"
            + Environment.NewLine
            + string.Join(Environment.NewLine, differences.Take(30)));
    }

    /// <summary>
    /// And every rule the reference fires, this library fires. The direction that matters: a rule KoSIT
    /// reports and we do not is a rule we are reading more permissively than the reference implementation.
    /// </summary>
    [Fact]
    public void EveryRuleTheReferenceFiresThisLibraryFiresToo()
    {
        Assert.SkipUnless(_comparison.Ran, _comparison.WhyNot);

        string[] missed =
        [
            .. _comparison.Results
                .SelectMany(result => result.KoSitFired
                    .Except(result.WeFired)
                    .Select(code => $"{code} (on {result.Name})"))
                .Order(StringComparer.Ordinal),
        ];

        missed.ShouldBeEmpty(
            $"{missed.Length} rule firings the KoSIT validator reports and this library does not:"
            + Environment.NewLine
            + string.Join(Environment.NewLine, missed.Take(40)));
    }

    /// <summary>And the comparison compared something, rather than an empty corpus with an empty corpus.</summary>
    [Fact]
    public void AndTheComparisonActuallyRan()
    {
        Assert.SkipUnless(_comparison.Ran, _comparison.WhyNot);

        _comparison.Results.Count.ShouldBeGreaterThan(50, "the XRechnung test suite has 86 documents");
        _comparison.Results.ShouldContain(
            result => result.KoSitFired.Count > 0,
            "a comparison in which the reference never fires anything is comparing nothing");
    }

    private static string Verdict(bool accepted) => accepted ? "accepts" : "rejects";
}

/// <summary>
/// Runs both engines over the corpus once, because starting a JVM eighty-six times is not a test, it is
/// a wait.
/// </summary>
public sealed class Comparison
{
    public Comparison()
    {
        IReadOnlyList<string> documents = Corpus.Documents();

        if (documents.Count == 0)
        {
            WhyNot = "run build/fetch-specs.sh xrechnung";
            return;
        }

        if (!KoSitValidator.IsAvailable)
        {
            WhyNot = KoSitValidator.WhyNot;
            return;
        }

        IReadOnlyDictionary<string, KoSitVerdict> theirs = KoSitValidator.Validate(documents);
        // The whole German setup, plus the schemas KoSIT also runs. Create(configure) replaces the
        // configuration rather than adding to it, so the defaults have to be named here as well —
        // registering only the schemas would compare our schema against their whole engine.
        GermanEInvoicing german = GermanEInvoicing.Create(builder => builder
            .AddDefaults()
            .AddGermany()
            .AddXRechnungRules()
            .AddSchemas());

        var results = new List<Result>();

        foreach (string path in documents)
        {
            string name = Path.GetFileNameWithoutExtension(path);

            if (!theirs.TryGetValue(name, out KoSitVerdict? verdict))
            {
                continue;
            }

            ValidationReport ours = german.Library.Validate(File.ReadAllText(path));

            results.Add(new Result(
                name,
                verdict.Fired,
                verdict.Errors,
                ours.Messages.Select(message => message.RuleIdentifier).ToHashSet(StringComparer.Ordinal),
                ours.Errors.Select(message => message.RuleIdentifier).ToHashSet(StringComparer.Ordinal)));
        }

        Results = results;
        Ran = true;
    }

    public bool Ran { get; }

    public string WhyNot { get; } = string.Empty;

    public IReadOnlyList<Result> Results { get; } = [];
}

/// <summary>What both engines said about one document.</summary>
/// <remarks>Named apart from the test class so the analyzer's rule about nested types is satisfied.</remarks>
public sealed record Result(
    string Name,
    IReadOnlySet<string> KoSitFired,
    IReadOnlySet<string> KoSitErrors,
    IReadOnlySet<string> WeFired,
    IReadOnlySet<string> WeErrors);
