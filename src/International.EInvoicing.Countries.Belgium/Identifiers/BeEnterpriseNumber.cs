using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using International.EInvoicing.Identifiers;
using International.EInvoicing.Values;

namespace International.EInvoicing.Countries.Belgium.Identifiers;

/// <summary>
/// A Belgian enterprise number — <em>ondernemingsnummer</em>, <em>numéro d'entreprise</em> — the ten digits
/// the Crossroads Bank assigns to a business.
/// </summary>
/// <remarks>
/// The VAT number is the same digits with <c>BE</c> in front, which is why both are read here rather than
/// modelled twice.
/// </remarks>
public readonly record struct BeEnterpriseNumber
{
    /// <summary>The scheme identifier a Belgian enterprise number is declared under on an invoice.</summary>
    public const string Scheme = "0208";

    private BeEnterpriseNumber(string value) => Value = value;

    /// <summary>The ten digits, without dots.</summary>
    public string Value { get; }

    /// <summary>Whether this holds a number at all.</summary>
    public bool IsSet => !string.IsNullOrEmpty(Value);

    /// <summary>The matching VAT number, the same digits prefixed with the country code.</summary>
    public string VatNumber => IsSet ? "BE" + Value : string.Empty;

    /// <summary>Reads an enterprise number, with or without its dots and country prefix.</summary>
    public static bool TryParse(string? value, out BeEnterpriseNumber number)
    {
        number = default;

        if (value is null)
        {
            return false;
        }

        string compact = CheckDigit.Compact(value).ToUpperInvariant();

        if (compact.StartsWith("BE", StringComparison.Ordinal))
        {
            compact = compact[2..];
        }

        if (compact.Length != 10 || !compact.All(char.IsAsciiDigit))
        {
            return false;
        }

        if (CheckDigit.Modulo97(compact.AsSpan(0, 8)) is not { } remainder)
        {
            return false;
        }

        string expected = (97 - remainder).ToString("D2", CultureInfo.InvariantCulture);
        if (!string.Equals(compact[8..], expected, StringComparison.Ordinal))
        {
            return false;
        }

        number = new BeEnterpriseNumber(compact);
        return true;
    }

    /// <summary>Whether the text is a valid enterprise number.</summary>
    public static bool IsValid([NotNullWhen(true)] string? value) => TryParse(value, out _);

    /// <summary>Reads an enterprise number, or throws when it is not one.</summary>
    /// <exception cref="FormatException">The value is not a valid enterprise number.</exception>
    public static BeEnterpriseNumber Parse(string value) =>
        TryParse(value, out BeEnterpriseNumber number)
            ? number
            : throw new FormatException($"'{value}' is not a Belgian enterprise number.");

    /// <summary>The identifier as an invoice carries it, with its scheme.</summary>
    public IdentifierField ToField() => new(Value, Scheme);

    /// <summary>The number grouped as it is written, <c>0417.497.106</c>.</summary>
    public string ToFormattedString() =>
        IsSet ? $"{Value[..4]}.{Value[4..7]}.{Value[7..]}" : string.Empty;

    /// <inheritdoc />
    public override string ToString() => Value ?? string.Empty;
}
