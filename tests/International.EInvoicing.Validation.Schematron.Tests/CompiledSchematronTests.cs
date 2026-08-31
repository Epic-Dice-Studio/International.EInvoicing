using International.EInvoicing.Validation.Schematron;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Validation.Schematron.Tests;

/// <summary>
/// Reading a rule set out of a Schematron that was compiled to XSLT, and proving it says the same thing.
/// </summary>
/// <remarks>
/// <para>
/// The claim being tested is uncomfortable to make on trust: that reading compiled rules recovers the
/// publisher's rules rather than an approximation of them. It is testable because one rule set exists in
/// both forms at the same version — EN 16931 1.3.16, whose source Schematron this repository ships and
/// whose compiled stylesheet the same publisher distributes.
/// </para>
/// <para>
/// So the two are read and compared. Every assertion identifier, every test expression and every severity
/// must match. If the compiler ever changed its output shape, or this reader ever guessed, this fails.
/// </para>
/// </remarks>
public class CompiledSchematronTests
{
    public static TheoryData<string> Syntaxes => new("ubl", "cii");

    [Theory]
    [MemberData(nameof(Syntaxes))]
    public void ACompiledRuleSetHoldsTheSameAssertionsAsItsSource(string syntax)
    {
        SchematronRuleSet compiled = Compiled(syntax);
        SchematronRuleSet source = Source(syntax);

        Assertion[] fromCompiled = [.. Assertions(compiled)];
        Assertion[] fromSource = [.. Assertions(source)];

        fromCompiled.Length.ShouldBe(
            fromSource.Length,
            $"the compiled {syntax} artefact holds {fromCompiled.Length} assertions, the source {fromSource.Length}");

        fromCompiled.ShouldBe(fromSource, ignoreOrder: true);
    }

    /// <summary>The rule contexts must survive too, since a rule judged in the wrong place is not the rule.</summary>
    [Theory]
    [MemberData(nameof(Syntaxes))]
    public void AndTheSameRuleContexts(string syntax)
    {
        Contexts(Compiled(syntax)).ShouldBe(Contexts(Source(syntax)), ignoreOrder: true);
    }

    /// <summary>
    /// And it runs: the compiled form, put over the official examples, must accept what the source accepts.
    /// </summary>
    [Fact]
    public void AndAcceptsTheDocumentsTheSourceAccepts()
    {
        string directory = Path.Combine(Artefacts.RepositoryRoot, "specs", "en16931", "ubl", "examples");
        var validator = new SchematronValidator();
        SchematronRuleSet compiled = Compiled("ubl");
        SchematronRuleSet source = Source("ubl");

        foreach (string path in Directory.EnumerateFiles(directory, "*.xml").Order(StringComparer.Ordinal))
        {
            string xml = File.ReadAllText(path);

            string[] fromCompiled = [.. Failures(validator, compiled, xml)];
            string[] fromSource = [.. Failures(validator, source, xml)];

            fromCompiled.ShouldBe(fromSource, ignoreOrder: true, Path.GetFileName(path));
        }
    }

    private static IEnumerable<string> Failures(SchematronValidator validator, SchematronRuleSet rules, string xml) =>
        validator.Validate(xml, rules).Messages.Select(message => message.RuleIdentifier).Distinct();

    private static SchematronRuleSet Compiled(string syntax)
    {
        string path = Path.Combine(
            Artefacts.RepositoryRoot,
            "specs",
            "en16931",
            "compiled",
            "1.3.16",
            syntax,
            $"EN16931-{syntax.ToUpperInvariant()}-validation.xslt");

        Assert.SkipWhen(
            !File.Exists(path),
            "The compiled EN 16931 artefact is not present; run build/fetch-specs.sh pint.");

        return CompiledSchematron.Read(File.ReadAllText(path), $"EN 16931 ({syntax}, compiled)", "1.3.16");
    }

    private static SchematronRuleSet Source(string syntax) => syntax == "ubl"
        ? SchematronRuleSet.Load(File.ReadAllText(Artefacts.UblRules), "EN 16931 (UBL)", "1.3.16")
        : SchematronRuleSet.Load(File.ReadAllText(Artefacts.CiiRules), "EN 16931 (CII)", "1.3.16");

    private static IEnumerable<Assertion> Assertions(SchematronRuleSet rules) => rules.Patterns
        .SelectMany(pattern => pattern.Rules)
        .SelectMany(rule => rule.Assertions)
        .Select(assertion => new Assertion(
            assertion.Identifier,
            assertion.Test.ToString() ?? string.Empty,
            assertion.Severity.ToString(),
            assertion.IsReport,
            assertion.Message));

    private static IEnumerable<string> Contexts(SchematronRuleSet rules) => rules.Patterns
        .SelectMany(pattern => pattern.Rules)
        .Select(rule => rule.Context.ToString() ?? string.Empty);

    private sealed record Assertion(
        string Identifier,
        string Test,
        string Severity,
        bool IsReport,
        string Message);
}
