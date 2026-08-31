using System.Xml.Linq;
using International.EInvoicing.Validation.Schematron.XPath;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Validation.Schematron.Tests;

/// <summary>
/// <c>instance of</c>, which asks what kind of thing an expression yielded.
/// </summary>
/// <remarks>
/// It was missing, and what it cost was a whole rule set: OpenPeppol's tax data rules build the path they
/// report a failure at by walking the ancestors and asking each one whether it is an element, and the parser
/// stopped at the first <c>instance of</c> in the file. A rule set that fails to load judges nothing — which
/// is why this engine raises rather than skipping what it cannot read.
/// </remarks>
public class XPathInstanceOfTests
{
    private static readonly XDocument Document = XDocument.Parse(
        """
        <invoice number="2026-0001">
          <line>first</line>
          <line>second</line>
        </invoice>
        """);

    [Fact]
    public void ANodeIsOfTheKindItIs()
    {
        Boolean("/invoice instance of element()").ShouldBeTrue();
        Boolean("/invoice instance of attribute()").ShouldBeFalse();
        Boolean("/invoice/@number instance of attribute()").ShouldBeTrue();
        Boolean("/invoice instance of node()").ShouldBeTrue();
        Boolean("/invoice instance of item()").ShouldBeTrue();
    }

    /// <summary>A name test narrows nothing here: <c>element(x)</c> asks the same question as <c>element()</c>.</summary>
    [Fact]
    public void AndTheNameTestIsAcceptedRatherThanRefused()
    {
        Boolean("/invoice instance of element(invoice)").ShouldBeTrue();
    }

    [Fact]
    public void TheOccurrenceIndicatorCountsWhatThereIs()
    {
        Boolean("/invoice/line instance of element()").ShouldBeFalse();
        Boolean("/invoice/line instance of element()*").ShouldBeTrue();
        Boolean("/invoice/line instance of element()+").ShouldBeTrue();
        Boolean("/invoice/line instance of element()?").ShouldBeFalse();
        Boolean("/invoice/missing instance of element()*").ShouldBeTrue();
        Boolean("/invoice/missing instance of element()+").ShouldBeFalse();
    }

    /// <summary>The shape the tax data rules use it in: walking the ancestors, counting the elements.</summary>
    [Fact]
    public void TheExpressionThePublishedRulesUse()
    {
        Number(
            "sum(for $ancestor in /invoice/line/ancestor-or-self::node() "
            + "return if ($ancestor instance of element()) then 1 else 0)")
            .ShouldBe(2m);
    }

    private static decimal? Number(string expression)
    {
        var evaluator = new XPathEvaluator(new Dictionary<string, string>(StringComparer.Ordinal));

        return evaluator.Evaluate(
            XPathParser.Parse(expression),
            new XPathContext(Document, Document, new Dictionary<string, XPathValue>(StringComparer.Ordinal)))
            .AsNumber();
    }

    private static bool Boolean(string expression)
    {
        var evaluator = new XPathEvaluator(new Dictionary<string, string>(StringComparer.Ordinal));

        return evaluator.Evaluate(
            XPathParser.Parse(expression),
            new XPathContext(Document, Document, new Dictionary<string, XPathValue>(StringComparer.Ordinal)))
            .AsBoolean();
    }
}
