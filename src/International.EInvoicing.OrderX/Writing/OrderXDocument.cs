using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using International.EInvoicing.Model;
using International.EInvoicing.Values;
using International.EInvoicing.Xml;

namespace International.EInvoicing.OrderX.Writing;

/// <summary>
/// Writes the elements of an Order-X document, and puts back what nobody mapped where it was found.
/// </summary>
/// <remarks>
/// <para>
/// The order of a Cross Industry Order's elements is normative, so an unmapped element written at the end of
/// its node is one a receiver's parser rejects. Each extension remembers the mapped sibling it followed;
/// <see cref="Node"/> opens a scope, and the extension is written as soon as that sibling has been.
/// </para>
/// <para>
/// A scope belongs to the element that was open when it was pushed, so the extensions of a party are flushed
/// among the party's children and not among its parent's.
/// </para>
/// </remarks>
internal sealed class OrderXDocument(XmlWriter writer) : IDisposable
{
    private readonly Stack<List<ExtensionElement>> _pending = new();
    private readonly Stack<int> _scopes = new();
    private readonly Stack<XName> _open = new();
    private int _depth;

    /// <summary>Starts the document element, binding the four namespaces every Order-X document uses.</summary>
    public void StartDocument()
    {
        writer.WriteStartDocument();
        writer.WriteStartElement(
            OrderXNames.RsmPrefix,
            "SCRDMCCBDACIOMessageStructure",
            OrderXNames.Rsm.NamespaceName);
        _open.Push(OrderXNames.Root);
        writer.WriteAttributeString("xmlns", OrderXNames.QdtPrefix, null, OrderXNames.Qdt.NamespaceName);
        writer.WriteAttributeString("xmlns", OrderXNames.RamPrefix, null, OrderXNames.Ram.NamespaceName);
        writer.WriteAttributeString("xmlns", OrderXNames.UdtPrefix, null, OrderXNames.Udt.NamespaceName);
        _depth++;
    }

    public void EndDocument()
    {
        End();
        writer.WriteEndDocument();
    }

    public void StartRsm(string localName)
    {
        writer.WriteStartElement(OrderXNames.RsmPrefix, localName, OrderXNames.Rsm.NamespaceName);
        _open.Push(OrderXNames.Rsm + localName);
        _depth++;
    }

    public void StartRam(string localName)
    {
        writer.WriteStartElement(OrderXNames.RamPrefix, localName, OrderXNames.Ram.NamespaceName);
        _open.Push(OrderXNames.Ram + localName);
        _depth++;
    }

    /// <summary>Starts a <c>ram:</c> element and scopes the node's extensions to it in one step.</summary>
    public void StartRam(string localName, ExtensionData extensions)
    {
        StartRam(localName);
        Node(extensions);
    }

    /// <summary>
    /// Declares that the element now open is a model node, so its extensions are flushed among its children.
    /// </summary>
    public void Node(ExtensionData extensions)
    {
        ArgumentNullException.ThrowIfNull(extensions);

        List<ExtensionElement> pending = [.. extensions.Where(element => !string.IsNullOrEmpty(element.Xml))];

        _scopes.Push(_depth);
        _pending.Push(pending);

        // Anything that preceded every mapped element goes out first, before the node has written anything.
        Flush(pending, null);
    }

    public void End()
    {
        if (_scopes.Count > 0 && _scopes.Peek() == _depth)
        {
            _scopes.Pop();
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

    public void Ram(string localName, string value) =>
        Written(localName, () => writer.WriteElementString(
            OrderXNames.RamPrefix,
            localName,
            OrderXNames.Ram.NamespaceName,
            XmlCharacters.Sanitize(value)));

    public void Text(string localName, TextField field)
    {
        if (!field.IsSet)
        {
            return;
        }

        Written(localName, () =>
        {
            writer.WriteStartElement(OrderXNames.RamPrefix, localName, OrderXNames.Ram.NamespaceName);
            Attribute("languageID", field.LanguageId);
            writer.WriteString(XmlCharacters.Sanitize(field.Raw ?? field.Value ?? string.Empty));
            writer.WriteEndElement();
        });
    }

    public void Code(string localName, CodeField field)
    {
        if (!field.IsSet)
        {
            return;
        }

        Written(localName, () =>
        {
            writer.WriteStartElement(OrderXNames.RamPrefix, localName, OrderXNames.Ram.NamespaceName);
            Attribute("listID", field.ListId);
            Attribute("listVersionID", field.ListVersionId);
            writer.WriteString(XmlCharacters.Sanitize(field.Raw ?? field.Value ?? string.Empty));
            writer.WriteEndElement();
        });
    }

    public void Identifier(string localName, IdentifierField field)
    {
        if (!field.IsSet)
        {
            return;
        }

        Written(localName, () =>
        {
            writer.WriteStartElement(OrderXNames.RamPrefix, localName, OrderXNames.Ram.NamespaceName);
            Attribute("schemeID", field.SchemeId);
            writer.WriteString(XmlCharacters.Sanitize(field.Raw ?? field.Value ?? string.Empty));
            writer.WriteEndElement();
        });
    }

    /// <summary>
    /// Writes an amount. Order-X states the currency once, on the document, and forbids
    /// <c>currencyID</c> everywhere else — the same rule the Cross Industry Invoice has.
    /// </summary>
    public void Amount(string localName, AmountField field)
    {
        if (field.IsSet)
        {
            Ram(localName, field.Raw ?? Format(field.Value));
        }
    }

    public void Quantity(string localName, QuantityField field)
    {
        if (!field.IsSet)
        {
            return;
        }

        Written(localName, () =>
        {
            writer.WriteStartElement(OrderXNames.RamPrefix, localName, OrderXNames.Ram.NamespaceName);
            Attribute("unitCode", field.UnitCode);
            writer.WriteString(XmlCharacters.Sanitize(field.Raw ?? Format(field.Value)));
            writer.WriteEndElement();
        });
    }

    public void Decimal(string localName, Field<decimal> field)
    {
        if (field.IsSet)
        {
            Ram(localName, field.Raw ?? Format(field.Value));
        }
    }

    /// <summary>Writes a true or false, which CII wraps in a <c>udt:Indicator</c>.</summary>
    public void Indicator(string localName, IndicatorField field)
    {
        if (!field.IsSet)
        {
            return;
        }

        Written(localName, () =>
        {
            writer.WriteStartElement(OrderXNames.RamPrefix, localName, OrderXNames.Ram.NamespaceName);
            writer.WriteElementString(
                OrderXNames.UdtPrefix,
                "Indicator",
                OrderXNames.Udt.NamespaceName,
                field.Raw ?? (field.Value == true ? "true" : "false"));
            writer.WriteEndElement();
        });
    }

    /// <summary>
    /// Writes a moment, which CII wraps in a <c>udt:DateTimeString</c> whose <c>format</c> says how to read
    /// it. The code the document arrived with is kept, so a sender who stated minutes gets minutes back.
    /// </summary>
    public void Moment(string localName, DateTimeField field)
    {
        if (!field.IsSet)
        {
            return;
        }

        Written(localName, () =>
        {
            writer.WriteStartElement(OrderXNames.RamPrefix, localName, OrderXNames.Ram.NamespaceName);
            writer.WriteStartElement(OrderXNames.UdtPrefix, "DateTimeString", OrderXNames.Udt.NamespaceName);
            string format = field.FormatCode ?? "102";
            writer.WriteAttributeString("format", XmlCharacters.Sanitize(format));
            writer.WriteString(XmlCharacters.Sanitize(field.Raw ?? Format(field.Value, format)));
            writer.WriteEndElement();
            writer.WriteEndElement();
        });
    }

    public void Dispose() => writer.Flush();

    private static string Format(decimal? value) =>
        value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;

    private static string Format(DateTimeOffset? value, string format) =>
        value?.UtcDateTime.ToString(
            format switch
            {
                "203" => "yyyyMMddHHmm",
                "204" => "yyyyMMddHHmmss",
                _ => "yyyyMMdd",
            },
            CultureInfo.InvariantCulture) ?? string.Empty;

    private void Attribute(string name, string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            writer.WriteAttributeString(name, value);
        }
    }

    /// <summary>Writes a leaf element, then flushes whatever was anchored to it.</summary>
    private void Written(string localName, Action write)
    {
        write();
        Placed(OrderXNames.Ram + localName);
    }

    /// <summary>
    /// Flushes the extensions anchored to the element just written, when the node they belong to is the one
    /// currently open.
    /// </summary>
    /// <remarks>
    /// The anchor is the reader's own <see cref="XName.ToString"/> of the sibling — namespace and local name
    /// — so it survives a document written with different prefixes than the one it was read from.
    /// </remarks>
    private void Placed(XName name)
    {
        if (_scopes.Count > 0 && _scopes.Peek() == _depth)
        {
            Flush(_pending.Peek(), name.ToString());
        }
    }

    private void Flush(List<ExtensionElement> pending, string? preceding)
    {
        for (var index = 0; index < pending.Count;)
        {
            if (pending[index].PrecedingSibling == preceding)
            {
                writer.WriteRaw(pending[index].Xml);
                pending.RemoveAt(index);
                continue;
            }

            index++;
        }
    }
}
