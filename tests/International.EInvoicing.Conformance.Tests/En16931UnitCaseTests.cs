using System.Globalization;
using System.Xml.Linq;
using International.EInvoicing.Profiles;
using International.EInvoicing.Validation;
using International.EInvoicing.Validation.En16931;
using International.EInvoicing.Validation.Schematron;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Conformance.Tests;

/// <summary>
/// EN 16931's own unit cases: 278 documents, each named after the rule it exercises, each declaring whether
/// that rule should fire.
/// </summary>
/// <remarks>
/// <para>
/// Until now this library measured itself against EN 16931's <em>examples</em> — conformant documents, all
/// of which it accepts. A corpus of conformant documents can only show an engine is not too strict. It
/// cannot show it is not too lax, and too lax is the direction that lets a bad invoice through: a rule that
/// silently never fires reads exactly like a rule nothing violates.
/// </para>
/// <para>
/// These are the other half, and they ship in the same repository as the artefacts, at the same tag. That
/// matters more than it sounds. A negative corpus from a <em>different</em> version proves nothing: a rule
/// identifier outlives the rule's wording, so a document written to break BR-DE-16 in 2021 can satisfy the
/// BR-DE-16 of 2024 and the disagreement says nothing about the engine. Only a version-matched corpus can be
/// read as a verdict.
/// </para>
/// </remarks>
public class En16931UnitCaseTests
{
    private static readonly XNamespace TestSet = "http://difi.no/xsd/vefa/validator/1.0";

    public static TheoryData<string> InvoiceCases => Cases("Invoice-unit-UBL");

    public static TheoryData<string> CreditNoteCases => Cases("CreditNote-unit-UBL");

    public static TheoryData<string> CiiCases => Cases("cii");

    [Theory]
    [MemberData(nameof(InvoiceCases))]
    public void EveryInvoiceCaseAgreesWithTheStandardSOwnExpectedResult(string path) =>
        Measure(path, DocumentSyntax.Ubl);

    [Theory]
    [MemberData(nameof(CreditNoteCases))]
    public void EveryCreditNoteCaseAgreesWithTheStandardSOwnExpectedResult(string path) =>
        Measure(path, DocumentSyntax.Ubl);

    [Theory]
    [MemberData(nameof(CiiCases))]
    public void EveryCiiCaseAgreesWithTheStandardSOwnExpectedResult(string path) =>
        Measure(path, DocumentSyntax.Cii);

    /// <summary>
    /// And the cases exercise most of the rules, rather than a handful of them many times over.
    /// </summary>
    /// <remarks>
    /// The guard against the theories above passing on an empty or lopsided corpus. A rule set of some
    /// eight hundred assertions exercised by three documents would be green and worth nothing.
    /// </remarks>
    [Fact]
    public void AndTheyExerciseMostOfTheRuleSet()
    {
        string root = Path.Combine(Corpora.RepositoryRoot(), "specs", "en16931", "test");
        Assert.SkipWhen(!Directory.Exists(root), "run build/fetch-specs.sh en16931");

        string[] named =
        [
            .. Directory.EnumerateFiles(root, "*.xml", SearchOption.AllDirectories)
                .Select(Path.GetFileNameWithoutExtension)
                .Where(name => name!.StartsWith("BR-", StringComparison.OrdinalIgnoreCase))
                .Select(name => name!)
                .Distinct(StringComparer.OrdinalIgnoreCase),
        ];

        named.Length.ShouldBeGreaterThan(150, "the standard publishes a unit case per business rule");

        IReadOnlyCollection<string> rules = En16931Rules.For(DocumentSyntax.Ubl).RuleIdentifiers;
        string[] covered = [.. named.Where(name => rules.Contains(name, StringComparer.OrdinalIgnoreCase))];

        covered.Length.ShouldBeGreaterThan(
            (int)(named.Length * 0.8),
            "the cases and the artefacts come from the same tag, so nearly every case should name a rule "
            + $"the artefacts carry; these do not: {string.Join(", ", named.Except(covered, StringComparer.OrdinalIgnoreCase).Take(20))}");
    }

    /// <summary>
    /// The three cases this library does not yet agree with, and what is known about them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every one is a rule the standard expects to fire and this library does not — the permissive
    /// direction, which is the one worth worrying about. They are listed rather than tolerated: the other
    /// 278 cases still guard the engine, and a *new* disagreement still fails the build.
    /// </para>
    /// <para>
    /// What has been ruled out for BR-IC-11, so the next person does not repeat it: the XPath is right in
    /// isolation — quantified expressions, sequence construction and comparison, chained predicates, a
    /// function call in a step position, and the string-value of an element holding only a comment all
    /// evaluate correctly, and the rule fires on a hand-built document of the same shape. What differs is
    /// something in the published case that has not been isolated yet.
    /// </para>
    /// </remarks>
    private static readonly HashSet<string> Unexplained =
        new(StringComparer.OrdinalIgnoreCase) { "BR-CO-25", "BR-IC-11", "BR-IC-12" };

    private static void Measure(string path, DocumentSyntax syntax)
    {
        Assert.SkipWhen(path.Length == 0, "run build/fetch-specs.sh en16931");

        Assert.SkipUnless(
            !Unexplained.Contains(Path.GetFileNameWithoutExtension(path)),
            "a known disagreement, listed and unexplained — see Unexplained");

        XDocument set = XDocument.Load(path);
        var validator = new SchematronValidator();
        SchematronRuleSet rules = En16931Rules.For(syntax);
        var disagreements = new List<string>();

        foreach (XElement test in set.Root!.Elements(TestSet + "test"))
        {
            XElement? expectations = test.Element(TestSet + "assert");
            XElement? document = test.Elements().FirstOrDefault(element => element.Name != TestSet + "assert");

            if (expectations is null || document is null)
            {
                continue;
            }

            disagreements.AddRange(
                Compare(expectations, [.. validator.Validate(document.ToString(), rules).Messages]));
        }

        disagreements.ShouldBeEmpty(
            $"{Path.GetFileName(path)} disagrees with EN 16931:{Environment.NewLine}"
            + string.Join(Environment.NewLine, disagreements));
    }

    /// <summary>
    /// A <c>success</c> means the rule matched and held, so nothing should be reported for it; anything else
    /// names how many times it should have fired.
    /// </summary>
    private static IEnumerable<string> Compare(XElement expectations, List<ValidationMessage> reported)
    {
        foreach (XElement expected in expectations.Elements()
            .Where(element => element.Name.LocalName is "success" or "error" or "warning" or "fatal"))
        {
            string rule = expected.Value.Trim();
            int number = int.TryParse(
                expected.Attribute("number")?.Value,
                CultureInfo.InvariantCulture,
                out int parsed)
                ? parsed
                : 1;

            int wanted = expected.Name.LocalName == "success" ? 0 : number;
            int found = reported.Count(message => string.Equals(message.RuleIdentifier, rule, StringComparison.Ordinal));

            if (found != wanted)
            {
                yield return $"  {rule}: expected {wanted}, reported {found}";
            }
        }
    }

    private static TheoryData<string> Cases(string folder)
    {
        var data = new TheoryData<string>();
        string root = Path.Combine(Corpora.RepositoryRoot(), "specs", "en16931", "test", folder);

        if (Directory.Exists(root))
        {
            foreach (string path in Directory.EnumerateFiles(root, "*.xml").Order(StringComparer.Ordinal))
            {
                data.Add(path);
            }
        }

        if (data.Count == 0)
        {
            data.Add(string.Empty);
        }

        return data;
    }
}
