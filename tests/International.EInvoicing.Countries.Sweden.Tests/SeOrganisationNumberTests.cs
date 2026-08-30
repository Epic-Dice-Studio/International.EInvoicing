using System.Globalization;
using International.EInvoicing.Countries.Sweden.Identifiers;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Countries.Sweden.Tests;

/// <summary>
/// The organisation number, and the Luhn check Peppol enforces on it.
/// </summary>
/// <remarks>
/// The valid examples are computed from the formula rather than copied from a register, and the tests beside
/// this one hand them to Peppol's own rule for a second opinion.
/// </remarks>
public class SeOrganisationNumberTests
{
    internal static IReadOnlyList<string> ValidNumbers { get; } = [.. Enumerable
        .Range(0, 8)
        .Select(offset => WithCheckDigit(556_000_000 + (offset * 1_111)))];

    public static TheoryData<string> Valid => [.. ValidNumbers];

    [Theory]
    [MemberData(nameof(Valid))]
    public void ANumberSatisfyingTheFormulaIsAccepted(string number)
    {
        SeOrganisationNumber.IsValid(number).ShouldBeTrue(number);
        SeOrganisationNumber.Parse(number).Value.ShouldBe(number);
    }

    [Fact]
    public void ChangingOneDigitBreaksIt()
    {
        string valid = ValidNumbers[0];
        char last = valid[^1];

        SeOrganisationNumber.IsValid(valid[..^1] + (last == '9' ? '0' : (char)(last + 1))).ShouldBeFalse();
    }

    [Theory]
    [InlineData("556000000")]        // nine digits
    [InlineData("55600000000")]      // eleven
    [InlineData("55600000A0")]
    [InlineData("")]
    [InlineData(null)]
    public void AnythingElseIsRefused(string? value) => SeOrganisationNumber.IsValid(value).ShouldBeFalse();

    [Fact]
    public void ItIsReadHoweverItIsWritten()
    {
        string number = ValidNumbers[0];

        SeOrganisationNumber.Parse($"{number[..6]}-{number[6..]}").Value.ShouldBe(number);
        SeOrganisationNumber.Parse("SE" + number + "01").Value.ShouldBe(number);
        SeOrganisationNumber.Parse(number).ToFormattedString().ShouldBe($"{number[..6]}-{number[6..]}");
        SeOrganisationNumber.Parse(number).VatNumber.ShouldBe("SE" + number + "01");
    }

    [Fact]
    public void ItIsWrittenInTheSchemePeppolReservesForIt()
    {
        SeOrganisationNumber.Scheme.ShouldBe("0007");
        SeOrganisationNumber.Parse(ValidNumbers[0]).ToField().SchemeId.ShouldBe("0007");
    }

    /// <summary>The Luhn check digit for nine leading digits.</summary>
    private static string WithCheckDigit(int nineDigits)
    {
        string body = nineDigits.ToString("D9", CultureInfo.InvariantCulture);
        int sum = 0;
        bool doubling = true;

        for (int index = body.Length - 1; index >= 0; index--)
        {
            int value = body[index] - '0';

            if (doubling)
            {
                value *= 2;
                if (value > 9)
                {
                    value -= 9;
                }
            }

            sum += value;
            doubling = !doubling;
        }

        return body + ((10 - (sum % 10)) % 10).ToString(CultureInfo.InvariantCulture);
    }
}
