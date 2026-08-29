using International.EInvoicing.Diagnostics;

namespace International.EInvoicing.Values;

/// <summary>
/// An identifier and the scheme it belongs to. The scheme is what makes an identifier meaningful: the same
/// digits are a SIRET, a VAT number or a Peppol participant depending on it.
/// </summary>
/// <param name="Value">The identifier itself.</param>
/// <param name="SchemeId">The <c>schemeID</c> attribute.</param>
/// <param name="SchemeAgencyId">The <c>schemeAgencyID</c> attribute.</param>
/// <param name="SchemeVersionId">The <c>schemeVersionID</c> attribute.</param>
/// <param name="Source">Where it came from, or <c>null</c> when built in code.</param>
public readonly record struct IdentifierField(
    string? Value,
    string? SchemeId = null,
    string? SchemeAgencyId = null,
    string? SchemeVersionId = null,
    FieldSource? Source = null) : IField
{
    /// <summary>A field carrying nothing.</summary>
    public static IdentifierField Unset => default;

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

    /// <summary>Wraps an identifier produced by code, with no scheme.</summary>
    public static implicit operator IdentifierField(string? value) => new(value);

    /// <summary>Unwraps the identifier.</summary>
    public static implicit operator string?(IdentifierField field) => field.Value;

    /// <inheritdoc />
    public override string ToString() =>
        SchemeId is null ? Raw ?? Value ?? string.Empty : $"{Value} [{SchemeId}]";
}
