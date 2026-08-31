using International.EInvoicing.Identifiers;
using International.EInvoicing.Values;

namespace International.EInvoicing.Countries.Greece.Identifiers;

/// <summary>
/// A Greek tax identifier — the AFM, <em>Αριθμός Φορολογικού Μητρώου</em> — nine digits.
/// </summary>
/// <remarks>
/// The check is the one Peppol enforces on scheme <c>9933</c> (<c>GR-R-009</c>, <c>GR-R-010</c>), and it is
/// unlike the others here: the first eight digits are weighted by descending powers of two — 256, 128, 64,
/// 32, 16, 8, 4, 2 — and the ninth must equal the sum modulo 11, modulo 10.
/// </remarks>
public readonly record struct GrTaxIdentifier
{
    /// <summary>The scheme identifier a Greek AFM is declared under on an invoice.</summary>
    public const string Scheme = "9933";

    private GrTaxIdentifier(string value) => Value = value;

    /// <summary>The nine digits.</summary>
    public string Value { get; }

    /// <summary>Whether this holds a number at all.</summary>
    public bool IsSet => !string.IsNullOrEmpty(Value);

    /// <summary>The matching VAT number: the same digits with the country code in front.</summary>
    public string VatNumber => IsSet ? "EL" + Value : string.Empty;

    /// <summary>Reads an AFM, with or without its <c>EL</c> or <c>GR</c> prefix.</summary>
    public static bool TryParse(string? value, out GrTaxIdentifier identifier)
    {
        identifier = default;

        if (value is null)
        {
            return false;
        }

        string compact = CheckDigit.Compact(value).ToUpperInvariant();

        if (compact.StartsWith("EL", StringComparison.Ordinal) || compact.StartsWith("GR", StringComparison.Ordinal))
        {
            compact = compact[2..];
        }

        if (compact.Length != 9 || !compact.All(char.IsAsciiDigit) || !Satisfies(compact))
        {
            return false;
        }

        identifier = new GrTaxIdentifier(compact);
        return true;
    }

    /// <summary>Reads an AFM, or throws.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
    /// <exception cref="FormatException">The value is not an AFM.</exception>
    public static GrTaxIdentifier Parse(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return TryParse(value, out GrTaxIdentifier identifier)
            ? identifier
            : throw new FormatException(
                $"'{value}' is not a Greek AFM: nine digits whose last satisfies the weighted checksum "
                + "Peppol enforces on scheme 9933.");
    }

    /// <summary>Whether a value is an AFM.</summary>
    public static bool IsValid(string? value) => TryParse(value, out _);

    /// <summary>The number as an identifier field, in the scheme Peppol reserves for it.</summary>
    public IdentifierField ToField() => new(Value, Scheme);

    /// <inheritdoc />
    public override string ToString() => Value ?? string.Empty;

    private static bool Satisfies(string digits)
    {
        int sum = 0;
        int weight = 256;

        for (int index = 0; index < 8; index++)
        {
            sum += (digits[index] - '0') * weight;
            weight /= 2;
        }

        return sum % 11 % 10 == digits[8] - '0';
    }
}
