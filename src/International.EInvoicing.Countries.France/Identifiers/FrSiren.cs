using System.Diagnostics.CodeAnalysis;
using International.EInvoicing.Identifiers;
using International.EInvoicing.Values;

namespace International.EInvoicing.Countries.France.Identifiers;

/// <summary>
/// A SIREN: the nine digits that identify a French business.
/// </summary>
/// <remarks>
/// The 2026 reform requires it on domestic invoices, which is why it is worth checking rather than
/// pattern-matching: a SIREN carries a Luhn check digit, so a typo is caught before the invoice leaves.
/// </remarks>
public readonly record struct FrSiren
{
    private FrSiren(string value) => Value = value;

    /// <summary>The nine digits, without spaces.</summary>
    public string Value { get; }

    /// <summary>Whether this holds a SIREN at all.</summary>
    public bool IsSet => !string.IsNullOrEmpty(Value);

    /// <summary>Reads a SIREN, accepting the spaces people write it with.</summary>
    /// <returns><c>true</c> when it is nine digits satisfying the Luhn check.</returns>
    public static bool TryParse(string? value, out FrSiren siren)
    {
        siren = default;

        if (value is null)
        {
            return false;
        }

        string compact = CheckDigit.Compact(value);
        if (compact.Length != 9 || !CheckDigit.SatisfiesLuhn(compact))
        {
            return false;
        }

        siren = new FrSiren(compact);
        return true;
    }

    /// <summary>Whether the text is a valid SIREN.</summary>
    public static bool IsValid([NotNullWhen(true)] string? value) => TryParse(value, out _);

    /// <summary>Reads a SIREN, or throws when it is not one.</summary>
    /// <exception cref="FormatException">The value is not a valid SIREN.</exception>
    public static FrSiren Parse(string value) =>
        TryParse(value, out FrSiren siren)
            ? siren
            : throw new FormatException($"'{value}' is not a SIREN: nine digits with a Luhn check are expected.");

    /// <summary>
    /// The identifier as an invoice carries it, with the scheme that says what the digits mean.
    /// </summary>
    public IdentifierField ToField() => new(Value, FrIdentifierSchemes.Siren);

    /// <summary>The SIREN grouped as it is written on paper, <c>732 829 320</c>.</summary>
    public string ToFormattedString() =>
        IsSet ? $"{Value[..3]} {Value[3..6]} {Value[6..]}" : string.Empty;

    /// <inheritdoc />
    public override string ToString() => Value ?? string.Empty;
}
