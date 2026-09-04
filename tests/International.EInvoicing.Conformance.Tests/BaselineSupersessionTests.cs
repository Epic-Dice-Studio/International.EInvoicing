using International.EInvoicing.Profiles;
using International.EInvoicing.Validation.En16931;
using International.EInvoicing.Validation.Schematron;
using International.EInvoicing.Validation.XRechnung;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Conformance.Tests;

/// <summary>
/// Whether a rule set may supersede EN 16931 is a claim about its artefact, and this is what checks it.
/// </summary>
/// <remarks>
/// <para>
/// A profile's rule set supersedes the base only if it actually carries the base's rules. Factur-X and
/// <c>GLOBALUBL.BE</c> do; XRechnung does not, and the KoSIT validator runs EN 16931 beside it as a separate
/// step. Getting that backwards has a cost in each direction: claim it falsely and the library quietly stops
/// checking almost everything, omit it where it is true and valid invoices are rejected for obeying their
/// own specification. Both have happened here, and the second was found by the KoSIT cross-check.
/// </para>
/// <para>
/// So the declaration is not left to memory. Each rule set that claims supersession must demonstrably share
/// the base's rules, and each that does not must demonstrably not.
/// </para>
/// </remarks>
public class BaselineSupersessionTests
{
    [Fact]
    public void XRechnungCarriesNoneOfTheBaseAndSoMustNotSupersedeIt()
    {
        foreach (DocumentSyntax syntax in (DocumentSyntax[])[DocumentSyntax.Ubl, DocumentSyntax.Cii])
        {
            Shared(XRechnungRules.For(syntax), syntax).ShouldBe(
                0,
                $"XRechnung {syntax} carries none of EN 16931 — KoSIT runs the two as separate steps, and a "
                + "rule set that superseded the base here would stop checking almost everything");
        }
    }

    [Fact]
    public void FacturXCarriesTheBaseAndSoMaySupersedeIt()
    {
        string directory = Path.Combine(
            Corpora.RepositoryRoot(), "specs", "national", "zugferd", "schematron");

        Assert.SkipWhen(!Directory.Exists(directory), "run build/fetch-specs.sh national");

        string newest = Directory.EnumerateDirectories(directory).Order(StringComparer.Ordinal).Last();

        foreach (string file in (string[])["FACTUR-X_EN16931.xslt", "FACTUR-X_EXTENDED.xslt"])
        {
            string path = Path.Combine(newest, file);
            Assert.SkipWhen(!File.Exists(path), $"{file} was not fetched");

            SchematronRuleSet rules = SchematronRuleSet.Load(File.ReadAllText(path), file, "fetched");

            Shared(rules, DocumentSyntax.Cii).ShouldBeGreaterThan(
                100,
                $"{file} is supposed to carry the EN 16931 rules its profile keeps");
        }
    }

    [Fact]
    public void AndGlobalUblBeCarriesNearlyAllOfIt()
    {
        string directory = Path.Combine(
            Corpora.RepositoryRoot(), "specs", "national", "ublbe", "schematron");

        Assert.SkipWhen(!Directory.Exists(directory), "run build/fetch-specs.sh national");

        string? newest = Directory
            .EnumerateFiles(directory, "GLOBALUBL.BE*.xslt", SearchOption.AllDirectories)
            .Order(StringComparer.Ordinal)
            .LastOrDefault();

        Assert.SkipWhen(newest is null, "GLOBALUBL.BE was not fetched");

        SchematronRuleSet rules = SchematronRuleSet.Load(File.ReadAllText(newest!), "GLOBALUBL.BE", "fetched");
        int baseline = En16931Rules.For(DocumentSyntax.Ubl).RuleIdentifiers.Count;

        Shared(rules, DocumentSyntax.Ubl).ShouldBeGreaterThan(
            baseline / 2,
            "GLOBALUBL.BE bundles the EN 16931 rules and adapts several of them");
    }

    private static int Shared(SchematronRuleSet rules, DocumentSyntax syntax)
    {
        IReadOnlyCollection<string> baseline = En16931Rules.For(syntax).RuleIdentifiers;

        return rules.RuleIdentifiers.Count(id => baseline.Contains(id, StringComparer.OrdinalIgnoreCase));
    }
}
