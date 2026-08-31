using Shouldly;
using Xunit;

namespace International.EInvoicing.Countries.Singapore.Tests;

/// <summary>
/// The Singaporean tax category codes, checked against the rule they were taken from.
/// </summary>
public class SgTaxCategoryTests
{
    [Fact]
    public void TheListMatchesTheRuleItWasTakenFrom()
    {
        string path = Path.Combine(
            RepositoryRoot(), "specs", "peppol", "pint", "schematron", "pint-sg");

        Assert.SkipWhen(
            !Directory.Exists(path),
            "The PINT artefacts are not present; run build/fetch-specs.sh pint.");

        string rules = File.ReadAllText(Directory
            .EnumerateFiles(path, "PINT-jurisdiction-aligned-rules.xslt", SearchOption.AllDirectories)
            .Order(StringComparer.Ordinal)
            .Last());

        int rule = rules.IndexOf("BR-CL-17-GST-SG", StringComparison.Ordinal);
        rule.ShouldBeGreaterThan(0);

        // The rule tests membership of a space-delimited list, which is the list itself.
        int opened = rules.LastIndexOf("contains( '", rule, StringComparison.Ordinal) + "contains( '".Length;
        string[] published = rules[opened..rules.IndexOf('\'', opened)]
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

        SgTaxCategory.All.ShouldBe(published, ignoreOrder: false);
    }

    /// <summary>The code every European example uses is the one Singapore refuses.</summary>
    [Fact]
    public void TheObviousCodeIsTheOneSingaporeRefuses()
    {
        SgTaxCategory.IsAllowed("S").ShouldBeFalse();
        SgTaxCategory.IsAllowed(SgTaxCategory.StandardRated).ShouldBeTrue();
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "International.EInvoicing.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("The repository root was not found.");
    }
}
