using International.EInvoicing.Identifiers;
using International.EInvoicing.Values;

namespace International.EInvoicing.Countries.Australia.Identifiers;

/// <summary>
/// An Australian Business Number — the eleven digits the Australian Business Register assigns.
/// </summary>
/// <remarks>
/// The check is the one Peppol enforces on scheme 0151 (<c>PEPPOL-COMMON-R050</c>): subtract one from the
/// first digit, weight the eleven digits by 10, 1, 3, 5, 7, 9, 11, 13, 15, 17, 19, and the sum is divisible
/// by 89. It is not a trailing check digit — every digit participates, which is why a transposition anywhere
/// in the number is caught.
/// </remarks>
public readonly record struct AuAbn
{
    /// <summary>The scheme identifier an ABN is declared under on an invoice.</summary>
    public const string Scheme = "0151";

    private static readonly int[] Weights = [10, 1, 3, 5, 7, 9, 11, 13, 15, 17, 19];

    private AuAbn(string value) => Value = value;

    /// <summary>The eleven digits, without spaces.</summary>
    public string Value { get; }

    /// <summary>Whether this holds a number at all.</summary>
    public bool IsSet => !string.IsNullOrEmpty(Value);

    /// <summary>Reads an ABN, however it is spaced.</summary>
    public static bool TryParse(string? value, out AuAbn abn)
    {
        abn = default;

        if (value is null)
        {
            return false;
        }

        string compact = CheckDigit.Compact(value);

        if (compact.Length != 11 || !compact.All(char.IsAsciiDigit) || !Satisfies(compact))
        {
            return false;
        }

        abn = new AuAbn(compact);
        return true;
    }

    /// <summary>Reads an ABN, or throws.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
    /// <exception cref="FormatException">The value is not an ABN.</exception>
    public static AuAbn Parse(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return TryParse(value, out AuAbn abn)
            ? abn
            : throw new FormatException(
                $"'{value}' is not an Australian Business Number: eleven digits whose weighted sum, with one "
                + "subtracted from the first, is divisible by 89.");
    }

    /// <summary>Whether a value is an ABN.</summary>
    public static bool IsValid(string? value) => TryParse(value, out _);

    /// <summary>The number as an identifier field, in the scheme Peppol reserves for it.</summary>
    public IdentifierField ToField() => new(Value, Scheme);

    /// <summary>The number as Australians write it: two digits, then three groups of three.</summary>
    public string ToFormattedString() =>
        IsSet ? $"{Value[..2]} {Value[2..5]} {Value[5..8]} {Value[8..]}" : string.Empty;

    /// <inheritdoc />
    public override string ToString() => Value ?? string.Empty;

    private static bool Satisfies(string digits)
    {
        int sum = (digits[0] - '0' - 1) * Weights[0];

        for (int index = 1; index < digits.Length; index++)
        {
            sum += (digits[index] - '0') * Weights[index];
        }

        return sum % 89 == 0;
    }
}
