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

        // The unit-case folders only: test/testfiles holds sample documents, which name no rule and are
        // exercised elsewhere.
        string[] uncarried =
        [
            .. ((string[])["Invoice-unit-UBL", "CreditNote-unit-UBL", "cii"])
                .Select(folder => Path.Combine(root, folder))
                .Where(Directory.Exists)
                .SelectMany(folder => Directory.EnumerateFiles(folder, "*.xml"))
                .Where(file => !Carried(file, file.Contains($"{Path.DirectorySeparatorChar}cii{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    ? DocumentSyntax.Cii
                    : DocumentSyntax.Ubl))
                .Select(Path.GetFileNameWithoutExtension)
                .Select(name => name!)
                .Order(StringComparer.Ordinal),
        ];

        // Only rules the 1.3.16 artefacts genuinely lack: BR-CO-25, and the IGIC and IPSI families.
        uncarried.ShouldAllBe(
            name => name.StartsWith("BR-CO-25", StringComparison.Ordinal)
                || name.StartsWith("BR-IG-", StringComparison.Ordinal)
                || name.StartsWith("BR-IP-", StringComparison.Ordinal),
            $"a case stopped being judged for a reason nobody expected: {string.Join(", ", uncarried)}");

        covered.Length.ShouldBeGreaterThan(
            (int)(named.Length * 0.8),
            "the cases and the artefacts come from the same tag, so nearly every case should name a rule "
            + $"the artefacts carry; these do not: {string.Join(", ", named.Except(covered, StringComparer.OrdinalIgnoreCase).Take(20))}");
    }

    /// <summary>
    /// A case naming a rule the artefacts of this version do not carry cannot be judged, and says so.
    /// </summary>
    /// <remarks>
    /// <c>BR-CO-25</c> is the one: the standard publishes a unit case for it at this tag and its own
    /// Schematron does not implement it, so there is no rule to fire and nothing to conclude. Measured
    /// against <see cref="SchematronRuleSet.RuleIdentifiers"/> rather than listed by hand, so a rule that
    /// arrives later stops being skipped without anyone remembering to look.
    /// </remarks>
    private static bool Carried(string path, DocumentSyntax syntax)
    {
        IReadOnlyCollection<string> carried = En16931Rules.For(syntax).RuleIdentifiers;

        // A file may be a variant of a case rather than a case of its own: BR-S-08-1, BR-S-08-2 and
        // BR-S-08-3 all exercise BR-S-08. Strip the trailing counter before deciding the rule is missing,
        // or three quarters of the skips are cases that could perfectly well have been judged.
        for (string name = Path.GetFileNameWithoutExtension(path); name.Length > 0;)
        {
            if (carried.Contains(name, StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }

            int dash = name.LastIndexOf('-');
            if (dash < 0 || !name[(dash + 1)..].All(char.IsAsciiDigit))
            {
                return false;
            }

            name = name[..dash];
        }

        return false;
    }

    private static void Measure(string path, DocumentSyntax syntax)
    {
        Assert.SkipWhen(path.Length == 0, "run build/fetch-specs.sh en16931");

        Assert.SkipUnless(
            Carried(path, syntax),
            $"the EN 16931 artefacts of this version carry no {Path.GetFileNameWithoutExtension(path)} rule");

        // PreserveWhitespace, and DisableFormatting below: a unit case is a document somebody wrote,
        // and reformatting it before judging it changes what the rules see.
        XDocument set = XDocument.Load(path, LoadOptions.PreserveWhitespace);
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
                Compare(
                    expectations,
                    [.. validator.Validate(document.ToString(SaveOptions.DisableFormatting), rules).Messages]));
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
