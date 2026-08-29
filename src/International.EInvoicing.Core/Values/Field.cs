using System.Diagnostics.CodeAnalysis;
using International.EInvoicing.Diagnostics;

namespace International.EInvoicing.Values;

/// <summary>
/// A value type carried by a document, alongside the raw text it came from.
/// Specialised fields such as <see cref="AmountField"/> add the attributes their syntax allows.
/// </summary>
/// <typeparam name="T">The value type carried.</typeparam>
/// <param name="Value">The typed value, or <c>null</c> when absent or unreadable.</param>
/// <param name="Source">Where it came from, or <c>null</c> when built in code.</param>
public readonly record struct Field<T>(T? Value, FieldSource? Source = null) : IField
    where T : struct
{
    /// <summary>A field carrying nothing.</summary>
    [SuppressMessage(
        "Design",
        "CA1000:Do not declare static members on generic types",
        Justification = "Every field type exposes Unset; dropping it here alone would make the family inconsistent.")]
    public static Field<T> Unset => default;

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

    /// <summary>Wraps a value produced by code.</summary>
    public static implicit operator Field<T>(T value) => new(value);

    /// <summary>Unwraps the typed value.</summary>
    public static implicit operator T?(Field<T> field) => field.Value;

    /// <inheritdoc />
    public override string ToString() => Raw ?? Value?.ToString() ?? string.Empty;
}
