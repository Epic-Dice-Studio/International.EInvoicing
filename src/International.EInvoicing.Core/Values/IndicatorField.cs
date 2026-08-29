using International.EInvoicing.Diagnostics;

namespace International.EInvoicing.Values;

/// <summary>
/// A boolean. Documents spell it <c>true</c>, <c>false</c>, <c>1</c> or <c>0</c> depending on the syntax, and
/// the raw text records which was used.
/// </summary>
/// <param name="Value">The indicator.</param>
/// <param name="Source">Where it came from, or <c>null</c> when built in code.</param>
public readonly record struct IndicatorField(bool? Value, FieldSource? Source = null) : IField
{
    /// <summary>A field carrying nothing.</summary>
    public static IndicatorField Unset => default;

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

    /// <summary>Wraps an indicator produced by code.</summary>
    public static implicit operator IndicatorField(bool value) => new(value);

    /// <summary>Unwraps the indicator.</summary>
    public static implicit operator bool?(IndicatorField field) => field.Value;

    /// <inheritdoc />
    public override string ToString() => Raw ?? Value?.ToString() ?? string.Empty;
}
