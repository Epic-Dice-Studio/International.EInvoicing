using System.Text;
using System.Xml;

namespace International.EInvoicing.Xml;

/// <summary>The characters a document can carry, and what to do about the ones it cannot.</summary>
public static class XmlCharacters
{
    /// <summary>
    /// The value with the characters XML cannot carry removed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// XML 1.0 forbids most control characters outright — there is no escape for them. A description copied
    /// out of an accounting system with a stray <c>0x07</c> in it would otherwise make writing fail with
    /// "hexadecimal value 0x07, is an invalid character", which tells a caller nothing about which field to
    /// look at and is not something they can reasonably prevent.
    /// </para>
    /// <para>
    /// Nothing meaningful is lost: those characters cannot travel in an invoice at all. Everything else,
    /// accents and emoji included, is left exactly as it was.
    /// </para>
    /// </remarks>
    /// <param name="value">The text to clean. <c>null</c> comes back as <c>null</c>.</param>
    /// <returns>
    /// The same instance when there was nothing to remove, so the common case allocates nothing.
    /// </returns>
    public static string? Sanitize(string? value)
    {
        if (value is null)
        {
            return value;
        }

        StringBuilder? cleaned = null;

        for (int index = 0; index < value.Length; index++)
        {
            char character = value[index];
            bool pair = char.IsHighSurrogate(character)
                && index + 1 < value.Length
                && char.IsLowSurrogate(value[index + 1]);

            // A surrogate on its own is not a character at all, and no writer can carry it.
            bool keep = pair
                ? XmlConvert.IsXmlSurrogatePair(value[index + 1], character)
                : XmlConvert.IsXmlChar(character);

            if (keep)
            {
                cleaned?.Append(character);

                if (pair)
                {
                    cleaned?.Append(value[index + 1]);
                }
            }
            else if (cleaned is null)
            {
                cleaned = new StringBuilder(value.Length).Append(value, 0, index);
            }

            if (pair)
            {
                index++;
            }
        }

        return cleaned?.ToString() ?? value;
    }
}
