using System.Xml.Linq;
using International.EInvoicing.Validation.Schematron.XPath;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Validation.Schematron.Tests;

/// <summary>
/// <c>every $v in … satisfies …</c> and its <c>some</c> twin.
/// </summary>
/// <remarks>
/// XPath 1.0 has no quantified expression, and the German rules use one: BR-DE-TMP-32 asks whether *every*
/// invoice line states a period. An engine that gets this wrong does not fail loudly — the assertion simply
/// never fires, and a rule that never fires reads exactly like a rule nothing violates.
/// </remarks>
public class XPathQuantifiedTests
{
    private static readonly XDocument Mixed = XDocument.Parse(
        """
        <invoice>
          <line><period>January</period></line>
          <line/>
        </invoice>
        """);

    private static readonly XDocument All = XDocument.Parse(
        """
        <invoice>
          <line><period>January</period></line>
          <line><period>February</period></line>
        </invoice>
        """);

    private static readonly XDocument None = XDocument.Parse("<invoice><line/><line/></invoice>");

    [Fact]
    public void EveryIsFalseWhenOneItemFailsTheTest()
    {
        Boolean(Mixed, "every $line in /invoice/line satisfies $line/period").ShouldBeFalse();
    }

    [Fact]
    public void AndTrueOnlyWhenTheyAllSatisfyIt()
    {
        Boolean(All, "every $line in /invoice/line satisfies $line/period").ShouldBeTrue();
    }

    [Fact]
    public void AndFalseWhenNoneOfThemDoes()
    {
        Boolean(None, "every $line in /invoice/line satisfies $line/period").ShouldBeFalse();
    }

    /// <summary>Over nothing at all it is true, which is what the specification says and is easy to get wrong.</summary>
    [Fact]
    public void AndVacuouslyTrueOverAnEmptySequence()
    {
        Boolean(None, "every $line in /invoice/absent satisfies $line/period").ShouldBeTrue();
    }

    [Fact]
    public void SomeIsTrueAsSoonAsOneItemSatisfiesIt()
    {
        Boolean(Mixed, "some $line in /invoice/line satisfies $line/period").ShouldBeTrue();
        Boolean(None, "some $line in /invoice/line satisfies $line/period").ShouldBeFalse();
    }

    /// <summary>
    /// The shape BR-DE-TMP-32 actually has: a union as the sequence, and the whole thing as the last arm of
    /// an <c>or</c>.
    /// </summary>
    [Fact]
    public void AndItWorksAsTheLastArmOfAnOrOverAUnion()
    {
        Boolean(
            Mixed,
            "/invoice/deliveryDate or /invoice/period "
            + "or (every $line in (/invoice/line | /invoice/creditLine) satisfies $line/period)")
            .ShouldBeFalse();
    }

    private static bool Boolean(XDocument document, string expression)
    {
        var evaluator = new XPathEvaluator(new Dictionary<string, string>(StringComparer.Ordinal));

        return evaluator
            .Evaluate(
                XPathParser.Parse(expression),
                new XPathContext(document, document, new Dictionary<string, XPathValue>(StringComparer.Ordinal)))
            .AsBoolean();
    }
}
