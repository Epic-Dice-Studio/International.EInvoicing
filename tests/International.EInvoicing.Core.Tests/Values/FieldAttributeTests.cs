using International.EInvoicing.Values;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Core.Tests.Values;

/// <summary>
/// The attributes carried next to a value are data, not decoration: a scheme decides what an identifier
/// means, a list version decides whether a code still exists, a unit decides what a quantity measures.
/// </summary>
public class FieldAttributeTests
{
    [Fact]
    public void AnIdentifier_KeepsItsScheme()
    {
        var siret = new IdentifierField("73282932000074", SchemeId: "0009", SchemeAgencyId: "6");

        siret.Value.ShouldBe("73282932000074");
        siret.SchemeId.ShouldBe("0009");
        siret.ToString().ShouldBe("73282932000074 [0009]");
    }

    [Fact]
    public void ACode_KeepsTheListItCameFrom()
    {
        var category = new CodeField("S", ListId: "UNCL5305", ListVersionId: "D22B");

        category.ListId.ShouldBe("UNCL5305");
        category.ListVersionId.ShouldBe("D22B");
    }

    [Fact]
    public void AnAmount_KeepsItsCurrencyAndFormatsInvariantly()
    {
        var total = new AmountField(1234.56m, "EUR");

        total.ToString().ShouldBe("1234.56 EUR");
    }

    [Fact]
    public void AQuantity_KeepsItsUnit()
    {
        var quantity = new QuantityField(2.5m, "HUR");

        quantity.ToString().ShouldBe("2.5 HUR");
    }

    [Fact]
    public void TextKeepsItsLanguage()
    {
        var note = new TextField("Facture acquittée", LanguageId: "fr");

        note.LanguageId.ShouldBe("fr");
    }

    [Fact]
    public void ATimestampKeepsItsFormatCode()
    {
        var issued = new DateTimeField(
            new DateTimeOffset(2025, 7, 1, 15, 15, 0, TimeSpan.Zero),
            DateTimeField.FormatCcyyMmDdHhMmSs);

        issued.FormatCode.ShouldBe("204");
        issued.ToString().ShouldBe("2025-07-01 15:15:00+00:00");
    }

    [Fact]
    public void BinaryContentComparesByValueNotByReference()
    {
        var first = new BinaryField([1, 2, 3], "application/pdf", "annex.pdf");
        var second = new BinaryField([1, 2, 3], "application/pdf", "annex.pdf");
        var different = new BinaryField([1, 2, 4], "application/pdf", "annex.pdf");

        first.ShouldBe(second);
        first.ShouldNotBe(different);
        first.ToString().ShouldBe("annex.pdf");
    }
}
