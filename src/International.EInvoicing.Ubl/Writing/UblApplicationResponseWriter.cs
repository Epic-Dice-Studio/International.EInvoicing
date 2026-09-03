using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Xml;
using International.EInvoicing.Documents;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;
using International.EInvoicing.Values;
using International.EInvoicing.Xml;

namespace International.EInvoicing.Ubl.Writing;

/// <summary>
/// Writes a lifecycle status message as a UBL <c>ApplicationResponse</c>.
/// </summary>
/// <remarks>
/// <para>
/// Element order is normative in UBL, so this writer is explicit rather than generated: the order below
/// follows <c>UBL-ApplicationResponse-2.1.xsd</c>.
/// </para>
/// <para>
/// The model holds one moment where UBL states two elements, and one status detail per <c>cac:Status</c>
/// where UBL tells a reason from a requested action by the code list. Both are undone here, so a message
/// read from a document and written back has the same elements in the same places.
/// </para>
/// </remarks>
[SuppressMessage(
    "Performance",
    "CA1822:Mark members as static",
    Justification = "An instance API so a caller can replace this writer through the registry.")]
public sealed class UblApplicationResponseWriter : IDocumentWriter<LifecycleStatusMessage>
{
    /// <inheritdoc />
    public DocumentSyntax Syntax => DocumentSyntax.Ubl;

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
    public Task WriteAsync(
        LifecycleStatusMessage document,
        Stream destination,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(destination);

        return DocumentStreams.WriteAllAsync(WriteToString(document), destination, cancellationToken);
    }

    private static void Write(LifecycleStatusMessage message, XmlWriter writer)
    {
        writer.WriteStartDocument();
        writer.WriteStartElement(
            UblApplicationResponseNames.RootElement,
            UblApplicationResponseNames.ApplicationResponse.NamespaceName);
        writer.WriteAttributeString("xmlns", UblNames.CacPrefix, null, UblNames.Cac.NamespaceName);
        writer.WriteAttributeString("xmlns", UblNames.CbcPrefix, null, UblNames.Cbc.NamespaceName);

        if (message.SpecificationIdentifier.IsDeclared)
        {
            Cbc(writer, "CustomizationID", message.SpecificationIdentifier.Value);
        }

        WriteIdentifier(writer, "ProfileID", message.BusinessProcessType);
        WriteIdentifier(writer, "ID", message.Identifier);
        WriteMoment(message.IssuedAt, writer);
        WriteText(writer, "Note", message.Note);

        WriteParty(message.Sender, "SenderParty", writer);
        WriteParty(message.Recipients.FirstOrDefault(), "ReceiverParty", writer);

        foreach (ReferencedDocumentStatus status in message.References)
        {
            WriteDocumentResponse(status, writer);
        }

        WriteExtensions(message.Extensions, writer);

        writer.WriteEndElement();
        writer.WriteEndDocument();
    }

    /// <summary>The model holds one moment; UBL states the date and the time of day separately.</summary>
    private static void WriteMoment(DateTimeField field, XmlWriter writer)
    {
        if (!field.IsSet)
        {
            return;
        }

        string text = field.Raw
            ?? field.Value?.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture)
            ?? string.Empty;

        int separator = text.IndexOf('T', StringComparison.Ordinal);
        Cbc(writer, "IssueDate", separator < 0 ? text : text[..separator]);

        if (separator >= 0 && separator + 1 < text.Length)
        {
            Cbc(writer, "IssueTime", text[(separator + 1)..]);
        }
    }

    private static void WriteDocumentResponse(ReferencedDocumentStatus status, XmlWriter writer)
    {
        StartCac(writer, "DocumentResponse");

        StartCac(writer, "Response");
        WriteCode(writer, "ResponseCode", status.ProcessConditionCode);
        WriteText(writer, "Description", status.ProcessCondition);
        WriteDate(writer, "EffectiveDate", status.EffectiveDate);
        WriteStatuses(status.StatusDetails, writer);
        writer.WriteEndElement();

        StartCac(writer, "DocumentReference");
        WriteIdentifier(writer, "ID", status.DocumentIdentifier);
        WriteDate(writer, "IssueDate", status.DocumentIssueDate);
        WriteCode(writer, "DocumentTypeCode", status.DocumentTypeCode);
        WriteIdentifier(writer, "VersionID", status.DocumentVersion);
        WriteExtensions(status.Extensions, writer);
        writer.WriteEndElement();

        WriteParty(status.Issuer, "IssuerParty", writer);
        WriteParty(status.Recipient, "RecipientParty", writer);

        foreach (ReferencedLineStatus line in status.LineStatuses)
        {
            StartCac(writer, "LineResponse");

            StartCac(writer, "LineReference");
            WriteIdentifier(writer, "LineID", line.LineIdentifier);
            writer.WriteEndElement();

            StartCac(writer, "Response");
            WriteCode(writer, "ResponseCode", line.ProcessConditionCode);
            WriteText(writer, "Description", line.ProcessCondition);
            WriteStatuses(line.StatusDetails, writer);
            writer.WriteEndElement();

            WriteExtensions(line.Extensions, writer);
            writer.WriteEndElement();
        }

        writer.WriteEndElement();
    }

    /// <summary>
    /// One <c>cac:Status</c> per detail, carrying whichever of the two codes the detail holds.
    /// </summary>
    /// <remarks>
    /// A detail that carries both — which is how the UN/CEFACT side of this model states a reason and the
    /// action it calls for in one place — becomes the two elements UBL requires, in that order.
    /// </remarks>
    private static void WriteStatuses(IReadOnlyList<DocumentStatusDetail> details, XmlWriter writer)
    {
        foreach (DocumentStatusDetail detail in details)
        {
            bool written = false;

            if (detail.ReasonCode.IsSet || detail.Reason.IsSet)
            {
                WriteStatus(detail, detail.ReasonCode, detail.Reason, writer);
                written = true;
            }

            if (detail.RequestedActionCode.IsSet || detail.RequestedAction.IsSet)
            {
                WriteStatus(detail, WithActionList(detail.RequestedActionCode), detail.RequestedAction, writer);
                written = true;
            }

            if (!written)
            {
                WriteStatus(detail, CodeField.Unset, TextField.Unset, writer);
            }
        }
    }

    private static void WriteStatus(DocumentStatusDetail detail, CodeField code, TextField reason, XmlWriter writer)
    {
        StartCac(writer, "Status");
        WriteCode(writer, "StatusReasonCode", code);
        WriteText(writer, "StatusReason", reason);

        foreach (DocumentStatusCharacteristic characteristic in detail.Characteristics)
        {
            StartCac(writer, "Condition");
            WriteIdentifier(writer, "AttributeID", characteristic.Identifier);
            WriteText(writer, "Description", characteristic.ValueText);
            WriteExtensions(characteristic.Extensions, writer);
            writer.WriteEndElement();
        }

        WriteExtensions(detail.Extensions, writer);
        writer.WriteEndElement();
    }

    /// <summary>
    /// A requested action is only a requested action because its <c>listID</c> says so: a message written
    /// without it comes back as a reason, and the round trip loses what the sender asked for.
    /// </summary>
    private static CodeField WithActionList(CodeField code) =>
        code.ListId is null ? code with { ListId = UblApplicationResponseNames.ActionCodeList } : code;

    private static void WriteParty(StatusParty? party, string localName, XmlWriter writer)
    {
        if (party is null)
        {
            return;
        }

        StartCac(writer, localName);
        WriteIdentifier(writer, "EndpointID", party.ElectronicAddress);

        if (party.GlobalIdentifier.IsSet)
        {
            StartCac(writer, "PartyIdentification");
            WriteIdentifier(writer, "ID", party.GlobalIdentifier);
            writer.WriteEndElement();
        }

        if (party.TradingName.IsSet)
        {
            StartCac(writer, "PartyName");
            WriteText(writer, "Name", party.TradingName);
            writer.WriteEndElement();
        }

        if (party.Name.IsSet)
        {
            StartCac(writer, "PartyLegalEntity");
            WriteText(writer, "RegistrationName", party.Name);
            writer.WriteEndElement();
        }

        if (party.Contact is { } contact)
        {
            StartCac(writer, "Contact");
            WriteText(writer, "Name", contact.Name);
            WriteText(writer, "Telephone", contact.Telephone);
            WriteText(writer, "ElectronicMail", contact.Email);
            WriteExtensions(contact.Extensions, writer);
            writer.WriteEndElement();
        }

        WriteExtensions(party.Extensions, writer);
        writer.WriteEndElement();
    }

    private static void WriteExtensions(ExtensionData extensions, XmlWriter writer)
    {
        foreach (ExtensionElement element in extensions)
        {
            if (SyntaxNamespaces.BelongsTo(element.NamespaceUri, DocumentSyntax.Cii))
            {
                continue;
            }

            writer.WriteRaw(element.Xml);
        }
    }

    private static void StartCac(XmlWriter writer, string localName) =>
        writer.WriteStartElement(UblNames.CacPrefix, localName, UblNames.Cac.NamespaceName);

    private static void Cbc(XmlWriter writer, string localName, string value) =>
        writer.WriteElementString(UblNames.CbcPrefix, localName, UblNames.Cbc.NamespaceName, XmlCharacters.Sanitize(value));

    private static void WriteText(XmlWriter writer, string localName, TextField field)
    {
        if (!field.IsSet)
        {
            return;
        }

        writer.WriteStartElement(UblNames.CbcPrefix, localName, UblNames.Cbc.NamespaceName);
        WriteAttributeIfSet(writer, "languageID", field.LanguageId);
        writer.WriteString(XmlCharacters.Sanitize(field.Raw ?? field.Value ?? string.Empty));
        writer.WriteEndElement();
    }

    private static void WriteCode(XmlWriter writer, string localName, CodeField field)
    {
        if (!field.IsSet)
        {
            return;
        }

        writer.WriteStartElement(UblNames.CbcPrefix, localName, UblNames.Cbc.NamespaceName);
        WriteAttributeIfSet(writer, "listID", field.ListId);
        WriteAttributeIfSet(writer, "listVersionID", field.ListVersionId);
        WriteAttributeIfSet(writer, "listAgencyID", field.ListAgencyId);
        writer.WriteString(XmlCharacters.Sanitize(field.Raw ?? field.Value ?? string.Empty));
        writer.WriteEndElement();
    }

    private static void WriteIdentifier(XmlWriter writer, string localName, IdentifierField field)
    {
        if (!field.IsSet)
        {
            return;
        }

        writer.WriteStartElement(UblNames.CbcPrefix, localName, UblNames.Cbc.NamespaceName);
        WriteAttributeIfSet(writer, "schemeID", field.SchemeId);
        WriteAttributeIfSet(writer, "schemeAgencyID", field.SchemeAgencyId);
        WriteAttributeIfSet(writer, "schemeVersionID", field.SchemeVersionId);
        writer.WriteString(XmlCharacters.Sanitize(field.Raw ?? field.Value ?? string.Empty));
        writer.WriteEndElement();
    }

    private static void WriteDate(XmlWriter writer, string localName, DateField field)
    {
        if (field.IsSet)
        {
            Cbc(writer, localName, field.Raw ?? field.Value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty);
        }
    }

    private static void WriteAttributeIfSet(XmlWriter writer, string name, string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            writer.WriteAttributeString(name, value);
        }
    }
}
