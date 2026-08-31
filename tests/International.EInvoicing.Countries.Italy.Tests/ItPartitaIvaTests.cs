using System.Globalization;
using International.EInvoicing.Countries.Italy.Identifiers;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Countries.Italy.Tests;

/// <summary>
/// The partita IVA, and the rule Peppol checks it against.
/// </summary>
/// <remarks>
/// The valid examples are computed from the formula rather than copied from the business register — an partita IVA
/// belongs to a real company. The test beside this one hands them to Peppol's own rule for a second opinion.
/// </remarks>
public class ItPartitaIvaTests
{
    internal static IReadOnlyList<string> ValidNumbers { get; } = [.. Enumerable
        .Range(0, 4000)
        .Select(offset => WithValidTail(12_345_670_000L + offset))
        .OfType<string>()
        .Take(8)];

    public static TheoryData<string> Valid => [.. ValidNumbers];

    [Theory]
    [MemberData(nameof(Valid))]
    public void ANumberSatisfyingTheFormulaIsAccepted(string abn)
    {
        ItPartitaIva.IsValid(abn).ShouldBeTrue(abn);
        ItPartitaIva.Parse(abn).Value.ShouldBe(abn);
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
        ItPartitaIva.IsValid(swapped).ShouldBeFalse(swapped);
    }

    [Theory]
    [InlineData("1234567890")]       // ten digits
    [InlineData("123456789012")]     // twelve
    [InlineData("1234567890A")]
    [InlineData("")]
    [InlineData(null)]
    public void AnythingElseIsRefused(string? value) => ItPartitaIva.IsValid(value).ShouldBeFalse();

    [Fact]
    public void ItIsReadHoweverItIsWritten()
    {
        string partitaIva = ValidNumbers[0];

        ItPartitaIva.Parse("IT" + partitaIva).Value.ShouldBe(partitaIva);
        ItPartitaIva.Parse(partitaIva).VatNumber.ShouldBe("IT" + partitaIva);
    }

    [Fact]
    public void ItIsWrittenInTheSchemePeppolReservesForIt()
    {
        ItPartitaIva.Scheme.ShouldBe("0211");
        ItPartitaIva.Parse(ValidNumbers[0]).ToField().SchemeId.ShouldBe("0211");
    }

    /// <summary>Eleven digits whose alternating sum divides by ten, as the Italian rule computes it.</summary>
    private static string? WithValidTail(long candidate)
    {
        string digits = candidate.ToString("D11", CultureInfo.InvariantCulture);

        if (digits.Length != 11)
        {
            return null;
        }

        const string evenPositionMap = "0246813579";
        int sum = 0;

        for (int index = 0; index < 11; index++)
        {
            int digit = digits[index] - '0';
            sum += index % 2 == 0 ? digit : evenPositionMap[digit] - '0';
        }

        return sum % 10 == 0 ? digits : null;
    }
}
