using International.EInvoicing.Diagnostics;

namespace International.EInvoicing.Values;

/// <summary>
/// A value drawn from a code list, with the list it was drawn from. The list version matters: codes are
/// retired between releases, and a document valid last year can carry a code that no longer exists.
/// </summary>
/// <param name="Value">The code.</param>
/// <param name="ListId">The <c>listID</c> attribute.</param>
/// <param name="ListVersionId">The <c>listVersionID</c> attribute.</param>
/// <param name="ListAgencyId">The <c>listAgencyID</c> attribute.</param>
/// <param name="Source">Where it came from, or <c>null</c> when built in code.</param>
public readonly record struct CodeField(
    string? Value,
    string? ListId = null,
    string? ListVersionId = null,
    string? ListAgencyId = null,
    FieldSource? Source = null) : IField
{
    /// <summary>A field carrying nothing.</summary>
    public static CodeField Unset => default;

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

    /// <summary>Wraps a code produced by code, with no list.</summary>
    public static implicit operator CodeField(string? value) => new(value);

    /// <summary>Unwraps the code.</summary>
    public static implicit operator string?(CodeField field) => field.Value;

    /// <inheritdoc />
    public override string ToString() => Raw ?? Value ?? string.Empty;
}
