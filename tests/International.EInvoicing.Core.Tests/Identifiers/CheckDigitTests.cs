using International.EInvoicing.Identifiers;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Core.Tests.Identifiers;

public class CheckDigitTests
{
    [Theory]
    [InlineData("732829320", true)]        // a SIREN
    [InlineData("73282932000074", true)]   // the matching SIRET
    [InlineData("732829321", false)]
    [InlineData("73282932000075", false)]
    [InlineData("", false)]
    [InlineData("12A456789", false)]
    public void LuhnAcceptsWhatItShould(string digits, bool expected) =>
        CheckDigit.SatisfiesLuhn(digits).ShouldBe(expected);

    [Theory]
    [InlineData("04174971", 91)]
    [InlineData("97", 0)]
    [InlineData("98", 1)]
    public void Modulo97IsComputedDigitByDigit(string digits, int expected) =>
        CheckDigit.Modulo97(digits).ShouldBe(expected);

    [Fact]
    public void Modulo97HandlesIdentifiersTooLongForAnInteger() =>
        CheckDigit.Modulo97("123456789012345678901234567890").ShouldNotBeNull();

    [Fact]
    public void Modulo97RejectsAnythingThatIsNotDigits() =>
        CheckDigit.Modulo97("0417497X").ShouldBeNull();

    /// <summary>The worked example from the Leitweg-ID format specification: 04011000-1234512345-06.</summary>
    [Fact]
    public void Iso7064MatchesThePublishedExample() =>
        CheckDigit.Iso7064Mod97("040110001234512345").ShouldBe("06");

    [Fact]
    public void Iso7064TreatsLettersAsTheirPositionPlusNine()
    {
        CheckDigit.Iso7064Mod97("A").ShouldNotBeNull();
        CheckDigit.Iso7064Mod97("04011000ABC").ShouldNotBeNull();
        CheckDigit.Iso7064Mod97("040-11000").ShouldBeNull();
    }

    [Fact]
    public void CompactKeepsOnlyLettersAndDigits() =>
        CheckDigit.Compact("0417.497.106").ShouldBe("0417497106");
}
