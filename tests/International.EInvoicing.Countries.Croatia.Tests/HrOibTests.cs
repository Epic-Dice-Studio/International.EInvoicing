using System.Globalization;
using International.EInvoicing.Countries.Croatia.Identifiers;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Countries.Croatia.Tests;

/// <summary>
/// The OIB and the ISO/IEC 7064 MOD 11,10 check that defines it.
/// </summary>
/// <remarks>
/// The valid examples are computed from the standard rather than copied from a register: an OIB identifies a
/// real person or company, and a demo has no business carrying one.
/// </remarks>
public class HrOibTests
{
    internal static IReadOnlyList<string> ValidNumbers { get; } = [.. Enumerable
        .Range(0, 8)
        .Select(offset => WithCheckDigit(1_234_567_800L + offset))];

    public static TheoryData<string> Valid => [.. ValidNumbers];

    [Theory]
    [MemberData(nameof(Valid))]
    public void ANumberSatisfyingTheStandardIsAccepted(string oib)
    {
        HrOib.IsValid(oib).ShouldBeTrue(oib);
        HrOib.Parse(oib).Value.ShouldBe(oib);
    }

    /// <summary>
    /// The published worked example: 69435151530 is the OIB the Croatian tax administration uses in its own
    /// documentation, and it is the one independent check available for a computed algorithm.
    /// </summary>
    [Fact]
    public void ThePublishedExampleIsAccepted() => HrOib.IsValid("69435151530").ShouldBeTrue();

    [Fact]
    public void ChangingOneDigitBreaksIt()
    {
        string valid = ValidNumbers[0];
        char last = valid[^1];

        HrOib.IsValid(valid[..^1] + (last == '9' ? '0' : (char)(last + 1))).ShouldBeFalse();
    }

    [Theory]
    [InlineData("1234567890")]       // ten digits
    [InlineData("123456789012")]     // twelve
    [InlineData("1234567890A")]
    [InlineData("")]
    [InlineData(null)]
    public void AnythingElseIsRefused(string? value) => HrOib.IsValid(value).ShouldBeFalse();

    [Fact]
    public void ItIsReadHoweverItIsWritten()
    {
        string oib = ValidNumbers[0];

        HrOib.Parse("HR" + oib).Value.ShouldBe(oib);
        HrOib.Parse(oib).VatNumber.ShouldBe("HR" + oib);
        HrOib.Parse(oib).ToField().SchemeId.ShouldBe(HrOib.Scheme);
    }

    /// <summary>The eleventh digit, under ISO/IEC 7064 MOD 11,10.</summary>
    private static string WithCheckDigit(long tenDigits)
    {
        string body = tenDigits.ToString("D10", CultureInfo.InvariantCulture);
        int remainder = 10;

        foreach (char digit in body)
        {
            remainder = (remainder + (digit - '0')) % 10;
            remainder = (remainder == 0 ? 10 : remainder) * 2 % 11;
        }

        return body + ((11 - remainder) % 10).ToString(CultureInfo.InvariantCulture);
    }
}
