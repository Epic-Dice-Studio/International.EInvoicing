using System.Globalization;
using International.EInvoicing.Countries.NewZealand.Identifiers;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Countries.NewZealand.Tests;

/// <summary>
/// The NZBN, and the GS1 check digit that defines it.
/// </summary>
/// <remarks>
/// An NZBN is a GS1 Global Location Number, so the check is the GS1 one — and that gives an independent
/// reference the other identifiers in this library do not have: any published GLN must satisfy it too.
/// </remarks>
public class NzNzbnTests
{
    internal static IReadOnlyList<string> ValidNumbers { get; } = [.. Enumerable
        .Range(0, 8)
        .Select(offset => WithCheckDigit(942_904_000_000L + (offset * 137)))];

    public static TheoryData<string> Valid => [.. ValidNumbers];

    [Theory]
    [MemberData(nameof(Valid))]
    public void ANumberSatisfyingTheFormulaIsAccepted(string nzbn)
    {
        NzNzbn.IsValid(nzbn).ShouldBeTrue(nzbn);
        NzNzbn.Parse(nzbn).Value.ShouldBe(nzbn);
    }

    /// <summary>
    /// The independent check is not here but next door: <c>NewZealandEInvoicingTests</c> hands every number
    /// this generates, and a set it refuses, to Peppol's own GLN rule. An algorithm tested only against
    /// numbers it generated itself proves nothing.
    /// </summary>
    [Fact]
    public void ChangingOneDigitBreaksIt()
    {
        string valid = ValidNumbers[0];
        char last = valid[^1];

        NzNzbn.IsValid(valid[..^1] + (last == '9' ? '0' : (char)(last + 1))).ShouldBeFalse();
    }

    [Theory]
    [InlineData("942904000000")]     // twelve digits
    [InlineData("94290400000000")]   // fourteen
    [InlineData("94290400000A0")]
    [InlineData("")]
    [InlineData(null)]
    public void AnythingElseIsRefused(string? value) => NzNzbn.IsValid(value).ShouldBeFalse();

    [Fact]
    public void ItIsWrittenInTheGlnSchemePeppolRoutesItBy()
    {
        NzNzbn.Scheme.ShouldBe("0088");
        NzNzbn.Parse(ValidNumbers[0]).ToField().SchemeId.ShouldBe("0088");
    }

    /// <summary>The thirteenth digit, under the GS1 rule: weights alternating 3 and 1 from the right.</summary>
    private static string WithCheckDigit(long twelveDigits)
    {
        string body = twelveDigits.ToString("D12", CultureInfo.InvariantCulture);
        int sum = 0;

        for (int index = 0; index < 12; index++)
        {
            // GS1 weights the even positions by three, counting from one at the left.
            sum += (body[index] - '0') * ((index % 2 == 1) ? 3 : 1);
        }

        return body + ((10 - (sum % 10)) % 10).ToString(CultureInfo.InvariantCulture);
    }
}
