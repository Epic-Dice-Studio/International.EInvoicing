using International.EInvoicing.Countries.Denmark.Identifiers;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Countries.Denmark.Tests;

/// <summary>
/// The CVR number, and the check Peppol enforces on it.
/// </summary>
/// <remarks>
/// Peppol checks the shape and not the modulo 11 check digit a CVR number also carries, and this follows it
/// deliberately: rejecting a number the receiving access point would have accepted is a worse failure than
/// letting a typo through. The test beside this one holds both sides to Peppol's own rule.
/// </remarks>
public class DkCvrNumberTests
{
    internal static IReadOnlyList<string> ValidNumbers { get; } =
        ["12345670", "10150817", "25313763", "33257872", "29189901", "37407345", "13585628", "26911745"];

    public static TheoryData<string> Valid => [.. ValidNumbers];

    [Theory]
    [MemberData(nameof(Valid))]
    public void EightDigitsAreAccepted(string number)
    {
        DkCvrNumber.IsValid(number).ShouldBeTrue(number);
        DkCvrNumber.Parse(number).Value.ShouldBe(number);
    }

    [Theory]
    [InlineData("1234567")]          // seven digits
    [InlineData("123456789")]        // nine
    [InlineData("1234567A")]
    [InlineData("")]
    [InlineData(null)]
    public void AnythingElseIsRefused(string? value) => DkCvrNumber.IsValid(value).ShouldBeFalse();

    [Fact]
    public void ItIsReadHoweverItIsWritten()
    {
        DkCvrNumber.Parse("DK12345670").Value.ShouldBe("12345670");
        DkCvrNumber.Parse("12 34 56 70").Value.ShouldBe("12345670");
        DkCvrNumber.Parse("12345670").VatNumber.ShouldBe("DK12345670");
    }

    [Fact]
    public void ItIsWrittenInTheSchemesPeppolReservesForIt()
    {
        DkCvrNumber.Scheme.ShouldBe("0184");
        DkCvrNumber.SeNumberScheme.ShouldBe("0198");

        DkCvrNumber number = DkCvrNumber.Parse("12345670");

        number.ToField().SchemeId.ShouldBe("0184");
        number.ToSeNumberField().Value.ShouldBe("DK12345670");
        number.ToSeNumberField().SchemeId.ShouldBe("0198");
    }
}
