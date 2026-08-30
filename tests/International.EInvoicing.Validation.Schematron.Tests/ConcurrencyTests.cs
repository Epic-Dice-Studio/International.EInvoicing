using International.EInvoicing.Validation;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Validation.Schematron.Tests;

/// <summary>
/// A rule set is loaded once and used by everything that validates afterwards, which in a server means from
/// many threads at once.
/// </summary>
/// <remarks>
/// Concurrent use is a recurring report against Schematron engines, and the failure mode is the bad kind:
/// not a crash but a wrong answer, because a rule's variables leaked between validations. Nothing here is
/// mutated after loading, and each validation gets its own evaluation state — this is what pins that.
/// </remarks>
public class ConcurrencyTests
{
    private const string RuleSet = """
        <schema xmlns="http://purl.oclc.org/dsdl/schematron">
          <ns prefix="i" uri="urn:example:invoice"/>
          <pattern>
            <rule context="i:Invoice">
              <let name="total" value="number(i:Total)"/>
              <let name="lines" value="sum(i:Line/number(.))"/>
              <assert id="TOTALS" test="$total = $lines">The total must be the sum of the lines.</assert>
            </rule>
          </pattern>
        </schema>
        """;

    [Fact]
    public void TheSameRuleSetGivesTheSameAnswerFromManyThreadsAtOnce()
    {
        SchematronRuleSet rules = SchematronRuleSet.Load(RuleSet, "totals", "1");
        string balanced = Invoice(100, [60, 40]);
        string unbalanced = Invoice(100, [60, 30]);

        bool[] results = new bool[400];

        Parallel.For(0, results.Length, index =>
        {
            string document = index % 2 == 0 ? balanced : unbalanced;
            results[index] = new SchematronValidator().Validate(document, rules).IsValid;
        });

        for (int index = 0; index < results.Length; index++)
        {
            results[index].ShouldBe(index % 2 == 0, $"iteration {index} disagreed with itself");
        }
    }

    private static string Invoice(int total, int[] lines) =>
        $"""
        <i:Invoice xmlns:i="urn:example:invoice">
          <i:Total>{total}</i:Total>
          {string.Concat(lines.Select(line => $"<i:Line>{line}</i:Line>"))}
        </i:Invoice>
        """;
}
