using International.EInvoicing.Identifiers;
using International.EInvoicing.Values;

namespace International.EInvoicing.Countries.Iceland.Identifiers;

/// <summary>
/// An Icelandic kennitala — the ten digits Iceland assigns to a person or a business.
/// </summary>
/// <remarks>
/// <para>
/// <c>IS-R-002</c> and <c>IS-R-004</c> are fatal: both parties' legal entity identifiers must be present and
/// carry scheme <c>0196</c>. That is what this type is for — putting the number where the rule looks for it.
/// </para>
/// <para>
/// The modulo 11 check a kennitala carries is verified here, since it is the number's own definition, but the
/// date part is not interpreted: kennitölur issued to businesses add 40 to the day, and a library has no
/// business deciding which of those two a caller meant.
/// </para>
/// </remarks>
public readonly record struct IsKennitala
{
    /// <summary>The scheme identifier a kennitala is declared under on an invoice.</summary>
    public const string Scheme = "0196";

    private static readonly int[] Weights = [3, 2, 7, 6, 5, 4, 3, 2];

    private IsKennitala(string value) => Value = value;

    /// <summary>The ten digits, without the hyphen.</summary>
    public string Value { get; }

    /// <summary>Whether this holds a number at all.</summary>
    public bool IsSet => !string.IsNullOrEmpty(Value);

    /// <summary>Reads a kennitala, with or without its hyphen and country prefix.</summary>
    public static bool TryParse(string? value, out IsKennitala kennitala)
    {
        kennitala = default;

        if (value is null)
        {
            return false;
        }

        string compact = CheckDigit.Compact(value).ToUpperInvariant();

        if (compact.StartsWith("IS", StringComparison.Ordinal))
        {
            compact = compact[2..];
        }

        // The tenth digit is the century marker, not part of the check.
        if (compact.Length != 10 || !compact.All(char.IsAsciiDigit)
            || !CheckDigit.SatisfiesMod11(compact.AsSpan(0, 9), Weights))
        {
            return false;
        }

        kennitala = new IsKennitala(compact);
        return true;
    }

    /// <summary>Reads a kennitala, or throws.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
    /// <exception cref="FormatException">The value is not a kennitala.</exception>
    public static IsKennitala Parse(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return TryParse(value, out IsKennitala kennitala)
            ? kennitala
            : throw new FormatException(
                $"'{value}' is not an Icelandic kennitala: ten digits whose ninth satisfies the modulo 11 "
                + "check.");
    }

    /// <summary>Whether a value is a kennitala.</summary>
    public static bool IsValid(string? value) => TryParse(value, out _);

    /// <summary>The number as an identifier field, in the scheme the Icelandic rules require.</summary>
    public IdentifierField ToField() => new(Value, Scheme);

    /// <summary>The number as Icelanders write it, with a hyphen before the last four digits.</summary>
    public string ToFormattedString() => IsSet ? $"{Value[..6]}-{Value[6..]}" : string.Empty;

    /// <inheritdoc />
    public override string ToString() => Value ?? string.Empty;
}
