using System.Xml;
using System.Xml.Linq;
using International.EInvoicing.Model;

namespace International.EInvoicing.Xml;

/// <summary>
/// An <see cref="XmlWriter"/> that puts unmapped content back where it was read from.
/// </summary>
/// <remarks>
/// <para>
/// Keeping what nobody mapped is only half of not losing it. Element order is normative in UBL, in CII and
/// in CDAR, so an extension written at the end of its node is one a receiver's parser rejects. Each
/// <see cref="ExtensionElement"/> remembers the mapped sibling it followed; <see cref="Node"/> opens a scope
/// for a model node, and each extension goes out as soon as the sibling it followed has been written.
/// </para>
/// <para>
/// The members mirror the <see cref="XmlWriter"/> ones a writer already calls, so a writer moves onto this
/// by changing the type in its signatures and nothing else.
/// </para>
/// <para>
/// One rule about ordering: <see cref="Node"/> writes content, and content closes the current element's
/// attribute list. Call it after the attributes of the element it belongs to, never between them.
/// </para>
/// <para>
/// UBL has its own equivalent — <c>UblDocument</c> — which reaches the same rule a different way: every UBL
/// model node is exactly one element, so "same parent" is "a direct child of the node's element", which it
/// checks structurally rather than by name. This one carries the parent explicitly because a CII model node
/// need not be one element: an invoice fills itself from the document context, the exchanged document and
/// three header sections at once.
/// </para>
/// </remarks>
public sealed class AnchoredDocument(XmlWriter writer) : IDisposable
{
    private readonly Stack<List<ExtensionElement>> _pending = new();
    private readonly Stack<int> _scopes = new();
    private readonly Stack<XName> _open = new();
    private int _depth;

    /// <summary>The writer underneath, for the rare thing this wrapper does not forward.</summary>
    public XmlWriter Writer => writer;

    /// <summary>
    /// Declares that the element now open is a model node, so its extensions are flushed among its children
    /// rather than at the end of the document.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="extensions"/> is <c>null</c>.</exception>
    public void Node(IEnumerable<ExtensionElement> extensions)
    {
        ArgumentNullException.ThrowIfNull(extensions);

        List<ExtensionElement> pending = [.. extensions.Where(element => !string.IsNullOrEmpty(element.Xml))];

        _scopes.Push(_depth);
        _pending.Push(pending);

        // Anything that came first inside the element now open goes out first.
        Flush(pending, _open.Count > 0 ? _open.Peek() : null, preceding: null);
    }

    /// <inheritdoc cref="XmlWriter.WriteStartDocument()" />
    public void WriteStartDocument() => writer.WriteStartDocument();

    /// <inheritdoc cref="XmlWriter.WriteEndDocument()" />
    public void WriteEndDocument() => writer.WriteEndDocument();

    /// <inheritdoc cref="XmlWriter.WriteStartElement(string, string, string)" />
    public void WriteStartElement(string? prefix, string localName, string? ns)
    {
        writer.WriteStartElement(prefix, localName, ns);
        _open.Push(XNamespace.Get(ns ?? string.Empty) + localName);
        _depth++;
    }

    /// <inheritdoc cref="XmlWriter.WriteStartElement(string)" />
    public void WriteStartElement(string localName) => WriteStartElement(null, localName, null);

    /// <summary>
    /// Closes the element, flushing first whatever was anchored inside it and then whatever was anchored to
    /// it.
    /// </summary>
    public void WriteEndElement()
    {
        XName? closing = _open.Count > 0 ? _open.Peek() : null;

        // Anything addressed to this element that followed a sibling it never wrote still belongs inside it,
        // and the end of the element is the closest place left.
        if (closing is not null && _scopes.Count > 0 && _depth > _scopes.Peek())
        {
            Flush(_pending.Peek(), closing, preceding: null, any: true);
        }

        if (_scopes.Count > 0 && _scopes.Peek() == _depth)
        {
            _scopes.Pop();

            // Whatever is left was addressed to an element this node never wrote at all.
            foreach (ExtensionElement element in _pending.Pop())
            {
                writer.WriteRaw(element.Xml);
            }
        }

        writer.WriteEndElement();
        _depth--;

        if (_open.Count > 0)
        {
            Placed(_open.Pop());
        }
    }

    /// <inheritdoc cref="XmlWriter.WriteString(string)" />
    public void WriteString(string? text) => writer.WriteString(text);

    /// <inheritdoc cref="XmlWriter.WriteRaw(string)" />
    public void WriteRaw(string data) => writer.WriteRaw(data);

    /// <inheritdoc cref="XmlWriter.WriteAttributeString(string, string)" />
    public void WriteAttributeString(string localName, string? value) =>
        writer.WriteAttributeString(localName, value);

    /// <inheritdoc cref="XmlWriter.WriteAttributeString(string, string, string, string)" />
    public void WriteAttributeString(string? prefix, string localName, string? ns, string? value) =>
        writer.WriteAttributeString(prefix, localName, ns, value);

    /// <inheritdoc cref="XmlWriter.WriteElementString(string, string, string, string)" />
    public void WriteElementString(string? prefix, string localName, string? ns, string? value)
    {
        writer.WriteElementString(prefix, localName, ns, value);
        Placed(XNamespace.Get(ns ?? string.Empty) + localName);
    }

    /// <inheritdoc cref="XmlWriter.Flush()" />
    public void Flush() => writer.Flush();

    /// <summary>Flushes the writer. The underlying <see cref="XmlWriter"/> is left to its own owner.</summary>
    public void Dispose() => writer.Flush();

    /// <summary>
    /// Flushes the extensions anchored to the element just written, when the node they belong to is the one
    /// currently open.
    /// </summary>
    /// <remarks>
    /// The anchor is the reader's own name for the sibling — namespace and local name — so it survives a
    /// document written with different prefixes than the one it was read from.
    /// </remarks>
    private void Placed(XName name)
    {
        // At or below the node's own depth: a model node may span several elements, and an extension inside
        // any of them belongs to it.
        if (_scopes.Count > 0 && _depth >= _scopes.Peek())
        {
            Flush(_pending.Peek(), _open.Count > 0 ? _open.Peek() : null, name.ToString());
        }
    }

    /// <summary>
    /// Writes out every pending extension addressed to <paramref name="parent"/> that followed
    /// <paramref name="preceding"/>, and forgets them.
    /// </summary>
    /// <param name="pending">What is still waiting for its place.</param>
    /// <param name="parent">The element now open, which is where these belong.</param>
    /// <param name="preceding">The sibling just written, or <c>null</c> for the start of the element.</param>
    /// <param name="any">
    /// Take everything addressed to <paramref name="parent"/> whatever it followed. Used when the element is
    /// about to close and this is the last chance to keep its content inside it.
    /// </param>
    private void Flush(List<ExtensionElement> pending, XName? parent, string? preceding, bool any = false)
    {
        string? parentName = parent?.ToString();

        for (var index = 0; index < pending.Count;)
        {
            ExtensionElement element = pending[index];

            // A reader that recorded no parent gets the older behaviour: the sibling alone decides.
            bool addressed = element.ParentName is null || element.ParentName == parentName;

            if (addressed && (any || element.PrecedingSibling == preceding))
            {
                writer.WriteRaw(element.Xml);
                pending.RemoveAt(index);
                continue;
            }

            index++;
        }
    }
}
