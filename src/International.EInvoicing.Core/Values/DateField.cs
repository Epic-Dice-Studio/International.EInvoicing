using System.Globalization;
using International.EInvoicing.Diagnostics;

namespace International.EInvoicing.Values;

/// <summary>
/// A date, with the format the document expressed it in. The format code is normative in CII
/// (<c>102</c> for <c>CCYYMMDD</c>, <c>610</c> for a month, <c>616</c> for a week) and is preserved so the
/// date can be written back exactly as it arrived.
/// </summary>
/// <param name="Value">The date, or <c>null</c> when the format carries less than a day.</param>
/// <param name="FormatCode">The UNTDID 2379 code from the <c>format</c> attribute.</param>
/// <param name="Source">Where it came from, or <c>null</c> when built in code.</param>
public readonly record struct DateField(DateOnly? Value, string? FormatCode = null, FieldSource? Source = null)
    : IField
{
    /// <summary>UNTDID 2379 code for <c>CCYYMMDD</c>, the format used by CII invoices.</summary>
    public const string FormatCcyyMmDd = "102";

    /// <summary>A field carrying nothing.</summary>
    public static DateField Unset => default;

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

    /// <summary>Wraps a date produced by code.</summary>
    public static implicit operator DateField(DateOnly value) => new(value);

    /// <summary>Unwraps the date.</summary>
    public static implicit operator DateOnly?(DateField field) => field.Value;

    /// <inheritdoc />
    public override string ToString() =>
        Raw ?? Value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty;
}
