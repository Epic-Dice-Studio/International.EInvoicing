using System.Globalization;
using International.EInvoicing.Countries.Iceland.Identifiers;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Countries.Iceland.Tests;

/// <summary>
/// The kennitala and its modulo 11 check.
/// </summary>
/// <remarks>
/// The valid examples are computed from the formula rather than copied from anywhere: a kennitala identifies
/// a real person or company, and inventing one that happens to exist is not a thing to do in a test fixture.
/// </remarks>
public class IsKennitalaTests
{
    internal static IReadOnlyList<string> ValidNumbers { get; } = [.. Enumerable
        .Range(0, 40)
        .Select(offset => WithCheckDigit(120_000_00 + (offset * 111)))
        .OfType<string>()
        .Take(8)];

    public static TheoryData<string> Valid => [.. ValidNumbers];

    [Theory]
    [MemberData(nameof(Valid))]
    public void ANumberSatisfyingTheFormulaIsAccepted(string number)
    {
        IsKennitala.IsValid(number).ShouldBeTrue(number);
        IsKennitala.Parse(number).Value.ShouldBe(number);
    }

    [Fact]
    public void ChangingTheCheckDigitBreaksIt()
    {
        string valid = ValidNumbers[0];
        char check = valid[8];

        IsKennitala.IsValid(valid[..8] + (check == '9' ? '0' : (char)(check + 1)) + valid[9]).ShouldBeFalse();
    }

    [Theory]
    [InlineData("120000000")]        // nine digits
    [InlineData("12000000000")]      // eleven
    [InlineData("12000000A0")]
    [InlineData("")]
    [InlineData(null)]
    public void AnythingElseIsRefused(string? value) => IsKennitala.IsValid(value).ShouldBeFalse();

    [Fact]
    public void ItIsReadHoweverItIsWritten()
    {
        string number = ValidNumbers[0];

        IsKennitala.Parse($"{number[..6]}-{number[6..]}").Value.ShouldBe(number);
        IsKennitala.Parse("IS" + number).Value.ShouldBe(number);
        IsKennitala.Parse(number).ToFormattedString().ShouldBe($"{number[..6]}-{number[6..]}");
    }

    [Fact]
    public void ItIsWrittenInTheSchemeTheIcelandicRulesLookIn()
    {
        IsKennitala.Scheme.ShouldBe("0196");
        IsKennitala.Parse(ValidNumbers[0]).ToField().SchemeId.ShouldBe("0196");
    }

    /// <summary>Eight digits, their modulo 11 check digit, and a century marker.</summary>
    private static string? WithCheckDigit(int eightDigits)
    {
        string body = eightDigits.ToString("D8", CultureInfo.InvariantCulture);
        int[] weights = [3, 2, 7, 6, 5, 4, 3, 2];
        int sum = 0;

        for (int index = 0; index < 8; index++)
        {
            sum += (body[index] - '0') * weights[index];
        }

        int remainder = sum % 11;
        int check = remainder == 0 ? 0 : 11 - remainder;

        return check == 10 ? null : body + check.ToString(CultureInfo.InvariantCulture) + "0";
    }
}
