using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Xml;
using International.EInvoicing.Cdar.Model;
using International.EInvoicing.Model;
using International.EInvoicing.Values;

namespace International.EInvoicing.Cdar.Writing;

/// <summary>
/// Writes a lifecycle message as UN/CEFACT CDAR.
/// </summary>
/// <remarks>
/// Element order follows the CDAR schema, as it must: a message with the right elements in the wrong order is
/// rejected before anyone reads its status. A field read from a message and not modified is written back from
/// its raw text, including the format code of a timestamp.
/// </remarks>
[SuppressMessage(
    "Performance",
    "CA1822:Mark members as static",
    Justification = "An instance API so a caller can replace this writer through the registry.")]
public sealed class CdarWriter
{
    /// <summary>Writes <paramref name="message"/> to <paramref name="stream"/>. The stream is left open.</summary>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    public void Write(LifecycleStatusMessage message, Stream stream)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(stream);

        var settings = new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "  ",
            Encoding = new System.Text.UTF8Encoding(false),
            CloseOutput = false,
        };

        using XmlWriter writer = XmlWriter.Create(stream, settings);
        Write(message, writer);
    }

    /// <summary>Writes <paramref name="message"/> and returns it as XML text.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="message"/> is <c>null</c>.</exception>
    public string WriteToString(LifecycleStatusMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        using var stream = new MemoryStream();
        Write(message, stream);
        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void Write(LifecycleStatusMessage message, XmlWriter writer)
    {
        writer.WriteStartDocument();
        writer.WriteStartElement(
            CdarNames.RsmPrefix,
            "CrossDomainAcknowledgementAndResponse",
            CdarNames.Rsm.NamespaceName);
        writer.WriteAttributeString("xmlns", CdarNames.RamPrefix, null, CdarNames.Ram.NamespaceName);
        writer.WriteAttributeString("xmlns", CdarNames.QdtPrefix, null, CdarNames.Qdt.NamespaceName);
        writer.WriteAttributeString("xmlns", CdarNames.UdtPrefix, null, CdarNames.Udt.NamespaceName);

        WriteContext(message, writer);
        WriteDocument(message, writer);
        WriteAcknowledgement(message, writer);
        WriteExtensions(message.Extensions, writer);

        writer.WriteEndElement();
        writer.WriteEndDocument();
    }

    private static void WriteContext(LifecycleStatusMessage message, XmlWriter writer)
    {
        StartRsm(writer, "ExchangedDocumentContext");

        if (message.BusinessProcessType.IsSet)
        {
            StartRam(writer, "BusinessProcessSpecifiedDocumentContextParameter");
            WriteIdentifier(writer, "ID", message.BusinessProcessType);
            writer.WriteEndElement();
        }

        if (message.SpecificationIdentifier.IsDeclared)
        {
            StartRam(writer, "GuidelineSpecifiedDocumentContextParameter");
            Ram(writer, "ID", message.SpecificationIdentifier.Value);
            writer.WriteEndElement();
        }

        writer.WriteEndElement();
    }

    private static void WriteDocument(LifecycleStatusMessage message, XmlWriter writer)
    {
        StartRsm(writer, "ExchangedDocument");
        WriteIdentifier(writer, "ID", message.Identifier);
        WriteText(writer, "Name", message.Name);
        WriteDateTime(writer, "IssueDateTime", message.IssuedAt);
        WriteParty(writer, "SenderTradeParty", message.Sender);
        WriteParty(writer, "IssuerTradeParty", message.Issuer);

        foreach (StatusParty recipient in message.Recipients)
        {
            WriteParty(writer, "RecipientTradeParty", recipient);
        }

        writer.WriteEndElement();
    }

    private static void WriteAcknowledgement(LifecycleStatusMessage message, XmlWriter writer)
    {
        StartRsm(writer, "AcknowledgementDocument");

        if (message.CoversMultipleDocuments.IsSet)
        {
            StartRam(writer, "MultipleReferencesIndicator");
            writer.WriteElementString(
                CdarNames.UdtPrefix,
                "Indicator",
                CdarNames.Udt.NamespaceName,
                message.CoversMultipleDocuments.Raw ?? (message.CoversMultipleDocuments.Value == true ? "true" : "false"));
            writer.WriteEndElement();
        }

        WriteCode(writer, "TypeCode", message.TypeCode);
        WriteDateTime(writer, "IssueDateTime", message.StatusIssuedAt);

        foreach (ReferencedDocumentStatus status in message.References)
        {
            StartRam(writer, "ReferenceReferencedDocument");
            WriteIdentifier(writer, "IssuerAssignedID", status.DocumentIdentifier);
            WriteCode(writer, "StatusCode", status.StatusCode);
            WriteCode(writer, "TypeCode", status.DocumentTypeCode);
            WriteDateTime(writer, "ReceiptDateTime", status.ReceivedAt);
            WriteDate(writer, "FormattedIssueDateTime", status.DocumentIssueDate);
            WriteCode(writer, "ProcessConditionCode", status.ProcessConditionCode);
            WriteText(writer, "ProcessCondition", status.ProcessCondition);
            WriteText(writer, "Reason", status.Reason);
            WriteParty(writer, "IssuerTradeParty", status.Issuer);
            WriteExtensions(status.Extensions, writer);
            writer.WriteEndElement();
        }

        writer.WriteEndElement();
    }

    private static void WriteParty(XmlWriter writer, string elementName, StatusParty? party)
    {
        if (party is null)
        {
            return;
        }

        StartRam(writer, elementName);
        WriteIdentifier(writer, "GlobalID", party.GlobalIdentifier);
        WriteText(writer, "Name", party.Name);
        WriteCode(writer, "RoleCode", party.RoleCode);

        if (party.ElectronicAddress.IsSet)
        {
            StartRam(writer, "URIUniversalCommunication");
            WriteIdentifier(writer, "URIID", party.ElectronicAddress);
            writer.WriteEndElement();
        }

        WriteExtensions(party.Extensions, writer);
        writer.WriteEndElement();
    }

    private static void WriteExtensions(ExtensionData extensions, XmlWriter writer)
    {
        foreach (ExtensionElement element in extensions)
        {
            writer.WriteRaw(element.Xml);
        }
    }

    private static void StartRsm(XmlWriter writer, string localName) =>
        writer.WriteStartElement(CdarNames.RsmPrefix, localName, CdarNames.Rsm.NamespaceName);

    private static void StartRam(XmlWriter writer, string localName) =>
        writer.WriteStartElement(CdarNames.RamPrefix, localName, CdarNames.Ram.NamespaceName);

    private static void Ram(XmlWriter writer, string localName, string value) =>
        writer.WriteElementString(CdarNames.RamPrefix, localName, CdarNames.Ram.NamespaceName, value);

    private static void WriteText(XmlWriter writer, string localName, TextField field)
    {
        if (field.IsSet)
        {
            Ram(writer, localName, field.Raw ?? field.Value ?? string.Empty);
        }
    }

    private static void WriteCode(XmlWriter writer, string localName, CodeField field)
    {
        if (!field.IsSet)
        {
            return;
        }

        writer.WriteStartElement(CdarNames.RamPrefix, localName, CdarNames.Ram.NamespaceName);
        if (!string.IsNullOrEmpty(field.ListId))
        {
            writer.WriteAttributeString("listID", field.ListId);
        }

        writer.WriteString(field.Raw ?? field.Value ?? string.Empty);
        writer.WriteEndElement();
    }

    private static void WriteIdentifier(XmlWriter writer, string localName, IdentifierField field)
    {
        if (!field.IsSet)
        {
            return;
        }

        writer.WriteStartElement(CdarNames.RamPrefix, localName, CdarNames.Ram.NamespaceName);
        if (!string.IsNullOrEmpty(field.SchemeId))
        {
            writer.WriteAttributeString("schemeID", field.SchemeId);
        }

        writer.WriteString(field.Raw ?? field.Value ?? string.Empty);
        writer.WriteEndElement();
    }

    private static void WriteDateTime(XmlWriter writer, string localName, DateTimeField field)
    {
        if (!field.IsSet)
        {
            return;
        }

        StartRam(writer, localName);
        writer.WriteStartElement(CdarNames.UdtPrefix, "DateTimeString", CdarNames.Udt.NamespaceName);
        writer.WriteAttributeString("format", field.FormatCode ?? DateTimeField.FormatCcyyMmDdHhMmSs);
        writer.WriteString(
            field.Raw ?? field.Value?.UtcDateTime.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture) ?? string.Empty);
        writer.WriteEndElement();
        writer.WriteEndElement();
    }

    /// <summary>The date of the document being reported on is a qualified, not unqualified, date string.</summary>
    private static void WriteDate(XmlWriter writer, string localName, DateField field)
    {
        if (!field.IsSet)
        {
            return;
        }

        StartRam(writer, localName);
        writer.WriteStartElement(CdarNames.QdtPrefix, "DateTimeString", CdarNames.Qdt.NamespaceName);
        writer.WriteAttributeString("format", field.FormatCode ?? DateField.FormatCcyyMmDd);
        writer.WriteString(
            field.Raw ?? field.Value?.ToString("yyyyMMdd", CultureInfo.InvariantCulture) ?? string.Empty);
        writer.WriteEndElement();
        writer.WriteEndElement();
    }
}
