using International.EInvoicing.Profiles;
using International.EInvoicing.Validation;
using International.EInvoicing.Validation.Schematron;
using International.EInvoicing.Validation.Schematron.XPath;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Validation.Schematron.Tests;

/// <summary>
/// Two ways a published rule set could load, run, and judge nothing at all.
/// </summary>
/// <remarks>
/// Both were found by pointing this engine at Peppol's Invoice Response rules, and both are the same kind of
/// failure: the engine reports that it ran, and nothing it was asked to check was checked. The report told
/// the truth in one case — "no rule in this set matched anything" — and that message is the only reason the
/// gap was visible at all.
/// </remarks>
public class WildcardNameTests
{
    /// <summary>
    /// <c>*:name</c> — that local name in whatever namespace, which XPath 2.0 allows and this engine used to
    /// refuse outright.
    /// </summary>
    [Theory]
    [InlineData("not(@*:schemaLocation)")]
    [InlineData("count(//*:Party)")]
    [InlineData("*:ID")]
    public void AWildcardPrefixIsAName(string expression) =>
        Should.NotThrow(() => XPathParser.Parse(expression));

    [Fact]
    public void AndMatchesTheLocalNameInAnyNamespace()
    {
        SchematronRuleSet rules = SchematronRuleSet.Load(
            """
            <schema xmlns="http://purl.oclc.org/dsdl/schematron">
              <ns prefix="a" uri="urn:a"/>
              <pattern>
                <rule context="/a:Root">
                  <assert id="ANY-NS" test="not(*:Forbidden)">no element called Forbidden, wherever it lives</assert>
                </rule>
              </pattern>
            </schema>
            """,
            "wildcard",
            "1.0");

        Failures(rules, "<Root xmlns=\"urn:a\"><Fine/></Root>").ShouldBeEmpty();

        Failures(rules, "<Root xmlns=\"urn:a\"><Forbidden xmlns=\"urn:somewhere-else\"/></Root>")
            .ShouldContain("ANY-NS");
    }

    /// <summary>
    /// A publisher who ships only the compiled form hands you a stylesheet, and reading one as source
    /// Schematron finds no patterns at all.
    /// </summary>
    /// <remarks>
    /// Nothing throws, nothing is reported at load time, and the rule set validates every document
    /// perfectly. OpenPEPPOL publishes the Invoice Response rules that way, so a caller pointing
    /// <c>AddRulesFromFile</c> at the artefact they were given had a rule set that judged nothing.
    /// </remarks>
    [Fact]
    public void ACompiledRuleSetIsRecognisedByWhatItIsRatherThanByItsFileName()
    {
        SchematronRuleSet rules = SchematronRuleSet.Load(Compiled, "compiled", "1.0");

        rules.AssertionCount.ShouldBe(1);

        Failures(rules, "<Root xmlns=\"urn:a\"/>").ShouldContain("FROM-COMPILED");
    }

    private static IReadOnlyList<string> Failures(SchematronRuleSet rules, string xml) =>
        [.. new SchematronValidator().Validate(xml, rules).Messages.Select(message => message.RuleIdentifier)];

    private const string Compiled = """
        <xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
                        xmlns:svrl="http://purl.oclc.org/dsdl/svrl"
                        xmlns:a="urn:a"
                        version="2.0">
          <xsl:template match="/a:Root" mode="M1">
            <svrl:fired-rule context="/a:Root"/>
            <xsl:choose>
              <xsl:when test="a:Required"/>
              <xsl:otherwise>
                <svrl:failed-assert test="a:Required">
                  <xsl:attribute name="id">FROM-COMPILED</xsl:attribute>
                  <xsl:attribute name="flag">fatal</xsl:attribute>
                  <svrl:text>Required is required</svrl:text>
                </svrl:failed-assert>
              </xsl:otherwise>
            </xsl:choose>
          </xsl:template>
        </xsl:stylesheet>
        """;
}
