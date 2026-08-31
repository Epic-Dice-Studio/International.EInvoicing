using System.Xml;
using System.Xml.Linq;

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
            // Not Async: nothing here calls XmlReader.ReadAsync. Parsing is work, not waiting, and the
            // asynchronous boundary is the transfer — see docs/adr/0012-async-at-the-boundary.md.
            CloseInput = false,
        };
    }

    /// <summary>Creates a hardened reader over <paramref name="stream"/>, which is left open.</summary>
    /// <remarks>
    /// Spelled out rather than given an optional parameter: adding one to a published overload changes what
    /// already-compiled callers bind to, which is a break nobody sees at build time.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <c>null</c>.</exception>
    public static XmlReader CreateReader(Stream stream) => CreateReader(stream, DocumentLimits.Default);

    /// <summary>Creates a hardened reader over <paramref name="stream"/>, which is left open.</summary>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    public static XmlReader CreateReader(Stream stream, DocumentLimits limits)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(limits);

        return XmlReader.Create(stream, CreateReaderSettings(limits));
    }

    /// <summary>Creates a hardened reader over <paramref name="xml"/>.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="xml"/> is <c>null</c>.</exception>
    public static XmlReader CreateReader(string xml) => CreateReader(xml, DocumentLimits.Default);

    /// <summary>Creates a hardened reader over <paramref name="xml"/>.</summary>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    public static XmlReader CreateReader(string xml, DocumentLimits limits)
    {
        ArgumentNullException.ThrowIfNull(xml);
        ArgumentNullException.ThrowIfNull(limits);

        return XmlReader.Create(new StringReader(xml), CreateReaderSettings(limits));
    }

    /// <summary>
    /// Refuses a document that nests deeper than <see cref="DocumentLimits.MaxElementDepth"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The limit is not about memory — a deeply nested document is small. It is about what happens next:
    /// evaluating an XPath expression over it, writing it back, walking it in a rule. Those recurse, and a
    /// document nested ten thousand deep exhausts the stack, which no <c>catch</c> can recover from.
    /// </para>
    /// <para>
    /// Checked after loading rather than during, because LINQ-to-XML builds the tree iteratively and so
    /// survives the parse; it is every consumer afterwards that must be protected. Reported as
    /// <c>XmlException</c> so it joins the malformed-document path a reader already has, and reaches the
    /// caller as EIV5001 rather than as a crash.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    /// <exception cref="XmlException">The document nests deeper than the limit allows.</exception>
    public static void EnsureDepthWithin(XElement root, DocumentLimits limits)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(limits);

        if (limits.MaxElementDepth <= 0)
        {
            return;
        }

        Stack<(XElement Element, int Depth)> pending = new();
        pending.Push((root, 1));

        while (pending.Count > 0)
        {
            (XElement element, int depth) = pending.Pop();

            if (depth > limits.MaxElementDepth)
            {
                throw new XmlException(
                    $"The document nests deeper than {limits.MaxElementDepth} elements, the limit in "
                    + "DocumentLimits.MaxElementDepth. Raise it only if you trust the source.");
            }

            foreach (XElement child in element.Elements())
            {
                pending.Push((child, depth + 1));
            }
        }
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
