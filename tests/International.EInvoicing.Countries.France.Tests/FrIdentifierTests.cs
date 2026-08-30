using International.EInvoicing.Countries.France.Identifiers;
using International.EInvoicing.Identifiers;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Countries.France.Tests;

public class FrIdentifierTests
{
    [Theory]
    [InlineData("732829320")]
    [InlineData("732 829 320")]
    [InlineData("732.829.320")]
    public void ASirenIsReadHoweverItIsWritten(string written)
    {
        FrSiren.TryParse(written, out FrSiren siren).ShouldBeTrue();
        siren.Value.ShouldBe("732829320");
        siren.ToFormattedString().ShouldBe("732 829 320");
    }

    [Theory]
    [InlineData("732829321")]   // check digit off by one
    [InlineData("73282932")]    // too short
    [InlineData("7328293200")]  // too long
    [InlineData("")]
    [InlineData(null)]
    public void SomethingThatIsNotASirenIsRefused(string? written) =>
        FrSiren.IsValid(written).ShouldBeFalse();

    [Fact]
    public void ASiretCarriesItsSirenAndItsEstablishment()
    {
        FrSiret siret = FrSiret.Parse("732 829 320 00074");

        siret.Value.ShouldBe("73282932000074");
        siret.Siren.Value.ShouldBe("732829320");
        siret.EstablishmentNumber.ShouldBe("00074");
        siret.ToFormattedString().ShouldBe("732 829 320 00074");
    }

    [Fact]
    public void ALaPosteSiretIsAcceptedThoughItFailsLuhn()
    {
        // Establishments under SIREN 356000000 predate the Luhn rule and satisfy a digit sum divisible by
        // five instead. A validator that does not know that rejects genuine invoices.
        const string laPoste = "35600000000001";

        CheckDigit.SatisfiesLuhn(laPoste).ShouldBeFalse();
        FrSiret.IsValid(laPoste).ShouldBeTrue();
    }

    [Fact]
    public void ALaPosteSiretBreakingItsOwnRuleIsStillRefused() =>
        FrSiret.IsValid("35600000000011").ShouldBeFalse();

    [Fact]
    public void AVatNumberIsCheckedAgainstItsSiren()
    {
        FrVatNumber number = FrVatNumber.Parse("FR44732829320");

        number.Siren.Value.ShouldBe("732829320");
        number.IsKeyVerified.ShouldBeTrue();
    }

    [Fact]
    public void AVatNumberIsBuiltFromASiren()
    {
        FrVatNumber number = FrVatNumber.ForSiren(FrSiren.Parse("732829320"));

        number.Value.ShouldBe("FR44732829320");
        number.ToField().SchemeId.ShouldBe("9957");
    }

    [Fact]
    public void AWrongKeyIsRefusedEvenWhenTheSirenIsRight() =>
        FrVatNumber.IsValid("FR45732829320").ShouldBeFalse();

    [Fact]
    public void AKeyContainingLettersIsAcceptedButSaysItCouldNotBeRecomputed()
    {
        FrVatNumber.TryParse("FRK7732829320", out FrVatNumber number).ShouldBeTrue();

        number.IsKeyVerified.ShouldBeFalse();
        number.Siren.Value.ShouldBe("732829320");
    }

    [Fact]
    public void AnIdentifierBecomesAFieldWithTheSchemeThatGivesItMeaning()
    {
        FrSiren.Parse("732829320").ToField().SchemeId.ShouldBe("0002");
        FrSiret.Parse("73282932000074").ToField().SchemeId.ShouldBe("0009");
    }
}
