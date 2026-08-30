using International.EInvoicing.Identifiers;
using International.EInvoicing.Values;

namespace International.EInvoicing.Countries.Sweden.Identifiers;

/// <summary>
/// A Swedish organisation number — <em>organisationsnummer</em> — the ten digits Bolagsverket assigns to a
/// legal person.
/// </summary>
/// <remarks>
/// The check is the Luhn one Peppol enforces on scheme 0007 (<c>PEPPOL-COMMON-R049</c>, <c>SE-R-013</c>):
/// ten digits, the last of which is the Luhn check digit of the first nine.
/// </remarks>
public readonly record struct SeOrganisationNumber
{
    /// <summary>The scheme identifier a Swedish organisation number is declared under on an invoice.</summary>
    public const string Scheme = "0007";

    private SeOrganisationNumber(string value) => Value = value;

    /// <summary>The ten digits, without the hyphen.</summary>
    public string Value { get; }

    /// <summary>Whether this holds a number at all.</summary>
    public bool IsSet => !string.IsNullOrEmpty(Value);

    /// <summary>The matching VAT number: the digits with SE in front and 01 behind.</summary>
    public string VatNumber => IsSet ? "SE" + Value + "01" : string.Empty;

    /// <summary>Reads an organisation number, with or without its hyphen and country prefix.</summary>
    public static bool TryParse(string? value, out SeOrganisationNumber number)
    {
        number = default;

        if (value is null)
        {
            return false;
        }

        string compact = CheckDigit.Compact(value).ToUpperInvariant();

        if (compact.StartsWith("SE", StringComparison.Ordinal))
        {
            compact = compact[2..];

            if (compact.Length == 12 && compact.EndsWith("01", StringComparison.Ordinal))
            {
                compact = compact[..^2];
            }
        }

        if (compact.Length != 10 || !CheckDigit.SatisfiesLuhn(compact))
        {
            return false;
        }

        number = new SeOrganisationNumber(compact);
        return true;
    }

    /// <summary>Reads an organisation number, or throws.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
    /// <exception cref="FormatException">The value is not a Swedish organisation number.</exception>
    public static SeOrganisationNumber Parse(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return TryParse(value, out SeOrganisationNumber number)
            ? number
            : throw new FormatException(
                $"'{value}' is not a Swedish organisation number: ten digits whose last digit is the Luhn "
                + "check digit of the first nine, as Peppol enforces on scheme 0007.");
    }

    /// <summary>Whether a value is a Swedish organisation number.</summary>
    public static bool IsValid(string? value) => TryParse(value, out _);

    /// <summary>The number as an identifier field, in the scheme Peppol reserves for it.</summary>
    public IdentifierField ToField() => new(Value, Scheme);

    /// <summary>The number as Swedes write it, with a hyphen before the last four digits.</summary>
    public string ToFormattedString() => IsSet ? $"{Value[..6]}-{Value[6..]}" : string.Empty;

    /// <inheritdoc />
    public override string ToString() => Value ?? string.Empty;
}
