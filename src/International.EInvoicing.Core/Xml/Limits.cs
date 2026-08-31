using International.EInvoicing.Diagnostics;

namespace International.EInvoicing.Xml;

/// <summary>
/// Keeping the promises <see cref="DocumentLimits"/> makes.
/// </summary>
/// <remarks>
/// A limit that is declared and not enforced is worse than no limit: it is documented reassurance a reader
/// relies on. These are the checks the readers call, in one place so every syntax enforces them the same way
/// and reports them with the same code.
/// </remarks>
public static class Limits
{
    /// <summary>Whether a count has reached a limit. A limit of zero or less means no limit.</summary>
    public static bool Exceeded(int soFar, int limit) => limit > 0 && soFar >= limit;

    /// <summary>The diagnostic for a document carrying more of something than the limits allow.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="what"/> is <c>null</c>.</exception>
    public static Diagnostic TooMany(int limit, string what)
    {
        ArgumentNullException.ThrowIfNull(what);

        return Diagnostic.Create(DiagnosticCodes.TooMany, limit, what) with
        {
            Found = $"more than {limit} {what}",
            AppliedFallback = $"the first {limit} were read; the rest were not",
        };
    }

    /// <summary>
    /// Decodes a base64 payload, or refuses it and says why.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The size is judged <em>before</em> decoding, from the length of the text: three bytes for every four
    /// characters. Decoding first and measuring after is how a document with a hundred-megabyte attachment
    /// takes the process down, and no <c>catch</c> recovers from that.
    /// </para>
    /// <para>
    /// A payload that is refused is not lost: the field keeps its raw base64 text, exactly as every other
    /// value that could not be typed does.
    /// </para>
    /// </remarks>
    /// <param name="base64">The payload as it appeared in the document.</param>
    /// <param name="limits">The limits in force.</param>
    /// <param name="diagnostics">Where to report a refusal.</param>
    /// <returns>The decoded bytes, or <c>null</c> when the payload was refused or is not valid base64.</returns>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    public static byte[]? Decode(string base64, DocumentLimits limits, DiagnosticCollector diagnostics)
    {
        ArgumentNullException.ThrowIfNull(base64);
        ArgumentNullException.ThrowIfNull(limits);
        ArgumentNullException.ThrowIfNull(diagnostics);

        long decodedBytes = base64.Length / 4L * 3L;

        if (limits.MaxAttachmentBytes > 0 && decodedBytes > limits.MaxAttachmentBytes)
        {
            diagnostics.Add(
                Diagnostic.Create(DiagnosticCodes.AttachmentTooLarge, decodedBytes, limits.MaxAttachmentBytes) with
                {
                    BusinessTerm = "BT-125",
                    Found = $"about {decodedBytes} bytes",
                    AppliedFallback = "the base64 text is kept; nothing was decoded",
                });

            return null;
        }

        byte[] buffer = new byte[decodedBytes + 3];

        return Convert.TryFromBase64String(base64, buffer, out int written) ? buffer[..written] : null;
    }
}
