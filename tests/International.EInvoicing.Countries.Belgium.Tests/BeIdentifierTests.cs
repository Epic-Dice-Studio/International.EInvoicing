using International.EInvoicing.Countries.Belgium.Identifiers;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Countries.Belgium.Tests;

public class BeIdentifierTests
{
    [Theory]
    [InlineData("0417497106")]
    [InlineData("0417.497.106")]
    [InlineData("BE 0417.497.106")]
    public void AnEnterpriseNumberIsReadHoweverItIsWritten(string written)
    {
        BeEnterpriseNumber.TryParse(written, out BeEnterpriseNumber number).ShouldBeTrue();

        number.Value.ShouldBe("0417497106");
        number.ToFormattedString().ShouldBe("0417.497.106");
        number.VatNumber.ShouldBe("BE0417497106");
    }

    [Theory]
    [InlineData("0417497107")]   // check digits off by one
    [InlineData("041749710")]    // too short
    [InlineData("04174971060")]  // too long
    [InlineData("")]
    [InlineData(null)]
    public void SomethingThatIsNotAnEnterpriseNumberIsRefused(string? written) =>
        BeEnterpriseNumber.IsValid(written).ShouldBeFalse();

    [Fact]
    public void AnEnterpriseNumberBecomesAFieldWithItsScheme() =>
        BeEnterpriseNumber.Parse("0417497106").ToField().SchemeId.ShouldBe("0208");

    [Fact]
    public void AStructuredCommunicationIsBuiltFromAReferenceOfYourOwn()
    {
        BeStructuredCommunication reference = BeStructuredCommunication.ForInvoice(123456789);

        BeStructuredCommunication.IsValid(reference.ToString()).ShouldBeTrue();
        reference.ToString().ShouldStartWith("+++");
        reference.ToString().ShouldEndWith("+++");
        reference.Digits.Length.ShouldBe(12);
    }

    [Fact]
    public void ItIsReadBackFromHoweverItWasWritten()
    {
        BeStructuredCommunication built = BeStructuredCommunication.ForInvoice(2026000123);

        BeStructuredCommunication.Parse(built.ToString()).Digits.ShouldBe(built.Digits);
        BeStructuredCommunication.Parse(built.Digits).Digits.ShouldBe(built.Digits);
    }

    [Fact]
    public void AReferenceWithTheWrongCheckIsRefused()
    {
        BeStructuredCommunication good = BeStructuredCommunication.ForInvoice(123456789);
        string wrong = good.Digits[..10] + (good.Digits[10..] == "01" ? "02" : "01");

        BeStructuredCommunication.IsValid(wrong).ShouldBeFalse();
    }

    [Fact]
    public void TheCheckIsNeverZeroBecauseZeroIsWrittenAsNinetySeven()
    {
        // 97 divides this reference exactly, so the check must be written 97 rather than 00.
        BeStructuredCommunication reference = BeStructuredCommunication.ForInvoice(97 * 10_309_278L);

        reference.Digits[10..].ShouldBe("97");
        BeStructuredCommunication.IsValid(reference.ToString()).ShouldBeTrue();
    }

    [Fact]
    public void AReferenceTooLargeToFitIsRefused() =>
        Should.Throw<ArgumentOutOfRangeException>(() => BeStructuredCommunication.ForInvoice(10_000_000_000L));

    [Fact]
    public void ItGoesIntoTheRemittanceInformation() =>
        BeStructuredCommunication.ForInvoice(1).ToField().Value.ShouldStartWith("+++");
}
