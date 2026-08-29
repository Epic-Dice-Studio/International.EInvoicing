using System.Xml;

namespace International.EInvoicing.Xml;

/// <summary>
/// Creates <see cref="XmlReader"/> instances that are safe to point at a document received from a third party:
/// no DTD, no external resolution, bounded entity expansion and a bounded document size.
/// Whitespace and comments are preserved, because readers reproduce the raw text of every field.
/// </summary>
public static class SecureXml
{
    private const long MaxCharactersFromEntities = 1024;

    /// <summary>Creates hardened reader settings using <see cref="DocumentLimits.Default"/>.</summary>
    public static XmlReaderSettings CreateReaderSettings() => CreateReaderSettings(DocumentLimits.Default);

    /// <summary>Creates hardened reader settings bound to <paramref name="limits"/>.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="limits"/> is <c>null</c>.</exception>
    public static XmlReaderSettings CreateReaderSettings(DocumentLimits limits)
    {
        ArgumentNullException.ThrowIfNull(limits);

        return new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersFromEntities = MaxCharactersFromEntities,
            MaxCharactersInDocument = limits.MaxDocumentCharacters,
            IgnoreWhitespace = false,
            IgnoreComments = false,
            IgnoreProcessingInstructions = false,
            ValidationType = ValidationType.None,
            CloseInput = false,
            Async = true,
        };
    }

    /// <summary>Creates a hardened reader over <paramref name="stream"/>, which is left open.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <c>null</c>.</exception>
    public static XmlReader CreateReader(Stream stream, DocumentLimits? limits = null)
    {
        ArgumentNullException.ThrowIfNull(stream);
        return XmlReader.Create(stream, CreateReaderSettings(limits ?? DocumentLimits.Default));
    }

    /// <summary>Creates a hardened reader over <paramref name="xml"/>.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="xml"/> is <c>null</c>.</exception>
    public static XmlReader CreateReader(string xml, DocumentLimits? limits = null)
    {
        ArgumentNullException.ThrowIfNull(xml);
        return XmlReader.Create(new StringReader(xml), CreateReaderSettings(limits ?? DocumentLimits.Default));
    }

    /// <summary>
    /// Indicates whether the reader has descended past <see cref="DocumentLimits.MaxElementDepth"/>.
    /// Depth cannot be bounded through <see cref="XmlReaderSettings"/>, so readers check it as they descend.
    /// </summary>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    public static bool IsDepthExceeded(XmlReader reader, DocumentLimits limits)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(limits);

        return limits.MaxElementDepth > 0 && reader.Depth > limits.MaxElementDepth;
    }
}
