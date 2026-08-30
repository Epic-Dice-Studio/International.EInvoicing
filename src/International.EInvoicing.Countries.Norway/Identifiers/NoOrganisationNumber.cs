using International.EInvoicing.Identifiers;
using International.EInvoicing.Values;

namespace International.EInvoicing.Countries.Norway.Identifiers;

/// <summary>
/// A Norwegian organisation number — <em>organisasjonsnummer</em> — the nine digits the Brønnøysund
/// registers assign to a business.
/// </summary>
/// <remarks>
/// The check is the modulo 11 one Peppol enforces on scheme 0192 (<c>PEPPOL-COMMON-R041</c>): weights
/// 3, 2, 7, 6, 5, 4, 3, 2 applied from the digit before the check digit leftwards. A number whose remainder
/// leaves no valid check digit does not exist, which is why parsing refuses it rather than rounding.
/// </remarks>
public readonly record struct NoOrganisationNumber
{
    /// <summary>The scheme identifier a Norwegian organisation number is declared under on an invoice.</summary>
    public const string Scheme = "0192";

    private static readonly int[] Weights = [3, 2, 7, 6, 5, 4, 3, 2];

    private NoOrganisationNumber(string value) => Value = value;

    /// <summary>The nine digits, without spaces.</summary>
    public string Value { get; }

    /// <summary>Whether this holds a number at all.</summary>
    public bool IsSet => !string.IsNullOrEmpty(Value);

    /// <summary>The matching VAT number: the same digits, prefixed with the country code and suffixed MVA.</summary>
    public string VatNumber => IsSet ? "NO" + Value + "MVA" : string.Empty;

    /// <summary>Reads an organisation number, however it is spaced, with or without its country prefix.</summary>
    public static bool TryParse(string? value, out NoOrganisationNumber number)
    {
        number = default;

        if (value is null)
        {
            return false;
        }

        string compact = CheckDigit.Compact(value).ToUpperInvariant();

        if (compact.StartsWith("NO", StringComparison.Ordinal))
        {
            compact = compact[2..];
        }

        if (compact.EndsWith("MVA", StringComparison.Ordinal))
        {
            compact = compact[..^3];
        }

        if (compact.Length != 9 || !CheckDigit.SatisfiesMod11(compact, Weights))
        {
            return false;
        }

        number = new NoOrganisationNumber(compact);
        return true;
    }

    /// <summary>Reads an organisation number, or throws.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
    /// <exception cref="FormatException">The value is not a Norwegian organisation number.</exception>
    public static NoOrganisationNumber Parse(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return TryParse(value, out NoOrganisationNumber number)
            ? number
            : throw new FormatException(
                $"'{value}' is not a Norwegian organisation number: nine digits satisfying the modulo 11 "
                + "check Peppol enforces on scheme 0192.");
    }

    /// <summary>Whether a value is a Norwegian organisation number.</summary>
    public static bool IsValid(string? value) => TryParse(value, out _);

    /// <summary>The number as an identifier field, in the scheme Peppol reserves for it.</summary>
    public IdentifierField ToField() => new(Value, Scheme);

    /// <summary>The number in the three-by-three grouping Norwegians write it in.</summary>
    public string ToFormattedString() =>
        IsSet ? $"{Value[..3]} {Value[3..6]} {Value[6..]}" : string.Empty;

    /// <inheritdoc />
    public override string ToString() => Value ?? string.Empty;
}
