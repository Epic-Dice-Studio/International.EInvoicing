using System.Globalization;
using International.EInvoicing.Diagnostics;

namespace International.EInvoicing.Values;

/// <summary>A monetary amount and its currency.</summary>
/// <param name="Value">The amount. Always <see cref="decimal"/>: binary floating point loses money.</param>
/// <param name="CurrencyCode">The ISO 4217 code from the <c>currencyID</c> attribute.</param>
/// <param name="Source">Where it came from, or <c>null</c> when built in code.</param>
public readonly record struct AmountField(decimal? Value, string? CurrencyCode = null, FieldSource? Source = null)
    : IField
{
    /// <summary>A field carrying nothing.</summary>
    public static AmountField Unset => default;

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

    /// <summary>Wraps an amount produced by code, with no currency.</summary>
    public static implicit operator AmountField(decimal value) => new(value);

    /// <summary>Unwraps the amount.</summary>
    public static implicit operator decimal?(AmountField field) => field.Value;

    /// <inheritdoc />
    public override string ToString() => (Value, CurrencyCode) switch
    {
        (null, _) => Raw ?? string.Empty,
        (_, null) => Value.Value.ToString(CultureInfo.InvariantCulture),
        _ => $"{Value.Value.ToString(CultureInfo.InvariantCulture)} {CurrencyCode}",
    };
}
