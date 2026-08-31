using Shouldly;
using Xunit;

namespace International.EInvoicing.Countries.Malaysia.Tests;

/// <summary>
/// The Malaysiaan tax category codes, checked against the rule they were taken from.
/// </summary>
public class MyTaxCategoryTests
{
    [Fact]
    public void TheListMatchesTheRuleItWasTakenFrom()
    {
        string path = Path.Combine(
            RepositoryRoot(), "specs", "peppol", "pint", "schematron", "pint-my");

        Assert.SkipWhen(
            !Directory.Exists(path),
            "The PINT artefacts are not present; run build/fetch-specs.sh pint.");

        string rules = File.ReadAllText(Directory
            .EnumerateFiles(path, "PINT-jurisdiction-aligned-rules.xslt", SearchOption.AllDirectories)
            .Order(StringComparer.Ordinal)
            .Last());

        int rule = rules.IndexOf("aligned-ibrp-cl-01-my", StringComparison.Ordinal);
        rule.ShouldBeGreaterThan(0);

        // The rule tests membership of a space-delimited list, which is the list itself.
        // The list is in the test the compiler kept on the assertion, just before the identifier it names.
        int opened = rules.LastIndexOf("contains( ' ", rule, StringComparison.Ordinal) + "contains( ' ".Length;
        string[] published = rules[opened..rules.IndexOf('\'', opened)]
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

        MyTaxCategory.All.ShouldBe(published, ignoreOrder: false);
    }

    /// <summary>The code every European example uses is the one Malaysia refuses.</summary>
    [Fact]
    public void TheObviousCodeIsTheOneMalaysiaRefuses()
    {
        MyTaxCategory.IsAllowed("S").ShouldBeFalse();
        MyTaxCategory.IsAllowed(MyTaxCategory.SalesTax).ShouldBeTrue();
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
