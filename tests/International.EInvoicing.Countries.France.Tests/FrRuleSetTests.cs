using International.EInvoicing.Profiles;
using International.EInvoicing.Validation;
using International.EInvoicing.Validation.Schematron;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Countries.France.Tests;

/// <summary>
/// The French rule sets are published by the DGFiP and carried by a repository that declares no licence, so
/// they are fetched rather than redistributed — <c>build/fetch-specs.sh france</c>.
/// </summary>
/// <remarks>
/// These tests measure the engine against them when they are present and say nothing when they are not,
/// rather than failing a checkout that has not fetched them. What they prove is that the engine can run the
/// French rules at all: every expression parses, and no rule is left unevaluable.
/// </remarks>
public class FrRuleSetTests
{
    private static string? RulesDirectory
    {
        get
        {
            string path = Path.Combine(RepositoryRoot(), "specs", "fr-dse", "rules");
            return Directory.Exists(path) ? path : null;
        }
    }

    public static TheoryData<string> RuleSets
    {
        get
        {
            var data = new TheoryData<string>();

            foreach (string path in RulesDirectory is null
                ? []
                : Directory.EnumerateFiles(RulesDirectory, "*.sch", SearchOption.AllDirectories))
            {
                data.Add(path);
            }

            // A theory with no cases fails discovery, so an absent corpus is one case that skips itself.
            if (data.Count == 0)
            {
                data.Add(string.Empty);
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(RuleSets))]
    public void EveryFrenchRuleSetLoadsWithEveryExpressionParsed(string path)
    {
        Assert.SkipWhen(path.Length == 0, "The French artefacts are not present; run build/fetch-specs.sh france.");

        SchematronRuleSet rules = SchematronRuleSet.Load(
            File.ReadAllText(path),
            Path.GetFileNameWithoutExtension(path),
            "1.4.0.03");

        rules.AssertionCount.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void TheFrenchInvoiceProfileIsAnExtensionNotACius()
    {
        // #conformant#, not #compliant#: a French invoice may carry what the base rules reject.
        FrProfiles.ExtendedCtcFrUbl.Id.Value.ShouldContain("#conformant#");
        FrProfiles.ExtendedCtcFrUbl.Id.Value.ShouldBe(
            "urn:cen.eu:en16931:2017#conformant#urn.cpro.gouv.fr:1p0:extended-ctc-fr");
        FrProfiles.ExtendedCtcFrUbl.Parent.ShouldBe(KnownProfiles.En16931Ubl.Id);
    }

    [Fact]
    public void BothSyntaxesCarryTheSameFrenchIdentifier()
    {
        FrProfiles.ExtendedCtcFrCii.Id.ShouldBe(FrProfiles.ExtendedCtcFrUbl.Id);
        FrProfiles.ExtendedCtcFrCii.Syntax.ShouldBe(DocumentSyntax.Cii);
    }

    [Fact]
    public void AFrenchInvoiceResolvesExactlyOnceTheProfilesAreRegistered()
    {
        var resolver = new ProfileResolver(new ProfileRegistry(FrProfiles.All));

        ProfileResolution resolution = resolver.Resolve(FrProfiles.ExtendedCtcFrUbl.Id, DocumentSyntax.Ubl);

        resolution.IsExact.ShouldBeTrue();
        resolution.Profile!.Name.ShouldBe("Extended CTC FR");
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
}
