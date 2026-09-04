using System.Xml.Linq;
using International.EInvoicing.Validation.Schematron.XPath;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Validation.Schematron.Tests;

/// <summary>
/// An element whose only content is a comment is an empty element.
/// </summary>
/// <remarks>
/// <para>
/// XPath builds an element's string-value from its text descendants. Comments are not text, so
/// <c>&lt;date&gt;&lt;!--nothing--&gt;&lt;/date&gt;</c> has a string-value of <c>""</c> and a length of 0 —
/// and rules written as <c>string-length(…) &gt; 1</c> are how EN 16931 asks "did they actually fill this
/// in?".
/// </para>
/// <para>
/// This was a real defect, and one only a negative corpus could show: an element holding nothing but a
/// comment counted as filled in, so BR-IC-11 and BR-IC-12 passed a document that states an intra-community
/// supply with no delivery date. The standard's own unit case for it is exactly that document.
/// </para>
/// </remarks>
public class CommentOnlyElementTests
{
    private static readonly XDocument Document =
        XDocument.Parse("<i><a><!--comment only--></a><b/><c>x</c><d>y<!--and a comment--></d></i>");

    [Fact]
    public void AnElementHoldingOnlyACommentHasNoStringValue()
    {
        Number("string-length(/i/a)").ShouldBe(0m);
        Text("string(/i/a)").ShouldBe(string.Empty);
        Text("normalize-space(/i/a)").ShouldBe(string.Empty);
    }

    [Fact]
    public void AndReadsTheSameAsAnEmptyOne()
    {
        Number("string-length(/i/b)").ShouldBe(Number("string-length(/i/a)"));
    }

    [Fact]
    public void WhileAnElementWithTextStillHasIt()
    {
        Number("string-length(/i/c)").ShouldBe(1m);
        Text("string(/i/c)").ShouldBe("x");
    }

    /// <summary>And a comment beside real text does not add to it.</summary>
    [Fact]
    public void AndACommentBesideTextAddsNothingToIt()
    {
        Text("string(/i/d)").ShouldBe("y");
        Number("string-length(/i/d)").ShouldBe(1m);
    }

    private static decimal? Number(string expression) => Evaluate(expression).AsNumber();

    private static string Text(string expression) => Evaluate(expression).AsText();

    private static XPathValue Evaluate(string expression)
    {
        var evaluator = new XPathEvaluator(new Dictionary<string, string>(StringComparer.Ordinal));

        return evaluator.Evaluate(
            XPathParser.Parse(expression),
            new XPathContext(Document, Document, new Dictionary<string, XPathValue>(StringComparer.Ordinal)));
    }
}
