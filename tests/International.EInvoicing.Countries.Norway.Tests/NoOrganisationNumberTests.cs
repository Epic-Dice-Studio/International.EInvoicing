using International.EInvoicing.Countries.Norway.Identifiers;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Countries.Norway.Tests;

/// <summary>
/// The organisation number, and the rule Peppol checks it against.
/// </summary>
/// <remarks>
/// The valid examples here are computed from the modulo 11 formula rather than copied from a business
/// register, and the tests beside this one hand them to Peppol's own rule for a second opinion. A fixture
/// somebody invented is the one thing in a test suite nobody checks — this project has already shipped one
/// invalid Belgian number that way.
/// </remarks>
public class NoOrganisationNumberTests
{
    /// <summary>Numbers built to satisfy the formula, computed rather than copied from anywhere.</summary>
    internal static IReadOnlyList<string> ValidNumbers { get; } = [.. Enumerable
        .Range(0, 40)
        .Select(offset => WithCheckDigit(91_000_000 + (offset * 1_111)))
        .OfType<string>()
        .Take(8)];

    public static TheoryData<string> Valid => [.. ValidNumbers];

    [Theory]
    [MemberData(nameof(Valid))]
    public void ANumberSatisfyingTheFormulaIsAccepted(string number)
    {
        NoOrganisationNumber.IsValid(number).ShouldBeTrue(number);
        NoOrganisationNumber.Parse(number).Value.ShouldBe(number);
    }

    [Fact]
    public void ChangingOneDigitBreaksIt()
    {
        string valid = NoOrganisationNumber.Parse(ValidNumbers[0]).Value;
        char last = valid[^1];
        string broken = valid[..^1] + (last == '9' ? '0' : (char)(last + 1));

        NoOrganisationNumber.IsValid(broken).ShouldBeFalse();
    }

    [Theory]
    [InlineData("91234567")]         // eight digits
    [InlineData("9123456789")]       // ten
    [InlineData("91234567A")]        // not digits
    [InlineData("")]
    [InlineData(null)]
    public void AnythingElseIsRefused(string? value) => NoOrganisationNumber.IsValid(value).ShouldBeFalse();

    [Fact]
    public void ItIsReadHoweverItIsWritten()
    {
        string number = ValidNumbers[0];
        string spaced = $"{number[..3]} {number[3..6]} {number[6..]}";

        NoOrganisationNumber.Parse(spaced).Value.ShouldBe(number);
        NoOrganisationNumber.Parse("NO" + number + "MVA").Value.ShouldBe(number);
        NoOrganisationNumber.Parse(number).ToFormattedString().ShouldBe(spaced);
        NoOrganisationNumber.Parse(number).VatNumber.ShouldBe("NO" + number + "MVA");
    }

    [Fact]
    public void ItIsWrittenInTheSchemePeppolReservesForIt()
    {
        NoOrganisationNumber.Scheme.ShouldBe("0192");
        NoOrganisationNumber.Parse(ValidNumbers[0]).ToField().SchemeId.ShouldBe("0192");
    }

    /// <summary>The check digit for eight leading digits, or <c>null</c> when the formula leaves none.</summary>
    internal static string? WithCheckDigit(int eightDigits)
    {
        string body = eightDigits.ToString("D8", System.Globalization.CultureInfo.InvariantCulture);
        int[] weights = [3, 2, 7, 6, 5, 4, 3, 2];
        int sum = 0;

        for (int index = 0; index < 8; index++)
        {
            sum += (body[index] - '0') * weights[index];
        }

        int remainder = sum % 11;
        int check = remainder == 0 ? 0 : 11 - remainder;

        return check == 10 ? null : body + check.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
