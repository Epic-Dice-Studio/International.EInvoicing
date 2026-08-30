using System.Globalization;

namespace International.EInvoicing.Identifiers;

/// <summary>
/// The check-digit algorithms national identifiers are built on.
/// </summary>
/// <remarks>
/// They live here rather than in a country package because the same handful serves everyone: France checks a
/// SIRET with Luhn, Belgium checks an enterprise number modulo 97, Germany checks a Leitweg-ID with the same
/// ISO 7064 scheme an IBAN uses.
/// </remarks>
public static class CheckDigit
{
    /// <summary>
    /// Whether the digits satisfy the Luhn formula — France's SIREN and SIRET, among many others.
    /// </summary>
    /// <param name="digits">Digits only. Anything else makes the result <c>false</c>.</param>
    public static bool SatisfiesLuhn(ReadOnlySpan<char> digits)
    {
        if (digits.Length == 0)
        {
            return false;
        }

        int sum = 0;
        bool doubling = false;

        for (int index = digits.Length - 1; index >= 0; index--)
        {
            if (!char.IsAsciiDigit(digits[index]))
            {
                return false;
            }

            int value = digits[index] - '0';

            if (doubling)
            {
                value *= 2;
                if (value > 9)
                {
                    value -= 9;
                }
            }

            sum += value;
            doubling = !doubling;
        }

        return sum % 10 == 0;
    }

    /// <summary>
    /// The remainder of a decimal string modulo 97, computed digit by digit so arbitrarily long identifiers
    /// work without overflowing.
    /// </summary>
    /// <param name="digits">Digits only.</param>
    /// <returns>The remainder, or <c>null</c> when the input is not all digits.</returns>
    public static int? Modulo97(ReadOnlySpan<char> digits)
    {
        if (digits.Length == 0)
        {
            return null;
        }

        int remainder = 0;

        foreach (char character in digits)
        {
            if (!char.IsAsciiDigit(character))
            {
                return null;
            }

            remainder = ((remainder * 10) + (character - '0')) % 97;
        }

        return remainder;
    }

    /// <summary>
    /// The two-digit check of ISO/IEC 7064 MOD 97-10, the scheme behind IBANs and the German Leitweg-ID.
    /// Letters count as their position in the alphabet plus nine, so <c>A</c> is 10 and <c>Z</c> is 35.
    /// </summary>
    /// <param name="payload">The identifier without its check digits.</param>
    /// <returns>The check digits, or <c>null</c> when the payload is not alphanumeric.</returns>
    public static string? Iso7064Mod97(ReadOnlySpan<char> payload)
    {
        if (payload.Length == 0)
        {
            return null;
        }

        int remainder = 0;

        foreach (char character in payload)
        {
            int value;

            if (char.IsAsciiDigit(character))
            {
                value = character - '0';
                remainder = ((remainder * 10) + value) % 97;
                continue;
            }

            if (!char.IsAsciiLetter(character))
            {
                return null;
            }

            value = char.ToUpperInvariant(character) - 'A' + 10;
            remainder = ((remainder * 100) + value) % 97;
        }

        // The payload is followed by two zeroes, then the check is 98 minus what remains.
        remainder = remainder * 100 % 97;
        return (98 - remainder).ToString("D2", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Whether the value is a structurally valid IBAN: the first four characters moved to the end, letters
    /// read as their position plus nine, leaves a remainder of one modulo 97.
    /// </summary>
    /// <remarks>
    /// This checks the number, not that the account exists. Published rule sets call out to it, which is why
    /// it lives beside the primitives rather than in a country package.
    /// </remarks>
    public static bool IsIban(string? value)
    {
        if (value is null)
        {
            return false;
        }

        string compact = Compact(value).ToUpperInvariant();

        if (compact.Length is < 5 or > 34 || !char.IsAsciiLetter(compact[0]) || !char.IsAsciiLetter(compact[1]))
        {
            return false;
        }

        string rearranged = compact[4..] + compact[..4];
        int remainder = 0;

        foreach (char character in rearranged)
        {
            if (char.IsAsciiDigit(character))
            {
                remainder = ((remainder * 10) + (character - '0')) % 97;
                continue;
            }

            if (!char.IsAsciiLetter(character))
            {
                return false;
            }

            remainder = ((remainder * 100) + (character - 'A' + 10)) % 97;
        }

        return remainder == 1;
    }

    /// <summary>Keeps only the digits and letters, so a formatted identifier can be checked as written.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
    public static string Compact(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return string.Concat(value.Where(char.IsAsciiLetterOrDigit));
    }
}
