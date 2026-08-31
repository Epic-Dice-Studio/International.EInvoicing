using International.EInvoicing.Identifiers;
using International.EInvoicing.Values;

namespace International.EInvoicing.Countries.NewZealand.Identifiers;

/// <summary>
/// A New Zealand Business Number — the thirteen digits the Companies Office issues.
/// </summary>
/// <remarks>
/// An NZBN is a GS1 Global Location Number, which is why it is declared under scheme <c>0088</c> rather than
/// one of its own, and why the check is the GS1 one Peppol enforces there (<c>PEPPOL-COMMON-R040</c>):
/// weights alternating 3 and 1 from the digit before the check digit leftwards.
/// </remarks>
public readonly record struct NzNzbn
{
    /// <summary>The scheme identifier an NZBN is declared under — the GLN scheme, since that is what it is.</summary>
    public const string Scheme = "0088";

    private NzNzbn(string value) => Value = value;

    /// <summary>The thirteen digits, without spaces.</summary>
    public string Value { get; }

    /// <summary>Whether this holds a number at all.</summary>
    public bool IsSet => !string.IsNullOrEmpty(Value);

    /// <summary>Reads an NZBN, however it is spaced.</summary>
    public static bool TryParse(string? value, out NzNzbn nzbn)
    {
        nzbn = default;

        if (value is null)
        {
            return false;
        }

        string compact = CheckDigit.Compact(value);

        if (compact.Length != 13 || !compact.All(char.IsAsciiDigit) || !CheckDigit.SatisfiesGs1(compact))
        {
            return false;
        }

        nzbn = new NzNzbn(compact);
        return true;
    }

    /// <summary>Reads an NZBN, or throws.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
    /// <exception cref="FormatException">The value is not an NZBN.</exception>
    public static NzNzbn Parse(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return TryParse(value, out NzNzbn nzbn)
            ? nzbn
            : throw new FormatException(
                $"'{value}' is not a New Zealand Business Number: thirteen digits ending in the GS1 check "
                + "digit of the first twelve.");
    }

    /// <summary>Whether a value is an NZBN.</summary>
    public static bool IsValid(string? value) => TryParse(value, out _);

    /// <summary>The number as an identifier field, in the GLN scheme Peppol routes it by.</summary>
    public IdentifierField ToField() => new(Value, Scheme);

    /// <inheritdoc />
    public override string ToString() => Value ?? string.Empty;
}
