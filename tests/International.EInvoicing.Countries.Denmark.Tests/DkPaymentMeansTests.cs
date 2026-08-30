using International.EInvoicing.Countries.Denmark;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Countries.Denmark.Tests;

/// <summary>
/// The allowed payment means, checked against the rule they were taken from rather than against a second
/// transcription of it.
/// </summary>
public class DkPaymentMeansTests
{
    [Fact]
    public void TheListMatchesTheRuleItWasTakenFrom()
    {
        string path = Path.Combine(RepositoryRoot(), "specs", "peppol", "rules", "PEPPOL-EN16931-UBL.sch");

        Assert.SkipWhen(
            !File.Exists(path),
            "The Peppol artefacts are not present; run build/fetch-specs.sh peppol.");

        string rules = File.ReadAllText(path);
        int rule = rules.IndexOf("id=\"DK-R-005\"", StringComparison.Ordinal);
        rule.ShouldBeGreaterThan(0);

        int opened = rules.IndexOf("contains(' ", rule, StringComparison.Ordinal) + "contains(' ".Length;
        string[] published = rules[opened..rules.IndexOf('\'', opened)]
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

        DkPaymentMeans.All.ShouldBe(published, ignoreOrder: false);
    }

    [Fact]
    public void TheObviousCodeIsTheOneDenmarkRefuses()
    {
        DkPaymentMeans.IsAllowed("30").ShouldBeFalse();               // plain credit transfer
        DkPaymentMeans.IsAllowed(DkPaymentMeans.SepaCreditTransfer).ShouldBeTrue();
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
