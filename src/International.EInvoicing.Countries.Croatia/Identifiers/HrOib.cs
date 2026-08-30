using International.EInvoicing.Identifiers;
using International.EInvoicing.Values;

namespace International.EInvoicing.Countries.Croatia.Identifiers;

/// <summary>
/// A Croatian OIB — <em>osobni identifikacijski broj</em> — the eleven digits Croatia assigns to a person or
/// a business.
/// </summary>
/// <remarks>
/// Croatia's 2026 mandate requires the OIB of <em>both</em> parties on every invoice, so it is the one thing
/// a Croatian invoice cannot be written without. The eleventh digit is a check digit under ISO/IEC 7064
/// MOD 11,10, and the VAT number is the same digits with <c>HR</c> in front.
/// </remarks>
public readonly record struct HrOib
{
    /// <summary>The scheme identifier an OIB is declared under on an invoice.</summary>
    public const string Scheme = "9934";

    private HrOib(string value) => Value = value;

    /// <summary>The eleven digits.</summary>
    public string Value { get; }

    /// <summary>Whether this holds a number at all.</summary>
    public bool IsSet => !string.IsNullOrEmpty(Value);

    /// <summary>The matching VAT number: the same digits with the country code in front.</summary>
    public string VatNumber => IsSet ? "HR" + Value : string.Empty;

    /// <summary>Reads an OIB, with or without its country prefix.</summary>
    public static bool TryParse(string? value, out HrOib oib)
    {
        oib = default;

        if (value is null)
        {
            return false;
        }

        string compact = CheckDigit.Compact(value).ToUpperInvariant();

        if (compact.StartsWith("HR", StringComparison.Ordinal))
        {
            compact = compact[2..];
        }

        if (compact.Length != 11 || !CheckDigit.SatisfiesIso7064Mod11To10(compact))
        {
            return false;
        }

        oib = new HrOib(compact);
        return true;
    }

    /// <summary>Reads an OIB, or throws.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
    /// <exception cref="FormatException">The value is not an OIB.</exception>
    public static HrOib Parse(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return TryParse(value, out HrOib oib)
            ? oib
            : throw new FormatException(
                $"'{value}' is not a Croatian OIB: eleven digits whose last satisfies ISO/IEC 7064 "
                + "MOD 11,10.");
    }

    /// <summary>Whether a value is an OIB.</summary>
    public static bool IsValid(string? value) => TryParse(value, out _);

    /// <summary>The number as an identifier field, in the scheme Croatian invoices declare it under.</summary>
    public IdentifierField ToField() => new(Value, Scheme);

    /// <inheritdoc />
    public override string ToString() => Value ?? string.Empty;
}
