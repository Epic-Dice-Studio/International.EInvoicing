using International.EInvoicing.Diagnostics;

namespace International.EInvoicing.Values;

/// <summary>An embedded binary object — an attached document, a logo — with what it is and what it is called.</summary>
/// <param name="Value">The decoded bytes.</param>
/// <param name="MimeCode">The <c>mimeCode</c> attribute.</param>
/// <param name="Filename">The <c>filename</c> attribute.</param>
/// <param name="Source">Where it came from, or <c>null</c> when built in code.</param>
public readonly record struct BinaryField(
    byte[]? Value,
    string? MimeCode = null,
    string? Filename = null,
    FieldSource? Source = null) : IField
{
    /// <summary>A field carrying nothing.</summary>
    public static BinaryField Unset => default;

    /// <inheritdoc />
    public string? Raw => Source?.Raw;

    /// <inheritdoc />
    public SourceLocation Location => Source?.Location ?? SourceLocation.None;

    /// <inheritdoc />
    public Diagnostic? Diagnostic => Source?.Diagnostic;

    /// <inheritdoc />
    public bool HasValue => Value is not null;

    /// <inheritdoc />
    public bool IsSet => Value is not null || Source is not null;

    /// <inheritdoc />
    public bool IsRawOnly => Value is null && Source?.Raw is not null;

    /// <inheritdoc />
    public bool IsFromSource => Source is not null;

    /// <inheritdoc />
    public object? UntypedValue => Value;

    /// <summary>Compares content, not references, so two fields carrying the same bytes are equal.</summary>
    public bool Equals(BinaryField other) =>
        MimeCode == other.MimeCode
        && Filename == other.Filename
        && Source == other.Source
        && (ReferenceEquals(Value, other.Value)
            || (Value is not null && other.Value is not null && Value.AsSpan().SequenceEqual(other.Value)));

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Value?.Length, MimeCode, Filename, Source);

    /// <inheritdoc />
    public override string ToString() => (Filename, Value) switch
    {
        (not null, _) => Filename,
        (null, not null) => $"{Value.Length} bytes",
        _ => string.Empty,
    };
}
