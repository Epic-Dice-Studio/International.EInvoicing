using System.Globalization;
using System.Xml;
using International.EInvoicing.Model;
using International.EInvoicing.Values;
using International.EInvoicing.Xml;

namespace International.EInvoicing.OrderX.Writing;

/// <summary>
/// Writes the elements of an Order-X document.
/// </summary>
/// <remarks>
/// The typed part of writing: which attributes a code carries, how CII wraps an indicator and a moment, how
/// a decimal is formatted. Where unmapped content goes back is <see cref="AnchoredDocument"/>'s business,
/// and this forwards to it, so an extension is written among a node's children rather than after them.
/// </remarks>
internal sealed class OrderXDocument(XmlWriter xml) : IDisposable
{
    private readonly AnchoredDocument _writer = new(xml);

    /// <summary>Starts the document element, binding the four namespaces every Order-X document uses.</summary>
    public void StartDocument()
    {
        _writer.WriteStartDocument();
        _writer.WriteStartElement(
            OrderXNames.RsmPrefix,
            "SCRDMCCBDACIOMessageStructure",
            OrderXNames.Rsm.NamespaceName);
        _writer.WriteAttributeString("xmlns", OrderXNames.QdtPrefix, null, OrderXNames.Qdt.NamespaceName);
        _writer.WriteAttributeString("xmlns", OrderXNames.RamPrefix, null, OrderXNames.Ram.NamespaceName);
        _writer.WriteAttributeString("xmlns", OrderXNames.UdtPrefix, null, OrderXNames.Udt.NamespaceName);
    }

    public void EndDocument()
    {
        End();
        _writer.WriteEndDocument();
    }

    public void StartRsm(string localName) =>
        _writer.WriteStartElement(OrderXNames.RsmPrefix, localName, OrderXNames.Rsm.NamespaceName);

    public void StartRam(string localName) =>
        _writer.WriteStartElement(OrderXNames.RamPrefix, localName, OrderXNames.Ram.NamespaceName);

    /// <summary>Starts a <c>ram:</c> element and scopes the node's extensions to it in one step.</summary>
    public void StartRam(string localName, ExtensionData extensions)
    {
        StartRam(localName);
        Node(extensions);
    }

    /// <summary>
    /// Declares that the element now open is a model node, so its extensions are flushed among its children.
    /// </summary>
    public void Node(ExtensionData extensions) => _writer.Node(extensions);

    public void End() => _writer.WriteEndElement();

    public void Ram(string localName, string value) =>
        _writer.WriteElementString(
            OrderXNames.RamPrefix,
            localName,
            OrderXNames.Ram.NamespaceName,
            XmlCharacters.Sanitize(value));

    public void Text(string localName, TextField field)
    {
        if (!field.IsSet)
        {
            return;
        }

        Start(localName);
        Attribute("languageID", field.LanguageId);
        Value(field.Raw ?? field.Value);
        End();
    }

    public void Code(string localName, CodeField field)
    {
        if (!field.IsSet)
        {
            return;
        }

        Start(localName);
        Attribute("listID", field.ListId);
        Attribute("listVersionID", field.ListVersionId);
        Value(field.Raw ?? field.Value);
        End();
    }

    public void Identifier(string localName, IdentifierField field)
    {
        if (!field.IsSet)
        {
            return;
        }

        Start(localName);
        Attribute("schemeID", field.SchemeId);
        Value(field.Raw ?? field.Value);
        End();
    }

    /// <summary>
    /// Writes an amount.
    /// </summary>
    /// <remarks>
    /// Order-X states the currency once, on the document, and forbids <c>currencyID</c> on nearly every
    /// amount — the same rule the Cross Industry Invoice has. <c>TaxTotalAmount</c> is the exception, and
    /// there the attribute is <em>required</em>, because a document may state the tax in a second currency.
    /// <paramref name="documentCurrency"/> stands in when the field carries none, which is what a caller
    /// assigning a plain <c>decimal</c> leaves behind.
    /// </remarks>
    public void Amount(
        string localName,
        AmountField field,
        bool withCurrency = false,
        string? documentCurrency = null)
    {
        if (!field.IsSet)
        {
            return;
        }

        if (!withCurrency)
        {
            Ram(localName, field.Raw ?? Format(field.Value));
            return;
        }

        Start(localName);

        // Nothing is invented when neither the amount nor the document names a currency: the document goes
        // out without the attribute and the schema says so, which beats guessing at somebody's money.
        Attribute("currencyID", field.CurrencyCode ?? documentCurrency);
        Value(field.Raw ?? Format(field.Value));
        End();
    }

    public void Quantity(string localName, QuantityField field)
    {
        if (!field.IsSet)
        {
            return;
        }

        Start(localName);
        Attribute("unitCode", field.UnitCode);
        Value(field.Raw ?? Format(field.Value));
        End();
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

        Start(localName);
        _writer.WriteElementString(
            OrderXNames.UdtPrefix,
            "Indicator",
            OrderXNames.Udt.NamespaceName,
            field.Raw ?? (field.Value == true ? "true" : "false"));
        End();
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

        string format = field.FormatCode ?? "102";

        Start(localName);
        _writer.WriteStartElement(OrderXNames.UdtPrefix, "DateTimeString", OrderXNames.Udt.NamespaceName);
        _writer.WriteAttributeString("format", XmlCharacters.Sanitize(format));
        _writer.WriteString(XmlCharacters.Sanitize(field.Raw ?? Format(field.Value, format)));
        _writer.WriteEndElement();
        End();
    }

    public void Dispose() => _writer.Dispose();

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

    private void Start(string localName) => StartRam(localName);

    private void Value(string? text) => _writer.WriteString(XmlCharacters.Sanitize(text ?? string.Empty));

    private void Attribute(string name, string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            _writer.WriteAttributeString(name, value);
        }
    }
}
