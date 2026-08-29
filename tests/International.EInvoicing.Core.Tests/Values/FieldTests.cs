using International.EInvoicing.Diagnostics;
using International.EInvoicing.Values;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Core.Tests.Values;

public class FieldTests
{
    private static readonly FieldSource ParsedSource =
        new("20260829", new SourceLocation("/Invoice/cbc:IssueDate", 4, 9));

    [Fact]
    public void Unset_CarriesNothing()
    {
        Field<decimal> field = Field<decimal>.Unset;

        field.IsSet.ShouldBeFalse();
        field.HasValue.ShouldBeFalse();
        field.IsRawOnly.ShouldBeFalse();
        field.IsFromSource.ShouldBeFalse();
        field.Raw.ShouldBeNull();
    }

    [Fact]
    public void AValueAssignedInCode_IsSetButNotFromSource()
    {
        Field<decimal> field = 42m;

        field.IsSet.ShouldBeTrue();
        field.HasValue.ShouldBeTrue();
        field.IsFromSource.ShouldBeFalse();
        field.IsRawOnly.ShouldBeFalse();
    }

    [Fact]
    public void AValueReadFromADocument_KeepsItsRawTextAndLocation()
    {
        var field = new DateField(new DateOnly(2026, 8, 29), DateField.FormatCcyyMmDd, ParsedSource);

        field.Value.ShouldBe(new DateOnly(2026, 8, 29));
        field.Raw.ShouldBe("20260829");
        field.FormatCode.ShouldBe("102");
        field.Location.Path.ShouldBe("/Invoice/cbc:IssueDate");
        field.Location.Line.ShouldBe(4);
        field.IsFromSource.ShouldBeTrue();
    }

    [Fact]
    public void AnUnreadableValue_KeepsItsTextAndExplainsWhy()
    {
        Diagnostic diagnostic = Diagnostic.Create(DiagnosticCodes.InvalidValue, "29/08/2026", "a date");
        var field = new DateField(null, "102", new FieldSource("29/08/2026", SourceLocation.None, diagnostic));

        field.HasValue.ShouldBeFalse();
        field.IsSet.ShouldBeTrue();
        field.IsRawOnly.ShouldBeTrue();
        field.Raw.ShouldBe("29/08/2026");
        field.Diagnostic!.Code.ShouldBe("EIV2001");
    }

    [Fact]
    public void ImplicitConversions_KeepEverydayUseOrdinary()
    {
        DateField field = new DateOnly(2026, 9, 1);
        DateOnly? back = field;

        back.ShouldBe(new DateOnly(2026, 9, 1));
    }

    [Fact]
    public void ToString_PrefersTheRawTextSoDisplayMatchesTheDocument()
    {
        var parsed = new DateField(new DateOnly(2026, 8, 29), DateField.FormatCcyyMmDd, ParsedSource);
        DateField built = new DateOnly(2026, 8, 29);

        parsed.ToString().ShouldBe("20260829");
        built.ToString().ShouldBe("2026-08-29");
    }

    [Fact]
    public void EveryFieldType_ExposesTheCommonContract()
    {
        IField[] fields =
        [
            new Field<decimal>(1m),
            new TextField("note"),
            new IdentifierField("FR12345678901"),
            new CodeField("380"),
            new AmountField(10m),
            new QuantityField(2m),
            new DateField(new DateOnly(2026, 1, 1)),
            new IndicatorField(true),
            new BinaryField([1, 2, 3]),
        ];

        fields.ShouldAllBe(f => f.IsSet && f.HasValue && !f.IsRawOnly && !f.IsFromSource);
        fields.ShouldAllBe(f => f.UntypedValue != null);
    }
}
