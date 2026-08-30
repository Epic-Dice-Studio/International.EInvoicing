using International.EInvoicing.Countries.Germany.Identifiers;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Countries.Germany.Tests;

/// <summary>
/// The examples come from the Leitweg-ID format specification and from the official XRechnung test suite,
/// not from numbers invented here.
/// </summary>
public class DeLeitwegIdTests
{
    [Fact]
    public void ThePublishedExampleIsAccepted()
    {
        DeLeitwegId route = DeLeitwegId.Parse("04011000-1234512345-06");

        route.CoarseAddress.ShouldBe("04011000");
        route.FineAddress.ShouldBe("1234512345");
        route.CheckDigits.ShouldBe("06");
    }

    [Fact]
    public void TheOneUsedThroughoutTheOfficialTestSuiteIsAccepted() =>
        DeLeitwegId.IsValid("04011000-12345-03").ShouldBeTrue();

    [Fact]
    public void AWrongCheckDigitIsRefused()
    {
        DeLeitwegId.IsValid("04011000-1234512345-07").ShouldBeFalse();
        DeLeitwegId.IsValid("04011000-12345-04").ShouldBeFalse();
    }

    [Fact]
    public void ARoutingIdentifierWithNoFineAddressIsAllowed()
    {
        DeLeitwegId route = DeLeitwegId.Create("04011000");

        route.FineAddress.ShouldBeEmpty();
        route.ToString().ShouldBe("04011000-" + route.CheckDigits);
        DeLeitwegId.IsValid(route.ToString()).ShouldBeTrue();
    }

    [Fact]
    public void BuildingOneComputesItsCheckDigits()
    {
        DeLeitwegId route = DeLeitwegId.Create("04011000", "1234512345");

        route.ToString().ShouldBe("04011000-1234512345-06");
    }

    [Fact]
    public void LettersAreAllowedAndCountedAsTheSpecificationSays()
    {
        DeLeitwegId route = DeLeitwegId.Create("04011000", "ABC123");

        DeLeitwegId.IsValid(route.ToString()).ShouldBeTrue();
        DeLeitwegId.Parse(route.ToString()).FineAddress.ShouldBe("ABC123");
    }

    [Theory]
    [InlineData("0")]                                   // coarse too short
    [InlineData("0401100004011000-12345-03")]           // coarse too long
    [InlineData("04011000-12345")]                      // no check digits at all
    [InlineData("04011000-12345-XX")]                   // check digits are digits
    [InlineData("0401_1000-12345-03")]                  // not alphanumeric
    [InlineData("")]
    [InlineData(null)]
    public void SomethingMisshapenIsRefused(string? written) =>
        DeLeitwegId.IsValid(written).ShouldBeFalse();

    [Fact]
    public void ItGoesIntoTheBuyerReference() =>
        DeLeitwegId.Parse("04011000-12345-03").ToBuyerReference().Value.ShouldBe("04011000-12345-03");
}
