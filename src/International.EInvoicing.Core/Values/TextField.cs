using International.EInvoicing.Diagnostics;

namespace International.EInvoicing.Values;

/// <summary>Free text, with the language it is written in.</summary>
/// <param name="Value">The text.</param>
/// <param name="LanguageId">The <c>languageID</c> attribute, when the document carried one.</param>
/// <param name="Source">Where it came from, or <c>null</c> when built in code.</param>
public readonly record struct TextField(string? Value, string? LanguageId = null, FieldSource? Source = null)
    : IField
{
    /// <summary>A field carrying nothing.</summary>
    public static TextField Unset => default;

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

    /// <summary>Wraps text produced by code.</summary>
    public static implicit operator TextField(string? value) => new(value);

    /// <summary>Unwraps the text.</summary>
    public static implicit operator string?(TextField field) => field.Value;

    /// <inheritdoc />
    public override string ToString() => Raw ?? Value ?? string.Empty;
}
