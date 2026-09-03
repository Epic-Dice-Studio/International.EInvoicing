using System.Xml.Linq;
using International.EInvoicing.Configuration;
using International.EInvoicing.Diagnostics;
using International.EInvoicing.Documents;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;
using International.EInvoicing.Values;
using International.EInvoicing.Xml;

namespace International.EInvoicing.Ubl.Reading;

/// <summary>
/// Reads a UBL <c>ApplicationResponse</c> — what happened to a document — into the canonical model.
/// </summary>
/// <remarks>
/// The same statement the French lifecycle messages make in UN/CEFACT syntax, which is why it fills the same
/// model. Peppol carries two profilings of it: the Invoice Response, which says an invoice is in process,
/// accepted, rejected, under query or paid, and the Message Level Response, which says whether the envelope
/// itself arrived and parsed. Both are this document with different code lists, so both are read here and
/// the codes come back uninterpreted for a profiling this library does not know.
/// </remarks>
public sealed class UblApplicationResponseReader : IDocumentReader<LifecycleStatusMessage>
{
    private readonly EInvoicingOptions _options;
    private readonly IProfileResolver _profiles;

    /// <summary>Creates a reader using the supplied options and profile resolver.</summary>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    public UblApplicationResponseReader(EInvoicingOptions options, IProfileResolver profiles)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(profiles);

        _options = options;
        _profiles = profiles;
    }

    /// <inheritdoc />
    public DocumentSyntax Syntax => DocumentSyntax.Ubl;

    /// <summary>Reads a response from a stream. The stream is left open.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <c>null</c>.</exception>
    public ParseResult<LifecycleStatusMessage> Read(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var diagnostics = new DiagnosticCollector(_options.DiagnosticPolicy);

        XElement root;
        try
        {
            using var reader = SecureXml.CreateReader(stream, _options.Limits);
            root = XElement.Load(reader, LoadOptions.SetLineInfo);
            SecureXml.EnsureDepthWithin(root, _options.Limits);
        }
        catch (System.Xml.XmlException exception)
        {
            diagnostics.Add(Diagnostic.Create(UblDiagnostics.MalformedDocument, exception.Message) with
            {
                Location = new SourceLocation(null, exception.LineNumber, exception.LinePosition),
            });

            return diagnostics.ToResult<LifecycleStatusMessage>(null);
        }

        return diagnostics.ToResult(ReadMessage(root, diagnostics));
    }

    /// <summary>Reads a response from XML text.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="xml"/> is <c>null</c>.</exception>
    public ParseResult<LifecycleStatusMessage> Read(string xml)
    {
        ArgumentNullException.ThrowIfNull(xml);

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(xml));
        return Read(stream);
    }

    /// <inheritdoc />
    public async Task<ParseResult<LifecycleStatusMessage>> ReadAsync(
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        byte[] content = await DocumentStreams.ReadAllAsync(stream, cancellationToken).ConfigureAwait(false);

        using var buffered = new MemoryStream(content, writable: false);
        return Read(buffered);
    }

    private LifecycleStatusMessage ReadMessage(XElement root, DiagnosticCollector diagnostics)
    {
        var mapped = new HashSet<XElement>();
        var owners = new Dictionary<XElement, InvoiceNode>();
        var values = new UblValueReader(diagnostics, mapped);
        var message = new LifecycleStatusMessage();

        message.SpecificationIdentifier = ProfileIdentifier.FromDocument(
            Take(root, UblNames.Cbc + "CustomizationID", mapped)?.Value);
        message.BusinessProcessType = values.ReadIdentifier(Take(root, UblNames.Cbc + "ProfileID", mapped));
        message.Identifier = values.ReadIdentifier(Take(root, UblNames.Cbc + "ID", mapped));
        message.IssuedAt = UblMoment.Read(
            Take(root, UblNames.Cbc + "IssueDate", mapped),
            Take(root, UblNames.Cbc + "IssueTime", mapped));
        message.Note = values.ReadText(Take(root, UblNames.Cbc + "Note", mapped));

        message.Sender = ReadParty(Take(root, UblNames.Cac + "SenderParty", mapped), values, mapped, owners);
        if (ReadParty(Take(root, UblNames.Cac + "ReceiverParty", mapped), values, mapped, owners) is { } receiver)
        {
            message.Recipients.Add(receiver);
        }

        foreach (XElement response in TakeAll(root, UblNames.Cac + "DocumentResponse", mapped))
        {
            ReferencedDocumentStatus status = ReadDocumentResponse(response, values, mapped, owners);
            owners[response] = status;
            message.References.Add(status);
        }

        UblExtensions.KeepEverythingElse(root, message, mapped, owners, diagnostics);

        ProfileResolution resolution = _profiles.Resolve(message.SpecificationIdentifier, DocumentSyntax.Ubl);
        foreach (Diagnostic diagnostic in resolution.Diagnostics)
        {
            diagnostics.Add(diagnostic);
        }

        message.Profile = resolution;
        message.Diagnostics = diagnostics.Diagnostics;
        return message;
    }

    private static ReferencedDocumentStatus ReadDocumentResponse(
        XElement element,
        UblValueReader values,
        HashSet<XElement> mapped,
        Dictionary<XElement, InvoiceNode> owners)
    {
        var status = new ReferencedDocumentStatus();

        if (Take(element, UblNames.Cac + "Response", mapped) is { } response)
        {
            owners[response] = status;
            ReadResponse(response, values, mapped, status.StatusDetails, owners,
                code => status.ProcessConditionCode = code,
                text => status.ProcessCondition = text);
            status.EffectiveDate = values.ReadDate(Take(response, UblNames.Cbc + "EffectiveDate", mapped));
        }

        if (Take(element, UblNames.Cac + "DocumentReference", mapped) is { } reference)
        {
            owners[reference] = status;
            status.DocumentIdentifier = values.ReadIdentifier(Take(reference, UblNames.Cbc + "ID", mapped));
            status.DocumentIssueDate = values.ReadDate(Take(reference, UblNames.Cbc + "IssueDate", mapped));
            status.DocumentTypeCode = values.ReadCode(Take(reference, UblNames.Cbc + "DocumentTypeCode", mapped));
            status.DocumentVersion = values.ReadIdentifier(Take(reference, UblNames.Cbc + "VersionID", mapped));
        }

        status.Issuer = ReadParty(Take(element, UblNames.Cac + "IssuerParty", mapped), values, mapped, owners);
        status.Recipient = ReadParty(Take(element, UblNames.Cac + "RecipientParty", mapped), values, mapped, owners);

        foreach (XElement line in TakeAll(element, UblNames.Cac + "LineResponse", mapped))
        {
            ReferencedLineStatus lineStatus = ReadLineResponse(line, values, mapped, owners);
            owners[line] = lineStatus;
            status.LineStatuses.Add(lineStatus);
        }

        return status;
    }

    private static ReferencedLineStatus ReadLineResponse(
        XElement element,
        UblValueReader values,
        HashSet<XElement> mapped,
        Dictionary<XElement, InvoiceNode> owners)
    {
        var status = new ReferencedLineStatus();

        if (Take(element, UblNames.Cac + "LineReference", mapped) is { } reference)
        {
            owners[reference] = status;
            status.LineIdentifier = values.ReadIdentifier(Take(reference, UblNames.Cbc + "LineID", mapped));
        }

        if (Take(element, UblNames.Cac + "Response", mapped) is { } response)
        {
            owners[response] = status;
            ReadResponse(response, values, mapped, status.StatusDetails, owners,
                code => status.ProcessConditionCode = code,
                text => status.ProcessCondition = text);
        }

        return status;
    }

    /// <summary>
    /// A response and the statuses hanging off it, which are how a sender says <em>why</em> and <em>what
    /// now</em>.
    /// </summary>
    /// <remarks>
    /// UBL repeats <c>cac:Status</c> rather than nesting the two, and tells a reason from a requested action
    /// by the code list the value is drawn from: a status carrying <c>OPStatusAction</c> is what the sender
    /// wants done, anything else is why. One detail per status keeps the document's own shape, so a message
    /// read and written back has the same number of them.
    /// </remarks>
    private static void ReadResponse(
        XElement response,
        UblValueReader values,
        HashSet<XElement> mapped,
        List<DocumentStatusDetail> details,
        Dictionary<XElement, InvoiceNode> owners,
        Action<CodeField> setCode,
        Action<TextField> setText)
    {
        setCode(values.ReadCode(Take(response, UblNames.Cbc + "ResponseCode", mapped)));
        setText(values.ReadText(Take(response, UblNames.Cbc + "Description", mapped)));

        foreach (XElement status in TakeAll(response, UblNames.Cac + "Status", mapped))
        {
            var detail = new DocumentStatusDetail();
            owners[status] = detail;

            CodeField code = values.ReadCode(Take(status, UblNames.Cbc + "StatusReasonCode", mapped));
            TextField reason = values.ReadText(Take(status, UblNames.Cbc + "StatusReason", mapped));

            if (IsRequestedAction(code))
            {
                detail.RequestedActionCode = code;
                detail.RequestedAction = reason;
            }
            else
            {
                detail.ReasonCode = code;
                detail.Reason = reason;
            }

            foreach (XElement condition in TakeAll(status, UblNames.Cac + "Condition", mapped))
            {
                var characteristic = new DocumentStatusCharacteristic
                {
                    Identifier = values.ReadIdentifier(Take(condition, UblNames.Cbc + "AttributeID", mapped)),
                    ValueText = values.ReadText(Take(condition, UblNames.Cbc + "Description", mapped)),
                };

                owners[condition] = characteristic;
                detail.Characteristics.Add(characteristic);
            }

            details.Add(detail);
        }
    }

    private static bool IsRequestedAction(CodeField code) =>
        string.Equals(code.ListId, UblApplicationResponseNames.ActionCodeList, StringComparison.OrdinalIgnoreCase);

    private static StatusParty? ReadParty(
        XElement? element,
        UblValueReader values,
        HashSet<XElement> mapped,
        Dictionary<XElement, InvoiceNode> owners)
    {
        if (element is null)
        {
            return null;
        }

        var party = new StatusParty();
        owners[element] = party;

        party.ElectronicAddress = values.ReadIdentifier(Take(element, UblNames.Cbc + "EndpointID", mapped));

        if (Take(element, UblNames.Cac + "PartyIdentification", mapped) is { } identification)
        {
            owners[identification] = party;
            party.GlobalIdentifier = values.ReadIdentifier(Take(identification, UblNames.Cbc + "ID", mapped));
        }

        if (Take(element, UblNames.Cac + "PartyName", mapped) is { } name)
        {
            owners[name] = party;
            party.TradingName = values.ReadText(Take(name, UblNames.Cbc + "Name", mapped));
        }

        if (Take(element, UblNames.Cac + "PartyLegalEntity", mapped) is { } legal)
        {
            owners[legal] = party;
            party.Name = values.ReadText(Take(legal, UblNames.Cbc + "RegistrationName", mapped));
        }

        if (Take(element, UblNames.Cac + "Contact", mapped) is { } contact)
        {
            var mappedContact = new Contact
            {
                Name = values.ReadText(Take(contact, UblNames.Cbc + "Name", mapped)),
                Telephone = values.ReadText(Take(contact, UblNames.Cbc + "Telephone", mapped)),
                Email = values.ReadText(Take(contact, UblNames.Cbc + "ElectronicMail", mapped)),
            };

            owners[contact] = mappedContact;
            party.Contact = mappedContact;
        }

        return party;
    }

    private static XElement? Take(XElement parent, XName name, HashSet<XElement> mapped)
    {
        XElement? element = parent.Element(name);
        if (element is not null)
        {
            mapped.Add(element);
        }

        return element;
    }

    private static List<XElement> TakeAll(XElement parent, XName name, HashSet<XElement> mapped)
    {
        List<XElement> elements = [.. parent.Elements(name)];
        foreach (XElement element in elements)
        {
            mapped.Add(element);
        }

        return elements;
    }
}
