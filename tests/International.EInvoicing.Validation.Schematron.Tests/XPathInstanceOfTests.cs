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

    /// <summary>
    /// The axes that include the node itself, which were reading as though they did not.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>ancestor-or-self</c> and <c>descendant-or-self</c> were both mapped onto their plain forms, so the
    /// node itself was dropped from every answer. Nothing in the rule sets running today asks the question in
    /// an assertion — the compiled EN 16931 artefacts use it to build the path of a failure, and the tax data
    /// rules to count the elements above a node — but an engine that answers a different question from the
    /// one asked is a defect whether or not anything has noticed.
    /// </para>
    /// <para>
    /// The counts below also pin two things this engine had never said out loud. A path yields a
    /// <b>node-set</b>: two lines share one parent, so walking up from both reaches that parent once, not
    /// twice — it was twice. And <c>node()</c> here selects named nodes, so the document node above the root
    /// element is not among them; every artefact this library runs uses <c>node()</c> to walk elements, and
    /// widening it would change what <c>//node()</c> means in every rule set at once.
    /// </para>
    /// </remarks>
    [Fact]
    public void TheAxesThatIncludeTheNodeItselfDo()
    {
        Number("count(/invoice/line[1]/ancestor-or-self::node())").ShouldBe(2m);
        Number("count(/invoice/line[1]/ancestor::node())").ShouldBe(1m);
        Number("count(/invoice/line/ancestor-or-self::node())").ShouldBe(3m);
        Number("count(/invoice/descendant-or-self::node())").ShouldBe(3m);
        Number("count(/invoice/descendant::node())").ShouldBe(2m);
        Number("count(/invoice/line[1]/ancestor-or-self::line)").ShouldBe(1m);
        Number("count(/invoice/line[1]/ancestor-or-self::invoice)").ShouldBe(1m);
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

    /// <summary>
    /// The shape the tax data rules use it in: walking the ancestors, counting the elements among them.
    /// </summary>
    /// <remarks>
    /// Three: both lines, and the invoice above them. The document node is not an element, and the two lines
    /// share one ancestor — <c>ancestor-or-self</c> over a sequence is one node-set, not two concatenated.
    /// </remarks>
    [Fact]
    public void TheExpressionThePublishedRulesUse()
    {
        Number(
            "sum(for $ancestor in /invoice/line/ancestor-or-self::node() "
            + "return if ($ancestor instance of element()) then 1 else 0)")
            .ShouldBe(3m);
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
