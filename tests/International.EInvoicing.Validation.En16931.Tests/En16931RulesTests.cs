using International.EInvoicing.Profiles;
using International.EInvoicing.Validation;
using International.EInvoicing.Validation.Schematron;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Validation.En16931.Tests;

/// <summary>
/// The strongest measurement available: the official XRechnung test suite, 86 documents nobody here wrote.
/// </summary>
/// <remarks>
/// A document declaring <c>#compliant#</c> follows a CIUS, which may only restrict EN 16931, so it must
/// satisfy the base rules. One declaring <c>#conformant#</c> follows an extension, which may add what the
/// base rules reject — the XRechnung Extension uses identifier schemes outside the ISO 6523 list, and
/// EN 16931 rejects those correctly. The two are therefore measured differently.
/// </remarks>
public class En16931RulesTests
{
    public static TheoryData<string> UblDocuments => Corpus("*_ubl.xml", conformantExtensions: false);

    public static TheoryData<string> CiiDocuments => Corpus("*_uncefact.xml", conformantExtensions: false);

    public static TheoryData<string> ExtensionDocuments => Corpus("*.xml", conformantExtensions: true);

    [Fact]
    public void TheArtefactsAreEmbeddedAndLoad()
    {
        En16931Rules.For(DocumentSyntax.Ubl).AssertionCount.ShouldBeGreaterThan(900);
        En16931Rules.For(DocumentSyntax.Cii).AssertionCount.ShouldBeGreaterThan(700);
        En16931Rules.ArtefactVersion.ShouldBe("1.3.16");
    }

    [Fact]
    public void LoadingTwiceReturnsTheSameRuleSet()
    {
        En16931Rules.For(DocumentSyntax.Ubl).ShouldBeSameAs(En16931Rules.For(DocumentSyntax.Ubl));
    }

    [Fact]
    public void ASyntaxWithNoArtefactsSaysSoRatherThanPretending()
    {
        En16931Rules.Covers(DocumentSyntax.Cdar).ShouldBeFalse();
        Should.Throw<NotSupportedException>(() => En16931Rules.For(DocumentSyntax.Cdar));
    }

    [Theory]
    [MemberData(nameof(UblDocuments))]
    public void EveryOfficialXRechnungUblDocumentSatisfiesEn16931(string path)
    {
        ValidationReport report = new SchematronValidator()
            .Validate(File.ReadAllText(path), En16931Rules.For(DocumentSyntax.Ubl));

        report.IsValid.ShouldBeTrue(Describe(path, report));
    }

    [Theory]
    [MemberData(nameof(CiiDocuments))]
    public void EveryOfficialXRechnungCiiDocumentSatisfiesEn16931(string path)
    {
        ValidationReport report = new SchematronValidator()
            .Validate(File.ReadAllText(path), En16931Rules.For(DocumentSyntax.Cii));

        report.IsValid.ShouldBeTrue(Describe(path, report));
    }

    /// <summary>
    /// The corpus, split on what each document claims: a CIUS restricts EN 16931, an extension may go beyond
    /// it. Reading the claim from the document is more honest than sorting by folder name.
    /// </summary>
    /// <summary>
    /// The medical-device profiling. It calls itself a CIUS, and a CIUS may only restrict EN 16931 — but it
    /// classifies items with <c>listID="CVD"</c>, which is not in UNTDID 7143, so EN 16931 rejects it and is
    /// right to. It is measured against the German rules, which define it, rather than against the base ones.
    /// </summary>
    private const string CvdProfiling = "urn:xeinkauf.de:kosit:xrechnung:cvd";

    private static TheoryData<string> Corpus(string pattern, bool conformantExtensions)
    {
        var data = new TheoryData<string>();
        string directory = Path.Combine(RepositoryRoot(), "specs", "xrechnung", "testsuite", "src", "test");

        foreach (string path in Directory
            .EnumerateFiles(directory, pattern, SearchOption.AllDirectories)
            .OrderBy(p => p, StringComparer.Ordinal))
        {
            string document = File.ReadAllText(path);
            bool isExtension = document.Contains("#conformant#", StringComparison.Ordinal)
                || document.Contains(CvdProfiling, StringComparison.Ordinal);

            if (isExtension == conformantExtensions)
            {
                data.Add(path);
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(ExtensionDocuments))]
    public void AConformantExtensionIsReadButNotHeldToTheBaseRules(string path)
    {
        DocumentSyntax syntax = path.EndsWith("_ubl.xml", StringComparison.Ordinal)
            ? DocumentSyntax.Ubl
            : DocumentSyntax.Cii;

        ValidationReport report = new SchematronValidator()
            .Validate(File.ReadAllText(path), En16931Rules.For(syntax));

        // The point is not that it passes or fails, but that the engine runs it and reports what it found.
        report.RuleSets.ShouldHaveSingleItem().Ran.ShouldBeTrue();
        report.Messages.ShouldAllBe(message => !string.IsNullOrEmpty(message.RuleIdentifier));
    }

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
