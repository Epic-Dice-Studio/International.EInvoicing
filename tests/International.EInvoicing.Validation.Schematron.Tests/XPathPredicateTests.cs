using System.Xml.Linq;
using International.EInvoicing.Validation.Schematron.XPath;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Validation.Schematron.Tests;

/// <summary>
/// Where a predicate applies, which is two different things wearing the same brackets.
/// </summary>
/// <remarks>
/// <para>
/// In <c>a/b[1]</c> the predicate belongs to the step: it is the first <c>b</c> of <em>each</em> <c>a</c>.
/// In <c>$digits[1]</c> it filters a sequence: one item out of the whole thing. An engine that treats both
/// the same way is wrong on one of them, and both appear in the artefacts this library runs — EN 16931 sums
/// <c>ActualAmount[1]</c> over every document-level allowance, and Peppol's Norwegian check digit indexes
/// into a sequence of digits.
/// </para>
/// <para>
/// This was a real defect: the whole result of a step was filtered as one list, so BR-CO-11 saw the first
/// allowance and ignored the rest, and an invoice with two of them was rejected for arithmetic it had got
/// right.
/// </para>
/// </remarks>
public class XPathPredicateTests
{
    private static readonly XDocument Document = XDocument.Parse(
        """
        <invoice>
          <allowance><amount>10</amount><amount>99</amount></allowance>
          <allowance><amount>25</amount><amount>99</amount></allowance>
        </invoice>
        """);

    [Fact]
    public void AStepPredicateAppliesToEachNodeTheStepStartedFrom()
    {
        Number("sum(/invoice/allowance/amount[1])").ShouldBe(35m);
    }

    [Fact]
    public void AndTheStepWithoutOneStillSeesThemAll()
    {
        Number("sum(/invoice/allowance/amount)").ShouldBe(233m);
    }

    /// <summary>The <c>//</c> shorthand is a step too, so its predicate is per node as well.</summary>
    [Fact]
    public void SoDoesThePredicateAfterADescendantShorthand()
    {
        Number("sum(//allowance/amount[1])").ShouldBe(35m);
        Number("count(//amount[2])").ShouldBe(2m);
    }

    /// <summary>A predicate after something that is not a step filters the sequence as a whole.</summary>
    [Fact]
    public void APredicateOnASequenceFiltersTheSequence()
    {
        Text("tokenize('urn:fdc:peppol.eu:2017:poacc:billing:01:1.0', ':')[7]").ShouldBe("01");
        Number("count((1, 2, 3)[2])").ShouldBe(1m);
    }

    [Fact]
    public void IncludingWhenItComesFromAVariable()
    {
        var digits = XPathValue.Nodes([1m, 2m, 3m, 4m]);

        Number("sum(for $i in (0 to 3) return $digits[$i + 1] * 2)", ("digits", digits)).ShouldBe(20m);
    }

    private static decimal? Number(string expression, params (string Name, XPathValue Value)[] variables) =>
        Evaluate(expression, variables).AsNumber();

    private static string Text(string expression, params (string Name, XPathValue Value)[] variables) =>
        Evaluate(expression, variables).AsText();

    private static XPathValue Evaluate(string expression, (string Name, XPathValue Value)[] variables)
    {
        var scope = new Dictionary<string, XPathValue>(StringComparer.Ordinal);

        foreach ((string name, XPathValue value) in variables)
        {
            scope[name] = value;
        }

        var evaluator = new XPathEvaluator(new Dictionary<string, string>(StringComparer.Ordinal));

        return evaluator.Evaluate(
            XPathParser.Parse(expression),
            new XPathContext(Document, Document, scope));
    }
}
