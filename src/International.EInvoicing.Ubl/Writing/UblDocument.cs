using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;
using International.EInvoicing.Values;
using International.EInvoicing.Xml;

namespace International.EInvoicing.Ubl.Writing;

/// <summary>
/// A UBL document being written: the element helpers every UBL writer needs, in one place.
/// </summary>
/// <remarks>
/// A field is written from its raw text when it has one, so a document that passes through unchanged comes
/// out equivalent to the one that went in, and a field that carries only a value is formatted. Element order
/// is not this type's business — it is normative, and each writer states it explicitly.
/// </remarks>
internal sealed class UblDocument : IDisposable
{
    private readonly XmlWriter _writer;
    private readonly bool _ownsDocument;

    /// <summary>The extensions of the node being written, and which of them are still to be placed.</summary>
    private readonly Stack<List<ExtensionElement>> _pending = new();

    private UblDocument(XmlWriter writer, bool ownsDocument)
    {
        _writer = writer;
        _ownsDocument = ownsDocument;
    }

    /// <summary>Wraps a writer whose document and root element the caller writes itself.</summary>
    public static UblDocument Wrap(XmlWriter writer) => new(writer, ownsDocument: false);

    /// <summary>Starts a document with the two namespaces every UBL document binds.</summary>
    public static UblDocument Open(Stream destination, string rootLocalName, string rootNamespace)
    {
        var settings = new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "  ",
            Encoding = new System.Text.UTF8Encoding(false),
            CloseOutput = false,
        };

        XmlWriter writer = XmlWriter.Create(destination, settings);
        writer.WriteStartDocument();
        writer.WriteStartElement(rootLocalName, rootNamespace);
        writer.WriteAttributeString("xmlns", UblNames.CacPrefix, null, UblNames.Cac.NamespaceName);
        writer.WriteAttributeString("xmlns", UblNames.CbcPrefix, null, UblNames.Cbc.NamespaceName);

        return new UblDocument(writer, ownsDocument: true);
    }

    /// <summary>Ends the node, writing back anything whose anchor never appeared.</summary>
    /// <remarks>
    /// An anchor goes missing when the element it named was itself dropped — a value the model holds and the
    /// writer had nothing to write for. Emitting the leftovers here keeps the content, which matters more
    /// than its position, and is what this did for everything before anchors existed.
    /// </remarks>
    public void EndNode()
    {
        _scopes.Pop();

        foreach (ExtensionElement element in _pending.Pop())
        {
            _writer.WriteRaw(element.Xml);
        }
    }

    public void StartCac(string localName)
    {
        _writer.WriteStartElement(UblNames.CacPrefix, localName, UblNames.Cac.NamespaceName);
        _open.Push(UblNames.Cac + localName);
    }

    /// <summary>
    /// Starts an element and scopes the node it holds, so the node's unmapped elements go back where they
    /// came from.
    /// </summary>
    /// <remarks>
    /// Each extension remembers the mapped sibling it followed. Until the matching <see cref="End"/>, every
    /// element written is checked against those anchors and any that named it is emitted straight after —
    /// which is what puts a national extension back in the middle of a document instead of at the end,
    /// where the schema refuses it.
    /// </remarks>
    public void StartCac(string localName, ExtensionData extensions)
    {
        StartCac(localName);
        Node(extensions);
    }

    /// <summary>Scopes the node being written into the element already started.</summary>
    public void Node(ExtensionData extensions)
    {
        _scopes.Push(_open.Count);
        List<ExtensionElement> pending = [.. extensions.Where(Writable)];
        _pending.Push(pending);

        // Anything the document had before its first mapped element goes out now.
        FlushLeading(pending);
    }

    /// <summary>
    /// Ends an element, writing back anything the document had after it — and, when this element scoped a
    /// node, anything of that node still unplaced.
    /// </summary>
    public void End()
    {
        if (_scopes.Count > 0 && _scopes.Peek() == _open.Count)
        {
            EndNode();
        }

        _writer.WriteEndElement();
        Placed(_open.Count > 0 ? _open.Pop() : null);
    }

    /// <summary>
    /// Whether an element that has just ended is a direct child of the node being scoped.
    /// </summary>
    /// <remarks>
    /// An anchor names a sibling, so only siblings may satisfy it. Without this check a <c>cbc:ID</c> nested
    /// three levels down would place an extension anchored to the node's own <c>cbc:ID</c>, and put it
    /// inside an element it never belonged to.
    /// </remarks>
    private bool AtNodeLevel => _scopes.Count > 0 && _open.Count == _scopes.Peek();

    public void Cbc(string localName, string value)
    {
        _writer.WriteElementString(UblNames.CbcPrefix, localName, UblNames.Cbc.NamespaceName, XmlCharacters.Sanitize(value));
        Placed(UblNames.Cbc + localName);
    }

    /// <summary>Writes back the extensions the document had before any mapped element of this node.</summary>
    private void FlushLeading(List<ExtensionElement> pending) => Flush(pending, anchor: null);

    public void Text(string localName, TextField field)
    {
        if (Start(localName, field))
        {
            Attribute("languageID", field.LanguageId);
            Value(field.Raw ?? field.Value);
        }
    }

    public void Code(string localName, CodeField field)
    {
        if (Start(localName, field))
        {
            Attribute("listID", field.ListId);
            Attribute("listVersionID", field.ListVersionId);
            Attribute("listAgencyID", field.ListAgencyId);
            Value(field.Raw ?? field.Value);
        }
    }

    public void Identifier(string localName, IdentifierField field)
    {
        if (Start(localName, field))
        {
            Attribute("schemeID", field.SchemeId);
            Attribute("schemeAgencyID", field.SchemeAgencyId);
            Attribute("schemeVersionID", field.SchemeVersionId);
            Value(field.Raw ?? field.Value);
        }
    }

    public void Quantity(string localName, QuantityField field)
    {
        if (Start(localName, field))
        {
            Attribute("unitCode", field.UnitCode);
            Attribute("unitCodeListVersionID", field.UnitCodeListVersion);
            Value(field.Raw ?? field.Value?.ToString(CultureInfo.InvariantCulture));
        }
    }

    /// <summary>Writes an amount, in the currency the field carries or, failing that, the document's own.</summary>
    /// <remarks>
    /// UBL makes <c>currencyID</c> mandatory on every amount, so an amount assigned as a plain decimal —
    /// which carries no currency — would otherwise be refused by the schema before any rule ran.
    /// </remarks>
    public void Amount(string localName, AmountField field, string? documentCurrency)
    {
        if (Start(localName, field))
        {
            Attribute("currencyID", field.CurrencyCode ?? documentCurrency);
            Value(field.Raw ?? field.Value?.ToString("0.00###############", CultureInfo.InvariantCulture));
        }
    }

    public void Decimal(string localName, Field<decimal> field)
    {
        if (field.IsSet)
        {
            Cbc(localName, field.Raw ?? field.Value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
        }
    }

    public void Date(string localName, DateField field)
    {
        if (field.IsSet)
        {
            Cbc(localName, field.Raw ?? field.Value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty);
        }
    }

    public void Indicator(string localName, IndicatorField field)
    {
        if (field.IsSet)
        {
            Cbc(localName, field.Raw ?? (field.Value == true ? "true" : "false"));
        }
    }

    /// <summary>Writes embedded bytes, base64 as UBL carries them.</summary>
    public void Binary(string localName, BinaryField field)
    {
        if (field.Value is not { } bytes)
        {
            return;
        }

        _writer.WriteStartElement(UblNames.CbcPrefix, localName, UblNames.Cbc.NamespaceName);
        Attribute("mimeCode", field.MimeCode);
        Attribute("filename", field.Filename);
        Value(field.Raw ?? Convert.ToBase64String(bytes));
    }

    /// <summary>Writes a moment as the date and time of day UBL states separately.</summary>
    public void Moment(string dateName, string timeName, DateTimeField field)
    {
        if (!field.IsSet)
        {
            return;
        }

        (string date, string? time) = UblMoment.Split(field);
        Cbc(dateName, date);

        if (time is not null)
        {
            Cbc(timeName, time);
        }
    }

    /// <summary>The <c>XmlWriter</c> surface a writer needs directly, so its own helpers can take this.</summary>
    /// <remarks>
    /// A writer that builds elements by hand — a signature block, a raw namespace declaration — still has to
    /// reach the underlying writer. Routing those through here rather than round it is what keeps every
    /// element passing the anchor check.
    /// </remarks>
    public void WriteStartDocument() => _writer.WriteStartDocument();

    /// <inheritdoc cref="WriteStartDocument"/>
    public void WriteEndDocument() => _writer.WriteEndDocument();

    /// <inheritdoc cref="WriteStartDocument"/>
    public void WriteStartElement(string prefix, string localName, string ns)
    {
        _writer.WriteStartElement(prefix, localName, ns);
        _open.Push(XName.Get(localName, ns));
    }

    /// <inheritdoc cref="WriteStartDocument"/>
    public void WriteStartElement(string localName, string ns)
    {
        _writer.WriteStartElement(localName, ns);
        _open.Push(XName.Get(localName, ns));
    }

    /// <inheritdoc cref="WriteStartDocument"/>
    public void WriteEndElement() => End();

    /// <inheritdoc cref="WriteStartDocument"/>
    public void WriteString(string? text) => _writer.WriteString(text);

    /// <inheritdoc cref="WriteStartDocument"/>
    public void WriteRaw(string xml) => _writer.WriteRaw(xml);

    /// <inheritdoc cref="WriteStartDocument"/>
    public void WriteAttributeString(string name, string value) => _writer.WriteAttributeString(name, value);

    /// <inheritdoc cref="WriteStartDocument"/>
    public void WriteAttributeString(string prefix, string localName, string? ns, string value) =>
        _writer.WriteAttributeString(prefix, localName, ns, value);

    /// <inheritdoc cref="WriteStartDocument"/>
    public void WriteElementString(string prefix, string localName, string ns, string? value)
    {
        _writer.WriteElementString(prefix, localName, ns, value);
        Placed(XName.Get(localName, ns));
    }

    /// <summary>Writes back what a reader kept because nothing in the model claimed it.</summary>
    public void Extensions(ExtensionData extensions)
    {
        foreach (ExtensionElement element in extensions)
        {
            if (SyntaxNamespaces.BelongsTo(element.NamespaceUri, DocumentSyntax.Cii))
            {
                continue;
            }

            _writer.WriteRaw(element.Xml);
        }
    }

    public void Dispose()
    {
        if (!_ownsDocument)
        {
            return;
        }

        _writer.WriteEndElement();
        _writer.WriteEndDocument();
        _writer.Dispose();
    }

    /// <summary>Writes back the extensions that named this element as the one they followed.</summary>
    private void Placed(XName? name)
    {
        if (name is not null && AtNodeLevel)
        {
            Flush(_pending.Peek(), name.ToString());
        }
    }

    private void Flush(List<ExtensionElement> pending, string? anchor)
    {
        for (int index = 0; index < pending.Count;)
        {
            if (pending[index].PrecedingSibling == anchor)
            {
                _writer.WriteRaw(pending[index].Xml);
                pending.RemoveAt(index);
            }
            else
            {
                index++;
            }
        }
    }

    private static bool Writable(ExtensionElement element) =>
        !SyntaxNamespaces.BelongsTo(element.NamespaceUri, DocumentSyntax.Cii);

    /// <summary>The elements started and not yet ended, so ending one knows which it was.</summary>
    private readonly Stack<XName> _open = new();

    /// <summary>At which depth each node scope was opened, so the right <see cref="End"/> closes it.</summary>
    private readonly Stack<int> _scopes = new();

    private bool Start(string localName, IField field)
    {
        if (!field.IsSet)
        {
            return false;
        }

        _writer.WriteStartElement(UblNames.CbcPrefix, localName, UblNames.Cbc.NamespaceName);
        _open.Push(UblNames.Cbc + localName);
        return true;
    }

    private void Attribute(string name, string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            _writer.WriteAttributeString(name, value);
        }
    }

    private void Value(string? text)
    {
        _writer.WriteString(XmlCharacters.Sanitize(text ?? string.Empty));
        End();
    }
}
