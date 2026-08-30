using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using International.EInvoicing.Identifiers;
using International.EInvoicing.Values;

namespace International.EInvoicing.Countries.Belgium.Identifiers;

/// <summary>
/// A Belgian structured communication: the payment reference written <c>+++123/4567/89012+++</c>.
/// </summary>
/// <remarks>
/// Ten free digits followed by two check digits, the remainder modulo 97 with zero written as 97. Belgian
/// receivers reconcile payments on it, so a reference that is merely plausible reconciles against nothing.
/// </remarks>
public readonly record struct BeStructuredCommunication
{
    private BeStructuredCommunication(string digits) => Digits = digits;

    /// <summary>The twelve digits, without separators.</summary>
    public string Digits { get; }

    /// <summary>Whether this holds a reference at all.</summary>
    public bool IsSet => !string.IsNullOrEmpty(Digits);

    /// <summary>Reads a structured communication, with or without its <c>+++</c> and slashes.</summary>
    public static bool TryParse(string? value, out BeStructuredCommunication communication)
    {
        communication = default;

        if (value is null)
        {
            return false;
        }

        string digits = new([.. value.Where(char.IsAsciiDigit)]);

        if (digits.Length != 12 || !Matches(digits[..10], digits[10..]))
        {
            return false;
        }

        communication = new BeStructuredCommunication(digits);
        return true;
    }

    /// <summary>Whether the text is a valid structured communication.</summary>
    public static bool IsValid([NotNullWhen(true)] string? value) => TryParse(value, out _);

    /// <summary>Reads a structured communication, or throws when it is not one.</summary>
    /// <exception cref="FormatException">The value is not a valid structured communication.</exception>
    public static BeStructuredCommunication Parse(string value) =>
        TryParse(value, out BeStructuredCommunication communication)
            ? communication
            : throw new FormatException($"'{value}' is not a Belgian structured communication.");

    /// <summary>
    /// Builds a reference from a number of your own — an invoice number, a customer account — computing the
    /// check digits.
    /// </summary>
    /// <param name="reference">Any value up to ten digits.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="reference"/> does not fit in ten digits.</exception>
    public static BeStructuredCommunication ForInvoice(long reference)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(reference);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(reference, 9_999_999_999L);

        string body = reference.ToString("D10", CultureInfo.InvariantCulture);
        return new BeStructuredCommunication(body + CheckFor(body));
    }

    /// <summary>The reference as it is written on an invoice, <c>+++123/4567/89012+++</c>.</summary>
    public override string ToString() =>
        IsSet ? $"+++{Digits[..3]}/{Digits[3..7]}/{Digits[7..]}+++" : string.Empty;

    /// <summary>The reference as BT-83 carries it.</summary>
    public TextField ToField() => new(ToString());

    private static bool Matches(string body, string check) =>
        string.Equals(CheckFor(body), check, StringComparison.Ordinal);

    /// <summary>The remainder modulo 97, with zero written as 97 so the check is never <c>00</c>.</summary>
    private static string CheckFor(string body)
    {
        int remainder = CheckDigit.Modulo97(body) ?? 0;
        return (remainder == 0 ? 97 : remainder).ToString("D2", CultureInfo.InvariantCulture);
    }
}
