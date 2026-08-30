using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using International.EInvoicing.Identifiers;
using International.EInvoicing.Values;

namespace International.EInvoicing.Countries.France.Identifiers;

/// <summary>
/// A French intra-community VAT number: <c>FR</c>, a two-character key, then the SIREN.
/// </summary>
/// <remarks>
/// The key is derived from the SIREN, so the two are checked against each other rather than separately. Some
/// numbers use letters in the key; those cannot be recomputed, so only their shape and their SIREN are
/// checked — stated here rather than silently accepted.
/// </remarks>
public readonly record struct FrVatNumber
{
    private FrVatNumber(string value, FrSiren siren, bool keyVerified)
    {
        Value = value;
        Siren = siren;
        IsKeyVerified = keyVerified;
    }

    /// <summary>The number without spaces, starting with <c>FR</c>.</summary>
    public string Value { get; }

    /// <summary>The business the number belongs to.</summary>
    public FrSiren Siren { get; }

    /// <summary>
    /// Whether the key was recomputed from the SIREN. It is <c>false</c> for the older keys containing
    /// letters, where only the shape and the SIREN could be checked.
    /// </summary>
    public bool IsKeyVerified { get; }

    /// <summary>Whether this holds a number at all.</summary>
    public bool IsSet => !string.IsNullOrEmpty(Value);

    /// <summary>Reads a VAT number, accepting the spaces people write it with.</summary>
    public static bool TryParse(string? value, out FrVatNumber number)
    {
        number = default;

        if (value is null)
        {
            return false;
        }

        string compact = CheckDigit.Compact(value).ToUpperInvariant();

        if (compact.Length != 13
            || !compact.StartsWith("FR", StringComparison.Ordinal)
            || !FrSiren.TryParse(compact[4..], out FrSiren siren))
        {
            return false;
        }

        string key = compact[2..4];
        if (!key.All(char.IsAsciiLetterOrDigit))
        {
            return false;
        }

        if (!key.All(char.IsAsciiDigit))
        {
            // A key containing letters cannot be recomputed; the SIREN behind it still can be.
            number = new FrVatNumber(compact, siren, keyVerified: false);
            return true;
        }

        if (!string.Equals(key, KeyFor(siren), StringComparison.Ordinal))
        {
            return false;
        }

        number = new FrVatNumber(compact, siren, keyVerified: true);
        return true;
    }

    /// <summary>Whether the text is a valid French VAT number.</summary>
    public static bool IsValid([NotNullWhen(true)] string? value) => TryParse(value, out _);

    /// <summary>Reads a VAT number, or throws when it is not one.</summary>
    /// <exception cref="FormatException">The value is not a valid French VAT number.</exception>
    public static FrVatNumber Parse(string value) =>
        TryParse(value, out FrVatNumber number)
            ? number
            : throw new FormatException($"'{value}' is not a French VAT number.");

    /// <summary>Builds the VAT number of a business from its SIREN.</summary>
    /// <exception cref="ArgumentException"><paramref name="siren"/> holds nothing.</exception>
    public static FrVatNumber ForSiren(FrSiren siren)
    {
        if (!siren.IsSet)
        {
            throw new ArgumentException("A SIREN is needed to build a VAT number.", nameof(siren));
        }

        return new FrVatNumber("FR" + KeyFor(siren) + siren.Value, siren, keyVerified: true);
    }

    /// <summary>The identifier as an invoice carries it.</summary>
    public IdentifierField ToField() => new(Value, FrIdentifierSchemes.VatNumber);

    /// <inheritdoc />
    public override string ToString() => Value ?? string.Empty;

    /// <summary>The key the tax administration derives from a SIREN.</summary>
    private static string KeyFor(FrSiren siren)
    {
        int remainder = CheckDigit.Modulo97(siren.Value) ?? 0;
        return ((12 + (3 * remainder)) % 97).ToString("D2", CultureInfo.InvariantCulture);
    }
}
