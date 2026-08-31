using System.Globalization;
using International.EInvoicing.Countries.Australia.Identifiers;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Countries.Australia.Tests;

/// <summary>
/// The ABN, and the rule Peppol checks it against.
/// </summary>
/// <remarks>
/// The valid examples are computed from the formula rather than copied from the business register — an ABN
/// belongs to a real company. The test beside this one hands them to Peppol's own rule for a second opinion.
/// </remarks>
public class AuAbnTests
{
    internal static IReadOnlyList<string> ValidNumbers { get; } = [.. Enumerable
        .Range(0, 400)
        .Select(offset => WithValidTail(51_824_753_000L + offset))
        .OfType<string>()
        .Take(8)];

    public static TheoryData<string> Valid => [.. ValidNumbers];

    [Theory]
    [MemberData(nameof(Valid))]
    public void ANumberSatisfyingTheFormulaIsAccepted(string abn)
    {
        AuAbn.IsValid(abn).ShouldBeTrue(abn);
        AuAbn.Parse(abn).Value.ShouldBe(abn);
    }

    /// <summary>
    /// Every digit is weighted, so a transposition anywhere is caught — not only a wrong last digit.
    /// </summary>
    [Fact]
    public void TransposingTwoDigitsBreaksIt()
    {
        string valid = ValidNumbers[0];
        int at = valid.Zip(valid.Skip(1)).Select((pair, index) => (pair, index))
            .First(x => x.pair.First != x.pair.Second).index;

        string swapped = valid[..at] + valid[at + 1] + valid[at] + valid[(at + 2)..];

        swapped.ShouldNotBe(valid);
        AuAbn.IsValid(swapped).ShouldBeFalse(swapped);
    }

    /// <summary>
    /// The one example that is not self-referential: 51 824 753 556 is the ABN the Australian Taxation
    /// Office publishes in its own developer documentation. An algorithm checked only against numbers it
    /// generated itself proves nothing.
    /// </summary>
    [Fact]
    public void ThePublishedExampleIsAccepted()
    {
        AuAbn.IsValid("51824753556").ShouldBeTrue();
        AuAbn.IsValid("51 824 753 556").ShouldBeTrue();
        AuAbn.IsValid("51824753557").ShouldBeFalse();
    }

    [Theory]
    [InlineData("5182475300")]       // ten digits
    [InlineData("518247530001")]     // twelve
    [InlineData("5182475300A")]
    [InlineData("")]
    [InlineData(null)]
    public void AnythingElseIsRefused(string? value) => AuAbn.IsValid(value).ShouldBeFalse();

    [Fact]
    public void ItIsReadHoweverItIsWritten()
    {
        string abn = ValidNumbers[0];
        string spaced = $"{abn[..2]} {abn[2..5]} {abn[5..8]} {abn[8..]}";

        AuAbn.Parse(spaced).Value.ShouldBe(abn);
        AuAbn.Parse(abn).ToFormattedString().ShouldBe(spaced);
    }

    [Fact]
    public void ItIsWrittenInTheSchemePeppolReservesForIt()
    {
        AuAbn.Scheme.ShouldBe("0151");
        AuAbn.Parse(ValidNumbers[0]).ToField().SchemeId.ShouldBe("0151");
    }

    /// <summary>The eleven digits whose weighted sum, less one on the first, divides by 89.</summary>
    private static string? WithValidTail(long candidate)
    {
        string digits = candidate.ToString("D11", CultureInfo.InvariantCulture);

        if (digits.Length != 11 || digits[0] == '0')
        {
            return null;
        }

        int[] weights = [10, 1, 3, 5, 7, 9, 11, 13, 15, 17, 19];
        int sum = (digits[0] - '0' - 1) * weights[0];

        for (int index = 1; index < 11; index++)
        {
            sum += (digits[index] - '0') * weights[index];
        }

        return sum % 89 == 0 ? digits : null;
    }
}
