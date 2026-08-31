using International.EInvoicing.Identifiers;
using International.EInvoicing.Values;

namespace International.EInvoicing.Countries.Italy.Identifiers;

/// <summary>
/// An Italian VAT number — the <em>partita IVA</em> — eleven digits.
/// </summary>
/// <remarks>
/// The check is the one Peppol enforces on scheme <c>0211</c> (<c>PEPPOL-COMMON-R047</c>): digits in odd
/// positions count as themselves, digits in even positions are mapped through <c>0246813579</c> — which is
/// the doubled digit with its own digits summed — and the total must be divisible by ten. It is Luhn wearing
/// Italian clothes.
/// </remarks>
public readonly record struct ItPartitaIva
{
    /// <summary>The scheme identifier an Italian VAT number is declared under on an invoice.</summary>
    public const string Scheme = "0211";

    private const string EvenPositionMap = "0246813579";

    private ItPartitaIva(string value) => Value = value;

    /// <summary>The eleven digits, without the country prefix.</summary>
    public string Value { get; }

    /// <summary>Whether this holds a number at all.</summary>
    public bool IsSet => !string.IsNullOrEmpty(Value);

    /// <summary>The VAT number as written on an invoice: the digits with <c>IT</c> in front.</summary>
    public string VatNumber => IsSet ? "IT" + Value : string.Empty;

    /// <summary>Reads a partita IVA, with or without its country prefix.</summary>
    public static bool TryParse(string? value, out ItPartitaIva partitaIva)
    {
        partitaIva = default;

        if (value is null)
        {
            return false;
        }

        string compact = CheckDigit.Compact(value).ToUpperInvariant();

        if (compact.StartsWith("IT", StringComparison.Ordinal))
        {
            compact = compact[2..];
        }

        if (compact.Length != 11 || !compact.All(char.IsAsciiDigit) || !Satisfies(compact))
        {
            return false;
        }

        partitaIva = new ItPartitaIva(compact);
        return true;
    }

    /// <summary>Reads a partita IVA, or throws.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
    /// <exception cref="FormatException">The value is not a partita IVA.</exception>
    public static ItPartitaIva Parse(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return TryParse(value, out ItPartitaIva partitaIva)
            ? partitaIva
            : throw new FormatException(
                $"'{value}' is not an Italian partita IVA: eleven digits satisfying the check Peppol "
                + "enforces on scheme 0211.");
    }

    /// <summary>Whether a value is a partita IVA.</summary>
    public static bool IsValid(string? value) => TryParse(value, out _);

    /// <summary>The number as an identifier field, in the scheme Peppol reserves for it.</summary>
    public IdentifierField ToField() => new(Value, Scheme);

    /// <inheritdoc />
    public override string ToString() => Value ?? string.Empty;

    private static bool Satisfies(string digits)
    {
        int sum = 0;

        for (int index = 0; index < digits.Length; index++)
        {
            int digit = digits[index] - '0';

            // Positions are one-based in the rule: the first digit counts as itself.
            sum += index % 2 == 0 ? digit : EvenPositionMap[digit] - '0';
        }

        return sum % 10 == 0;
    }
}
