using System.Globalization;
using System.Xml.Linq;
using International.EInvoicing.Profiles;
using International.EInvoicing.Validation;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Validation.Schematron.Tests;

/// <summary>
/// Measures the engine against Peppol's own unit corpus.
/// </summary>
/// <remarks>
/// <para>
/// The Peppol artefacts declare no licence, so this library does not redistribute them: they are fetched with
/// <c>build/fetch-specs.sh peppol</c> into a git-ignored folder, and these tests skip and say so when they
/// are absent.
/// </para>
/// <para>
/// The corpus is the strongest measurement available for Peppol. Each case is a document fragment with the
/// number of times a named rule is expected to fire, published by Peppol themselves — so agreement is not a
/// matter of opinion.
/// </para>
/// </remarks>
public class PeppolConformanceTests
{
    private static readonly XNamespace TestSet = "http://difi.no/xsd/vefa/validator/1.0";

    public static TheoryData<string> UnitTests => Corpus(DocumentSyntax.Ubl);

    public static TheoryData<string> CiiUnitTests => Corpus(DocumentSyntax.Cii);

    [Theory]
    [MemberData(nameof(UnitTests))]
    public void EveryUblCaseAgreesWithPeppolsOwnExpectedResult(string path) => Measure(path, DocumentSyntax.Ubl);

    [Theory]
    [MemberData(nameof(CiiUnitTests))]
    public void EveryCiiCaseAgreesWithPeppolsOwnExpectedResult(string path) => Measure(path, DocumentSyntax.Cii);

    [Fact]
    public void TheRuleSetsLoadInFull()
    {
        SkipWithoutArtefacts();

        Rules(DocumentSyntax.Ubl, "PEPPOL").AssertionCount.ShouldBeGreaterThan(100);
        Rules(DocumentSyntax.Cii, "PEPPOL").AssertionCount.ShouldBeGreaterThan(80);
    }

    private static void Measure(string path, DocumentSyntax syntax)
    {
        Assert.SkipWhen(path.Length == 0, "The Peppol artefacts are not present; run build/fetch-specs.sh peppol.");

        XDocument set = XDocument.Load(path);
        var validator = new SchematronValidator();
        SchematronRuleSet peppol = Rules(syntax, "PEPPOL");
        SchematronRuleSet cen = Rules(syntax, "CEN");
        var disagreements = new List<string>();

        foreach (XElement test in set.Root!.Elements(TestSet + "test"))
        {
            XElement? expectations = test.Element(TestSet + "assert");
            XElement? document = test.Elements().FirstOrDefault(element => element.Name != TestSet + "assert");

            if (expectations is null || document is null)
            {
                continue;
            }

            string xml = document.ToString();
            List<ValidationMessage> reported =
            [
                .. validator.Validate(xml, peppol).Messages,
                .. validator.Validate(xml, cen).Messages,
            ];

            disagreements.AddRange(Compare(expectations, reported));
        }

        disagreements.ShouldBeEmpty(
            $"{Path.GetFileName(path)} disagrees with Peppol:{Environment.NewLine}"
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

    private static SchematronRuleSet Rules(DocumentSyntax syntax, string family)
    {
        string name = $"{family}-EN16931-{(syntax == DocumentSyntax.Ubl ? "UBL" : "CII")}.sch";

        return SchematronRuleSet.Load(
            File.ReadAllText(Path.Combine(PeppolDirectory, "rules", name)),
            $"{family} (Peppol {syntax})",
            "3.0");
    }

    private static TheoryData<string> Corpus(DocumentSyntax syntax)
    {
        var data = new TheoryData<string>();
        string directory = Path.Combine(
            PeppolDirectory,
            $"unit-{(syntax == DocumentSyntax.Ubl ? "UBL" : "CII")}-PEPPOL");

        if (Directory.Exists(directory))
        {
            foreach (string path in Directory
                .EnumerateFiles(directory, "*.xml")
                .OrderBy(path => path, StringComparer.Ordinal))
            {
                data.Add(path);
            }
        }

        // A theory with no cases fails discovery, so an absent corpus is one case that skips itself.
        if (data.Count == 0)
        {
            data.Add(string.Empty);
        }

        return data;
    }

    private static void SkipWithoutArtefacts() =>
        Assert.SkipWhen(
            !Directory.Exists(Path.Combine(PeppolDirectory, "rules")),
            "The Peppol artefacts are not present; run build/fetch-specs.sh peppol.");

    private static string PeppolDirectory => Path.Combine(RepositoryRoot(), "specs", "peppol");

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
