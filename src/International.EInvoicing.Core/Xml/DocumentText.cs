using System.Text;
using International.EInvoicing.Diagnostics;

namespace International.EInvoicing.Xml;

/// <summary>Bytes turned into text, and what that cost.</summary>
/// <param name="Text">The document as text.</param>
/// <param name="Encoding">The encoding actually used to decode it.</param>
/// <param name="Diagnostic">What went wrong, when the declaration and the bytes disagreed.</param>
public sealed record DecodedDocument(string Text, Encoding Encoding, Diagnostic? Diagnostic);

/// <summary>
/// Turning the bytes of a document into text, honouring what the document says about itself.
/// </summary>
/// <remarks>
/// <para>
/// The single most discussed issue in the German validator's tracker, and it is not exotic: a sender declares
/// <c>encoding="UTF-8"</c> and transmits Latin-1, or declares Latin-1 and transmits UTF-8. Decoding
/// everything as UTF-8 turns <c>Müller</c> into <c>M?ller</c> or <c>MÃ¼ller</c> — a document that validates,
/// arrives, and is wrong in the one field a human reads.
/// </para>
/// <para>
/// So the declaration is honoured, the bytes are checked against it, and a disagreement is reported rather
/// than papered over. A document is still produced: losing an invoice over a mis-declared encoding would be
/// the worse outcome, and the diagnostic says exactly what was assumed.
/// </para>
/// </remarks>
public static class DocumentText
{
    private const int DeclarationSearchBytes = 256;

    /// <summary>
    /// Decodes a document, using its byte-order mark, then its XML declaration, then UTF-8.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="content"/> is <c>null</c>.</exception>
    public static DecodedDocument Decode(byte[] content)
    {
        ArgumentNullException.ThrowIfNull(content);

        string? declared = DeclaredEncoding(content);

        if (ByteOrderMark(content) is { } marked)
        {
            // A byte-order mark outranks the declaration: it is what an XML processor must obey.
            return Settle(marked.GetString(content), marked, declared, null);
        }

        if (declared is null)
        {
            return Utf8OrFallback(content, declaredName: null);
        }

        if (TryResolve(declared) is not { } encoding)
        {
            DecodedDocument fallback = Utf8OrFallback(content, declared);

            return fallback with
            {
                Diagnostic = Diagnostic.Create(DiagnosticCodes.UnsupportedEncoding, declared) with
                {
                    Found = declared,
                    AppliedFallback = "decoded as UTF-8, or as ISO-8859-1 when the bytes are not valid UTF-8",
                },
            };
        }

        if (encoding.CodePage == Encoding.UTF8.CodePage)
        {
            return Utf8OrFallback(content, declared);
        }

        return Settle(encoding.GetString(content), encoding, declared, null);
    }

    /// <summary>
    /// Hands back decoded text whose declaration no longer contradicts it.
    /// </summary>
    /// <remarks>
    /// Once the bytes are text, the <c>encoding</c> pseudo-attribute describes something that no longer
    /// exists — and a parser reading the text will refuse a name it does not know, losing a document this
    /// library already decoded successfully. So when the encoding used is not the one declared, the
    /// pseudo-attribute is dropped. XML allows a declaration without it, and saying nothing is the only
    /// truthful option: the text is not in the declared encoding, and it is not in UTF-8 either.
    /// </remarks>
    private static DecodedDocument Settle(string text, Encoding used, string? declared, Diagnostic? diagnostic)
    {
        bool contradicts = declared is not null && TryResolve(declared)?.CodePage != used.CodePage;

        return new DecodedDocument(contradicts ? WithoutEncodingDeclaration(text) : text, used, diagnostic);
    }

    /// <summary>Removes the <c>encoding</c> pseudo-attribute from the XML declaration, if there is one.</summary>
    private static string WithoutEncodingDeclaration(string text)
    {
        int declaration = text.IndexOf("<?xml", StringComparison.Ordinal);

        if (declaration < 0)
        {
            return text;
        }

        int end = text.IndexOf("?>", declaration, StringComparison.Ordinal);

        if (end < 0)
        {
            return text;
        }

        string prologue = text[declaration..end];
        int marker = prologue.IndexOf("encoding", StringComparison.Ordinal);

        if (marker < 0)
        {
            return text;
        }

        int open = prologue.IndexOfAny(['"', '\''], marker);
        int close = open < 0 ? -1 : prologue.IndexOf(prologue[open], open + 1);

        return close < 0 ? text : text[..(declaration + marker)] + text[(declaration + close + 1)..];
    }

    /// <summary>
    /// Decodes as UTF-8, and says so when the bytes are not valid UTF-8.
    /// </summary>
    /// <remarks>
    /// ISO-8859-1 is the fallback because every byte sequence is valid in it, so the document is always
    /// produced, and because it is what the senders who get this wrong are almost always sending.
    /// </remarks>
    private static DecodedDocument Utf8OrFallback(byte[] content, string? declaredName)
    {
        var strict = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

        try
        {
            return Settle(strict.GetString(content), Encoding.UTF8, declaredName, null);
        }
        catch (DecoderFallbackException invalid)
        {
            return Settle(
                Latin1.GetString(content),
                Latin1,
                declaredName,
                Diagnostic.Create(
                    DiagnosticCodes.DeclaredEncodingMismatch,
                    declaredName ?? "UTF-8",
                    invalid.Index) with
                {
                    Expected = declaredName ?? "UTF-8",
                    Found = "bytes that are not valid UTF-8",
                    AppliedFallback = "decoded as ISO-8859-1, in which every byte sequence is valid",
                });
        }
    }

    private static Encoding Latin1 { get; } = Encoding.GetEncoding(28591);

    private static Encoding? ByteOrderMark(byte[] content)
    {
        if (content.Length >= 3 && content[0] == 0xEF && content[1] == 0xBB && content[2] == 0xBF)
        {
            return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        }

        if (content.Length >= 2 && content[0] == 0xFF && content[1] == 0xFE)
        {
            return Encoding.Unicode;
        }

        return content.Length >= 2 && content[0] == 0xFE && content[1] == 0xFF ? Encoding.BigEndianUnicode : null;
    }

    /// <summary>
    /// The <c>encoding</c> pseudo-attribute of the XML declaration, when there is one.
    /// </summary>
    /// <remarks>
    /// Scanned out of the leading bytes as ASCII, which is safe: the declaration itself must be ASCII in
    /// every encoding an XML processor is required to accept, and that is exactly why it is allowed to name
    /// the encoding of what follows.
    /// </remarks>
    private static string? DeclaredEncoding(byte[] content)
    {
        string head = Encoding.ASCII.GetString(content, 0, Math.Min(content.Length, DeclarationSearchBytes));

        int declaration = head.IndexOf("<?xml", StringComparison.Ordinal);

        if (declaration < 0)
        {
            return null;
        }

        int end = head.IndexOf("?>", declaration, StringComparison.Ordinal);
        string prologue = end < 0 ? head[declaration..] : head[declaration..end];

        int marker = prologue.IndexOf("encoding", StringComparison.Ordinal);

        if (marker < 0)
        {
            return null;
        }

        int open = prologue.IndexOfAny(['"', '\''], marker);

        if (open < 0)
        {
            return null;
        }

        int close = prologue.IndexOf(prologue[open], open + 1);

        return close < 0 ? null : prologue[(open + 1)..close].Trim();
    }

    /// <summary>
    /// The encodings this library decodes.
    /// </summary>
    /// <remarks>
    /// Deliberately short. Anything beyond these needs <c>System.Text.Encoding.CodePages</c>, which is a
    /// dependency, and a document in Windows-1252 or Shift-JIS is rare enough that saying "I decoded this as
    /// UTF-8 and here is why that may be wrong" beats carrying the package for everyone.
    /// </remarks>
    private static Encoding? TryResolve(string name) => name.ToUpperInvariant() switch
    {
        "UTF-8" or "UTF8" => Encoding.UTF8,
        "UTF-16" or "UTF16" or "UCS-2" => Encoding.Unicode,
        "ISO-8859-1" or "ISO8859-1" or "LATIN1" or "LATIN-1" or "L1" => Latin1,
        "US-ASCII" or "ASCII" => Encoding.ASCII,
        _ => null,
    };
}
