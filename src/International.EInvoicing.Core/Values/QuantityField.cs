using System.Globalization;
using International.EInvoicing.Diagnostics;

namespace International.EInvoicing.Values;

/// <summary>A quantity and the unit it is expressed in.</summary>
/// <param name="Value">The quantity.</param>
/// <param name="UnitCode">The UN/ECE Recommendation 20 code from the <c>unitCode</c> attribute.</param>
/// <param name="UnitCodeListVersion">The <c>unitCodeListVersionID</c> attribute.</param>
/// <param name="Source">Where it came from, or <c>null</c> when built in code.</param>
public readonly record struct QuantityField(
    decimal? Value,
    string? UnitCode = null,
    string? UnitCodeListVersion = null,
    FieldSource? Source = null) : IField
{
    /// <summary>A field carrying nothing.</summary>
    public static QuantityField Unset => default;

    /// <inheritdoc />
    public string? Raw => Source?.Raw;

    /// <inheritdoc />
    public SourceLocation Location => Source?.Location ?? SourceLocation.None;

    /// <inheritdoc />
    public Diagnostic? Diagnostic => Source?.Diagnostic;

    /// <inheritdoc />
    public bool HasValue => Value.HasValue;

    /// <inheritdoc />
    public bool IsSet => Value.HasValue || Source is not null;

    /// <inheritdoc />
    public bool IsRawOnly => !Value.HasValue && Source?.Raw is not null;

    /// <inheritdoc />
    public bool IsFromSource => Source is not null;

    /// <inheritdoc />
    public object? UntypedValue => Value;

    /// <summary>Wraps a quantity produced by code, with no unit.</summary>
    public static implicit operator QuantityField(decimal value) => new(value);

    /// <summary>Unwraps the quantity.</summary>
    public static implicit operator decimal?(QuantityField field) => field.Value;

    /// <inheritdoc />
    public override string ToString() => (Value, UnitCode) switch
    {
        (null, _) => Raw ?? string.Empty,
        (_, null) => Value.Value.ToString(CultureInfo.InvariantCulture),
        _ => $"{Value.Value.ToString(CultureInfo.InvariantCulture)} {UnitCode}",
    };
}
