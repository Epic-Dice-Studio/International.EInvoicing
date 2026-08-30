using International.EInvoicing.Identifiers;
using International.EInvoicing.Values;

namespace International.EInvoicing.Countries.Denmark.Identifiers;

/// <summary>
/// A Danish CVR number — the eight digits the Central Business Register assigns to a business.
/// </summary>
/// <remarks>
/// <para>
/// This checks what Peppol checks on scheme 0184 (<c>PEPPOL-COMMON-R042</c>): eight digits, optionally
/// prefixed <c>DK</c>. It deliberately stops there. A CVR number does carry a modulo 11 check digit, but the
/// network does not enforce it, and rejecting a number the receiving access point would have accepted is a
/// worse failure than accepting a typo — the library is meant to be an aid, not a second gate.
/// </para>
/// <para>
/// The SE number (scheme 0198) is the same eight digits with <c>DK</c> in front, which is why both are read
/// here rather than modelled twice.
/// </para>
/// </remarks>
public readonly record struct DkCvrNumber
{
    /// <summary>The scheme identifier a Danish CVR number is declared under on an invoice.</summary>
    public const string Scheme = "0184";

    /// <summary>The scheme identifier the SE number is declared under.</summary>
    public const string SeNumberScheme = "0198";

    private DkCvrNumber(string value) => Value = value;

    /// <summary>The eight digits, without the country prefix.</summary>
    public string Value { get; }

    /// <summary>Whether this holds a number at all.</summary>
    public bool IsSet => !string.IsNullOrEmpty(Value);

    /// <summary>The matching VAT number, and the SE number: the same digits with DK in front.</summary>
    public string VatNumber => IsSet ? "DK" + Value : string.Empty;

    /// <summary>Reads a CVR or SE number, with or without its country prefix.</summary>
    public static bool TryParse(string? value, out DkCvrNumber number)
    {
        number = default;

        if (value is null)
        {
            return false;
        }

        string compact = CheckDigit.Compact(value).ToUpperInvariant();

        if (compact.StartsWith("DK", StringComparison.Ordinal))
        {
            compact = compact[2..];
        }

        if (compact.Length != 8 || !compact.All(char.IsAsciiDigit))
        {
            return false;
        }

        number = new DkCvrNumber(compact);
        return true;
    }

    /// <summary>Reads a CVR number, or throws.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
    /// <exception cref="FormatException">The value is not a Danish CVR number.</exception>
    public static DkCvrNumber Parse(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return TryParse(value, out DkCvrNumber number)
            ? number
            : throw new FormatException(
                $"'{value}' is not a Danish CVR number: eight digits, optionally prefixed DK, as Peppol "
                + "enforces on scheme 0184.");
    }

    /// <summary>Whether a value is a Danish CVR number.</summary>
    public static bool IsValid(string? value) => TryParse(value, out _);

    /// <summary>The number as an identifier field, in the scheme Peppol reserves for it.</summary>
    public IdentifierField ToField() => new(Value, Scheme);

    /// <summary>The SE number, in its own scheme.</summary>
    public IdentifierField ToSeNumberField() => new(VatNumber, SeNumberScheme);

    /// <inheritdoc />
    public override string ToString() => Value ?? string.Empty;
}
