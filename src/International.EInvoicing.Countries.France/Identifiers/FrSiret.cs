using System.Diagnostics.CodeAnalysis;
using International.EInvoicing.Identifiers;
using International.EInvoicing.Values;

namespace International.EInvoicing.Countries.France.Identifiers;

/// <summary>
/// A SIRET: a SIREN followed by the five digits identifying one establishment of that business.
/// </summary>
/// <remarks>
/// Checked with Luhn, except for La Poste, whose SIRETs predate the rule and satisfy a different one — a
/// validator that does not know that rejects genuine invoices from the largest employer in France.
/// </remarks>
public readonly record struct FrSiret
{
    private const string LaPosteSiren = "356000000";

    private FrSiret(string value) => Value = value;

    /// <summary>The fourteen digits, without spaces.</summary>
    public string Value { get; }

    /// <summary>Whether this holds a SIRET at all.</summary>
    public bool IsSet => !string.IsNullOrEmpty(Value);

    /// <summary>The business the establishment belongs to.</summary>
    public FrSiren Siren => IsSet && FrSiren.TryParse(Value[..9], out FrSiren siren) ? siren : default;

    /// <summary>The five digits identifying the establishment.</summary>
    public string EstablishmentNumber => IsSet ? Value[9..] : string.Empty;

    /// <summary>Reads a SIRET, accepting the spaces people write it with.</summary>
    public static bool TryParse(string? value, out FrSiret siret)
    {
        siret = default;

        if (value is null)
        {
            return false;
        }

        string compact = CheckDigit.Compact(value);
        if (compact.Length != 14 || !compact.All(char.IsAsciiDigit))
        {
            return false;
        }

        bool valid = compact.StartsWith(LaPosteSiren, StringComparison.Ordinal)
            ? SatisfiesLaPosteRule(compact)
            : CheckDigit.SatisfiesLuhn(compact);

        if (!valid)
        {
            return false;
        }

        siret = new FrSiret(compact);
        return true;
    }

    /// <summary>Whether the text is a valid SIRET.</summary>
    public static bool IsValid([NotNullWhen(true)] string? value) => TryParse(value, out _);

    /// <summary>Reads a SIRET, or throws when it is not one.</summary>
    /// <exception cref="FormatException">The value is not a valid SIRET.</exception>
    public static FrSiret Parse(string value) =>
        TryParse(value, out FrSiret siret)
            ? siret
            : throw new FormatException($"'{value}' is not a SIRET: fourteen digits with a valid check are expected.");

    /// <summary>The identifier as an invoice carries it, with its scheme.</summary>
    public IdentifierField ToField() => new(Value, FrIdentifierSchemes.Siret);

    /// <summary>The SIRET grouped as it is written on paper, <c>732 829 320 00074</c>.</summary>
    public string ToFormattedString() =>
        IsSet ? $"{Value[..3]} {Value[3..6]} {Value[6..9]} {Value[9..]}" : string.Empty;

    /// <inheritdoc />
    public override string ToString() => Value ?? string.Empty;

    /// <summary>La Poste's establishments satisfy a digit sum divisible by five instead of Luhn.</summary>
    private static bool SatisfiesLaPosteRule(string digits) =>
        digits.Sum(digit => digit - '0') % 5 == 0;
}
