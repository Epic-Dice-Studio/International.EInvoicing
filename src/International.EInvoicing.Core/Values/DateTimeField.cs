using System.Globalization;
using International.EInvoicing.Diagnostics;

namespace International.EInvoicing.Values;

/// <summary>
/// A moment in time, with the format the document expressed it in. Invoices carry dates
/// (<see cref="DateField"/>); lifecycle messages carry timestamps, because when a status occurred is the
/// point of the message.
/// </summary>
/// <param name="Value">The moment, or <c>null</c> when the text could not be read.</param>
/// <param name="FormatCode">The UNTDID 2379 code from the <c>format</c> attribute.</param>
/// <param name="Source">Where it came from, or <c>null</c> when built in code.</param>
public readonly record struct DateTimeField(
    DateTimeOffset? Value,
    string? FormatCode = null,
    FieldSource? Source = null) : IField
{
    /// <summary>UNTDID 2379 code for <c>CCYYMMDDHHMMSS</c>, the format lifecycle messages use.</summary>
    public const string FormatCcyyMmDdHhMmSs = "204";

    /// <summary>A field carrying nothing.</summary>
    public static DateTimeField Unset => default;

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

    /// <summary>Wraps a moment produced by code.</summary>
    public static implicit operator DateTimeField(DateTimeOffset value) => new(value);

    /// <summary>Unwraps the moment.</summary>
    public static implicit operator DateTimeOffset?(DateTimeField field) => field.Value;

    /// <inheritdoc />
    public override string ToString() =>
        Raw ?? Value?.ToString("yyyy-MM-dd HH:mm:ssK", CultureInfo.InvariantCulture) ?? string.Empty;
}
