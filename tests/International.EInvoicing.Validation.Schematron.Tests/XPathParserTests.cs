using International.EInvoicing.Validation.Schematron.XPath;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Validation.Schematron.Tests;

public class XPathParserTests
{
    [Fact]
    public void EveryExpressionInTheOfficialArtefactsParses()
    {
        var failures = new List<string>();

        foreach (string expression in Artefacts.AllExpressions)
        {
            try
            {
                XPathParser.Parse(expression);
            }
            catch (XPathException exception)
            {
                failures.Add($"{exception.Message}  <<{Shorten(expression)}>>");
            }
        }

        failures.ShouldBeEmpty(
            $"{failures.Count} of {Artefacts.AllExpressions.Count} expressions did not parse:"
            + Environment.NewLine
            + string.Join(Environment.NewLine, failures.Take(10)));
    }

    [Fact]
    public void TheArtefactsAreActuallyThere()
    {
        Artefacts.AllExpressions.Count.ShouldBeGreaterThan(1500);
    }

    private static string Shorten(string expression) =>
        expression.Length <= 120 ? expression : expression[..120] + "…";
}
