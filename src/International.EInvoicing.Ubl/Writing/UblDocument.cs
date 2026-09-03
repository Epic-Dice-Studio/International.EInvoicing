using System.Globalization;
using System.Xml;
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

    private UblDocument(XmlWriter writer) => _writer = writer;

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

        return new UblDocument(writer);
    }

    public void StartCac(string localName) =>
        _writer.WriteStartElement(UblNames.CacPrefix, localName, UblNames.Cac.NamespaceName);

    public void End() => _writer.WriteEndElement();

    public void Cbc(string localName, string value) =>
        _writer.WriteElementString(UblNames.CbcPrefix, localName, UblNames.Cbc.NamespaceName, XmlCharacters.Sanitize(value));

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
        _writer.WriteEndElement();
        _writer.WriteEndDocument();
        _writer.Dispose();
    }

    private bool Start(string localName, IField field)
    {
        if (!field.IsSet)
        {
            return false;
        }

        _writer.WriteStartElement(UblNames.CbcPrefix, localName, UblNames.Cbc.NamespaceName);
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
        _writer.WriteEndElement();
    }
}
