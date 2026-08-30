using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Xml;
using International.EInvoicing.Cdar.Model;
using International.EInvoicing.Documents;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;
using International.EInvoicing.Values;
using International.EInvoicing.Xml;

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
public sealed class CdarWriter : IDocumentWriter<LifecycleStatusMessage>
{
    /// <inheritdoc />
    public DocumentSyntax Syntax => DocumentSyntax.Cdar;

    /// <summary>Writes <paramref name="document"/> to <paramref name="destination"/>. The stream is left open.</summary>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    public void Write(LifecycleStatusMessage document, Stream destination)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(destination);

        var settings = new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "  ",
            Encoding = new System.Text.UTF8Encoding(false),
            CloseOutput = false,
        };

        using XmlWriter writer = XmlWriter.Create(destination, settings);
        Write(document, writer);
    }

    /// <summary>Writes <paramref name="document"/> and returns it as XML text.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> is <c>null</c>.</exception>
    public string WriteToString(LifecycleStatusMessage document)
    {
        ArgumentNullException.ThrowIfNull(document);

        using var buffer = new MemoryStream();
        Write(document, buffer);
        return System.Text.Encoding.UTF8.GetString(buffer.ToArray());
    }

    /// <inheritdoc />
    public Task WriteAsync(LifecycleStatusMessage document, Stream destination, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(destination);

        return DocumentStreams.WriteAllAsync(WriteToString(document), destination, cancellationToken);
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
                CdarNames.Udt.NamespaceName, XmlCharacters.Sanitize(message.CoversMultipleDocuments.Raw ?? (message.CoversMultipleDocuments.Value == true ? "true" : "false")));
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

            foreach (DocumentStatusDetail detail in status.StatusDetails)
            {
                WriteStatusDetail(writer, detail);
            }

            WriteExtensions(status.Extensions, writer);
            writer.WriteEndElement();
        }

        writer.WriteEndElement();
    }

    private static void WriteStatusDetail(XmlWriter writer, DocumentStatusDetail detail)
    {
        StartRam(writer, "SpecifiedDocumentStatus");
        WriteCode(writer, "ProcessConditionCode", detail.ProcessConditionCode);
        WriteCode(writer, "ReasonCode", detail.ReasonCode);
        WriteText(writer, "Reason", detail.Reason);
        WriteCode(writer, "RequestedActionCode", detail.RequestedActionCode);
        WriteText(writer, "RequestedAction", detail.RequestedAction);
        WriteNumber(writer, "SequenceNumeric", detail.SequenceNumber);

        foreach (DocumentStatusCharacteristic characteristic in detail.Characteristics)
        {
            WriteCharacteristic(writer, characteristic);
        }

        WriteExtensions(detail.Extensions, writer);
        writer.WriteEndElement();
    }

    private static void WriteCharacteristic(XmlWriter writer, DocumentStatusCharacteristic characteristic)
    {
        StartRam(writer, "SpecifiedDocumentCharacteristic");
        WriteIdentifier(writer, "ID", characteristic.Identifier);
        WriteCode(writer, "TypeCode", characteristic.TypeCode);

        // The changed flag is an IndicatorString here, not the Indicator the rest of the message uses.
        if (characteristic.ValueChanged.IsSet)
        {
            StartRam(writer, "ValueChangedIndicator");
            writer.WriteElementString(
                CdarNames.UdtPrefix,
                "IndicatorString",
                CdarNames.Udt.NamespaceName, XmlCharacters.Sanitize(characteristic.ValueChanged.Raw
                    ?? (characteristic.ValueChanged.Value == true ? "true" : "false")));
            writer.WriteEndElement();
        }

        WriteText(writer, "Name", characteristic.Name);
        WriteText(writer, "Location", characteristic.Location);
        WriteAmount(writer, "ValueAmount", characteristic.ValueAmount);
        WriteDecimal(writer, "ValuePercent", characteristic.ValuePercent);
        WriteText(writer, "ValueText", characteristic.ValueText);
        WriteExtensions(characteristic.Extensions, writer);
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
        writer.WriteElementString(CdarNames.RamPrefix, localName, CdarNames.Ram.NamespaceName, XmlCharacters.Sanitize(value));

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
            writer.WriteAttributeString("listID", XmlCharacters.Sanitize(field.ListId));
        }

        writer.WriteString(XmlCharacters.Sanitize(field.Raw ?? field.Value ?? string.Empty));
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
            writer.WriteAttributeString("schemeID", XmlCharacters.Sanitize(field.SchemeId));
        }

        writer.WriteString(XmlCharacters.Sanitize(field.Raw ?? field.Value ?? string.Empty));
        writer.WriteEndElement();
    }

    private static void WriteAmount(XmlWriter writer, string localName, AmountField field)
    {
        if (!field.IsSet)
        {
            return;
        }

        writer.WriteStartElement(CdarNames.RamPrefix, localName, CdarNames.Ram.NamespaceName);
        if (!string.IsNullOrEmpty(field.CurrencyCode))
        {
            writer.WriteAttributeString("currencyID", XmlCharacters.Sanitize(field.CurrencyCode));
        }

        writer.WriteString(XmlCharacters.Sanitize(field.Raw ?? field.Value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty));
        writer.WriteEndElement();
    }

    private static void WriteDecimal(XmlWriter writer, string localName, Field<decimal> field)
    {
        if (field.IsSet)
        {
            Ram(writer, localName, field.Raw ?? field.Value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
        }
    }

    private static void WriteNumber(XmlWriter writer, string localName, Field<int> field)
    {
        if (field.IsSet)
        {
            Ram(writer, localName, field.Raw ?? field.Value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
        }
    }

    private static void WriteDateTime(XmlWriter writer, string localName, DateTimeField field)
    {
        if (!field.IsSet)
        {
            return;
        }

        StartRam(writer, localName);
        writer.WriteStartElement(CdarNames.UdtPrefix, "DateTimeString", CdarNames.Udt.NamespaceName);
        writer.WriteAttributeString("format", XmlCharacters.Sanitize(field.FormatCode ?? DateTimeField.FormatCcyyMmDdHhMmSs));
        writer.WriteString(XmlCharacters.Sanitize(
            field.Raw ?? field.Value?.UtcDateTime.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture) ?? string.Empty));
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
        writer.WriteAttributeString("format", XmlCharacters.Sanitize(field.FormatCode ?? DateField.FormatCcyyMmDd));
        writer.WriteString(XmlCharacters.Sanitize(
            field.Raw ?? field.Value?.ToString("yyyyMMdd", CultureInfo.InvariantCulture) ?? string.Empty));
        writer.WriteEndElement();
        writer.WriteEndElement();
    }
}
