using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using International.EInvoicing.Configuration;
using International.EInvoicing.Diagnostics;
using International.EInvoicing.Documents;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;
using International.EInvoicing.Values;
using International.EInvoicing.Xml;

namespace International.EInvoicing.Cdar.Reading;

/// <summary>
/// Reads a UN/CEFACT lifecycle message into the canonical model.
/// </summary>
/// <remarks>
/// The reader is generic on purpose. A national profiling restricts the message and gives meaning to its
/// codes without changing its shape, so a profiling this library does not know still parses: the codes come
/// back uninterpreted, and the downgrade is reported rather than hidden.
/// </remarks>
public sealed class CdarReader : IDocumentReader<LifecycleStatusMessage>
{
    private readonly EInvoicingOptions _options;
    private readonly IProfileResolver _profiles;

    /// <summary>Creates a reader using the supplied options and profile resolver.</summary>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    public CdarReader(EInvoicingOptions options, IProfileResolver profiles)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(profiles);

        _options = options;
        _profiles = profiles;
    }

    /// <inheritdoc />
    public DocumentSyntax Syntax => DocumentSyntax.Cdar;

    /// <summary>Reads a lifecycle message from a stream. The stream is left open.</summary>
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
        catch (XmlException exception)
        {
            diagnostics.Add(Diagnostic.Create(CdarDiagnostics.MalformedDocument, exception.Message) with
            {
                Location = new SourceLocation(null, exception.LineNumber, exception.LinePosition),
            });

            return diagnostics.ToResult<LifecycleStatusMessage>(null);
        }

        return diagnostics.ToResult(ReadMessage(root, diagnostics));
    }

    /// <summary>Reads a lifecycle message from XML text.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="xml"/> is <c>null</c>.</exception>
    public ParseResult<LifecycleStatusMessage> Read(string xml)
    {
        ArgumentNullException.ThrowIfNull(xml);

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(xml));
        return Read(stream);
    }

    /// <inheritdoc />
    public async Task<ParseResult<LifecycleStatusMessage>> ReadAsync(Stream stream, CancellationToken cancellationToken = default)
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
        var values = new CdarValueReader(diagnostics, mapped);
        var message = new LifecycleStatusMessage();

        XElement? context = In(values, root, CdarNames.Rsm + "ExchangedDocumentContext");
        message.BusinessProcessType = values.ReadIdentifier(
            In(values, In(values, context, CdarNames.Ram + "BusinessProcessSpecifiedDocumentContextParameter"), CdarNames.Ram + "ID"));
        message.SpecificationIdentifier = ProfileIdentifier.FromDocument(
            In(values, In(values, context, CdarNames.Ram + "GuidelineSpecifiedDocumentContextParameter"), CdarNames.Ram + "ID")?.Value);

        ReadDocument(In(values, root, CdarNames.Rsm + "ExchangedDocument"), message, values, owners);
        ReadAcknowledgement(In(values, root, CdarNames.Rsm + "AcknowledgementDocument"), message, values, owners);

        KeepEverythingElse(root, message, mapped, owners, diagnostics);

        ProfileResolution resolution = _profiles.Resolve(message.SpecificationIdentifier, DocumentSyntax.Cdar);
        foreach (Diagnostic diagnostic in resolution.Diagnostics)
        {
            diagnostics.Add(diagnostic);
        }

        message.Profile = resolution;
        message.Diagnostics = diagnostics.Diagnostics;
        return message;
    }

    private static void ReadDocument(
        XElement? document,
        LifecycleStatusMessage message,
        CdarValueReader values,
        Dictionary<XElement, InvoiceNode> owners)
    {
        if (document is null)
        {
            return;
        }

        message.Identifier = values.ReadIdentifier(In(values, document, CdarNames.Ram + "ID"));
        message.Name = values.ReadText(In(values, document, CdarNames.Ram + "Name"));
        message.IssuedAt = values.ReadDateTime(In(values, document, CdarNames.Ram + "IssueDateTime"));

        message.Sender = ReadParty(In(values, document, CdarNames.Ram + "SenderTradeParty"), values, owners);
        message.Issuer = ReadParty(In(values, document, CdarNames.Ram + "IssuerTradeParty"), values, owners);

        foreach (XElement recipient in AllIn(values, document, CdarNames.Ram + "RecipientTradeParty"))
        {
            if (ReadParty(recipient, values, owners) is { } party)
            {
                message.Recipients.Add(party);
            }
        }
    }

    private static void ReadAcknowledgement(
        XElement? acknowledgement,
        LifecycleStatusMessage message,
        CdarValueReader values,
        Dictionary<XElement, InvoiceNode> owners)
    {
        if (acknowledgement is null)
        {
            return;
        }

        message.CoversMultipleDocuments = values.ReadIndicator(
            In(values, acknowledgement, CdarNames.Ram + "MultipleReferencesIndicator"));
        message.TypeCode = values.ReadCode(In(values, acknowledgement, CdarNames.Ram + "TypeCode"));
        message.StatusIssuedAt = values.ReadDateTime(In(values, acknowledgement, CdarNames.Ram + "IssueDateTime"));

        foreach (XElement reference in AllIn(values, acknowledgement, CdarNames.Ram + "ReferenceReferencedDocument"))
        {
            var status = new ReferencedDocumentStatus
            {
                DocumentIdentifier = values.ReadIdentifier(In(values, reference, CdarNames.Ram + "IssuerAssignedID")),
                StatusCode = values.ReadCode(In(values, reference, CdarNames.Ram + "StatusCode")),
                DocumentTypeCode = values.ReadCode(In(values, reference, CdarNames.Ram + "TypeCode")),
                ReceivedAt = values.ReadDateTime(In(values, reference, CdarNames.Ram + "ReceiptDateTime")),
                DocumentIssueDate = values.ReadDate(In(values, reference, CdarNames.Ram + "FormattedIssueDateTime")),
                ProcessConditionCode = values.ReadCode(In(values, reference, CdarNames.Ram + "ProcessConditionCode")),
                ProcessCondition = values.ReadText(In(values, reference, CdarNames.Ram + "ProcessCondition")),
                Reason = values.ReadText(In(values, reference, CdarNames.Ram + "Reason")),
                Issuer = ReadParty(In(values, reference, CdarNames.Ram + "IssuerTradeParty"), values, owners),
            };

            foreach (XElement detail in AllIn(values, reference, CdarNames.Ram + "SpecifiedDocumentStatus"))
            {
                status.StatusDetails.Add(ReadStatusDetail(detail, values, owners));
            }

            owners[reference] = status;
            message.References.Add(status);
        }
    }

    private static DocumentStatusDetail ReadStatusDetail(
        XElement element,
        CdarValueReader values,
        Dictionary<XElement, InvoiceNode> owners)
    {
        var detail = new DocumentStatusDetail
        {
            ProcessConditionCode = values.ReadCode(In(values, element, CdarNames.Ram + "ProcessConditionCode")),
            ReasonCode = values.ReadCode(In(values, element, CdarNames.Ram + "ReasonCode")),
            Reason = values.ReadText(In(values, element, CdarNames.Ram + "Reason")),
            RequestedActionCode = values.ReadCode(In(values, element, CdarNames.Ram + "RequestedActionCode")),
            RequestedAction = values.ReadText(In(values, element, CdarNames.Ram + "RequestedAction")),
            SequenceNumber = values.ReadInteger(In(values, element, CdarNames.Ram + "SequenceNumeric")),
        };

        foreach (XElement characteristic in AllIn(values, element, CdarNames.Ram + "SpecifiedDocumentCharacteristic"))
        {
            detail.Characteristics.Add(ReadCharacteristic(characteristic, values, owners));
        }

        owners[element] = detail;
        return detail;
    }

    private static DocumentStatusCharacteristic ReadCharacteristic(
        XElement element,
        CdarValueReader values,
        Dictionary<XElement, InvoiceNode> owners)
    {
        var characteristic = new DocumentStatusCharacteristic
        {
            Identifier = values.ReadIdentifier(In(values, element, CdarNames.Ram + "ID")),
            TypeCode = values.ReadCode(In(values, element, CdarNames.Ram + "TypeCode")),
            ValueChanged = values.ReadIndicator(In(values, element, CdarNames.Ram + "ValueChangedIndicator")),
            Name = values.ReadText(In(values, element, CdarNames.Ram + "Name")),
            Location = values.ReadText(In(values, element, CdarNames.Ram + "Location")),
            ValueAmount = values.ReadAmount(In(values, element, CdarNames.Ram + "ValueAmount")),
            ValuePercent = values.ReadDecimal(In(values, element, CdarNames.Ram + "ValuePercent")),
            ValueText = values.ReadText(In(values, element, CdarNames.Ram + "ValueText")),
        };

        owners[element] = characteristic;
        return characteristic;
    }

    private static StatusParty? ReadParty(
        XElement? element,
        CdarValueReader values,
        Dictionary<XElement, InvoiceNode> owners)
    {
        if (element is null)
        {
            return null;
        }

        var party = new StatusParty
        {
            GlobalIdentifier = values.ReadIdentifier(In(values, element, CdarNames.Ram + "GlobalID")),
            Name = values.ReadText(In(values, element, CdarNames.Ram + "Name")),
            RoleCode = values.ReadCode(In(values, element, CdarNames.Ram + "RoleCode")),
            ElectronicAddress = values.ReadIdentifier(
                In(values, In(values, element, CdarNames.Ram + "URIUniversalCommunication"), CdarNames.Ram + "URIID")),
        };

        owners[element] = party;
        return party;
    }

    private static XElement? In(CdarValueReader values, XElement? parent, XName name)
    {
        XElement? child = parent?.Element(name);
        values.Consume(child);
        return child;
    }

    private static List<XElement> AllIn(CdarValueReader values, XElement? parent, XName name)
    {
        List<XElement> children = [.. parent?.Elements(name) ?? []];
        foreach (XElement child in children)
        {
            values.Consume(child);
        }

        return children;
    }

    private static void KeepEverythingElse(
        XElement source,
        InvoiceNode node,
        HashSet<XElement> mapped,
        IReadOnlyDictionary<XElement, InvoiceNode> owners,
        DiagnosticCollector diagnostics)
    {
        string? preceding = null;

        foreach (XElement element in source.Elements())
        {
            if (mapped.Contains(element))
            {
                preceding = element.Name.ToString();

                KeepEverythingElse(
                    element,
                    owners.TryGetValue(element, out InvoiceNode? owner) ? owner : node,
                    mapped,
                    owners,
                    diagnostics);
                continue;
            }

            node.Extensions.Add(new ExtensionElement(
                element.Name.NamespaceName,
                element.Name.LocalName,
                element.ToString(SaveOptions.DisableFormatting),
                CdarValueReader.LocationOf(element),
                preceding,
                source.Name.ToString()));

            diagnostics.Add(Diagnostic.Create(CdarDiagnostics.UnmappedElement, element.Name.LocalName) with
            {
                Location = CdarValueReader.LocationOf(element),
                Found = element.Name.LocalName,
                AppliedFallback = "kept verbatim as extension data",
            });
        }
    }
}

/// <summary>Turns CDAR elements into fields, marking each one as mapped so leftovers can be kept.</summary>
internal sealed class CdarValueReader(DiagnosticCollector diagnostics, HashSet<XElement> mapped)
{
    public bool Consume([NotNullWhen(true)] XElement? element)
    {
        if (element is null)
        {
            return false;
        }

        mapped.Add(element);
        return true;
    }

    public TextField ReadText(XElement? element) =>
        Consume(element) ? new TextField(element.Value, null, Source(element)) : TextField.Unset;

    public CodeField ReadCode(XElement? element) =>
        Consume(element)
            ? new CodeField(element.Value, element.Attribute("listID")?.Value, null, null, Source(element))
            : CodeField.Unset;

    public IdentifierField ReadIdentifier(XElement? element) =>
        Consume(element)
            ? new IdentifierField(element.Value, element.Attribute("schemeID")?.Value, null, null, Source(element))
            : IdentifierField.Unset;

    public IndicatorField ReadIndicator(XElement? parent)
    {
        if (!Consume(parent))
        {
            return IndicatorField.Unset;
        }

        // A characteristic writes its flag as an IndicatorString; everything else uses Indicator.
        XElement? element = parent.Element(CdarNames.Udt + "Indicator")
            ?? parent.Element(CdarNames.Udt + "IndicatorString")
            ?? parent;
        Consume(element);

        return element.Value.Trim().ToUpperInvariant() switch
        {
            "TRUE" or "1" => new IndicatorField(true, Source(element)),
            "FALSE" or "0" => new IndicatorField(false, Source(element)),
            _ => new IndicatorField(null, Source(element, Report(element, "an indicator"))),
        };
    }

    public AmountField ReadAmount(XElement? element)
    {
        if (!Consume(element))
        {
            return AmountField.Unset;
        }

        string? currency = element.Attribute("currencyID")?.Value;

        return decimal.TryParse(element.Value.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out decimal amount)
            ? new AmountField(amount, currency, Source(element))
            : new AmountField(null, currency, Source(element, Report(element, "an amount")));
    }

    public Field<decimal> ReadDecimal(XElement? element)
    {
        if (!Consume(element))
        {
            return Field<decimal>.Unset;
        }

        return decimal.TryParse(element.Value.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out decimal value)
            ? new Field<decimal>(value, Source(element))
            : new Field<decimal>(null, Source(element, Report(element, "a decimal number")));
    }

    public Field<int> ReadInteger(XElement? element)
    {
        if (!Consume(element))
        {
            return Field<int>.Unset;
        }

        return int.TryParse(element.Value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
            ? new Field<int>(value, Source(element))
            : new Field<int>(null, Source(element, Report(element, "a whole number")));
    }

    /// <summary>Reads a timestamp, which lifecycle messages express as <c>CCYYMMDDHHMMSS</c> (format 204).</summary>
    public DateTimeField ReadDateTime(XElement? parent)
    {
        if (DateString(parent) is not { } element)
        {
            return DateTimeField.Unset;
        }

        string? format = element.Attribute("format")?.Value;
        string text = element.Value.Trim();

        if (DateTime.TryParseExact(text, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTime moment))
        {
            return new DateTimeField(new DateTimeOffset(moment, TimeSpan.Zero), format, Source(element));
        }

        if (DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset parsed))
        {
            return new DateTimeField(parsed, format, Source(element));
        }

        return new DateTimeField(null, format, Source(element, Report(element, "a timestamp")));
    }

    public DateField ReadDate(XElement? parent)
    {
        if (DateString(parent) is not { } element)
        {
            return DateField.Unset;
        }

        string? format = element.Attribute("format")?.Value;
        string text = element.Value.Trim();

        return DateOnly.TryParseExact(text, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly date)
            ? new DateField(date, format, Source(element))
            : new DateField(null, format, Source(element, Report(element, "a date")));
    }

    public static SourceLocation LocationOf(XElement element)
    {
        var lineInfo = (IXmlLineInfo)element;
        return new SourceLocation(
            PathOf(element),
            lineInfo.HasLineInfo() ? lineInfo.LineNumber : 0,
            lineInfo.HasLineInfo() ? lineInfo.LinePosition : 0);
    }

    private XElement? DateString(XElement? parent)
    {
        if (!Consume(parent))
        {
            return null;
        }

        XElement? element = parent.Element(CdarNames.Udt + "DateTimeString")
            ?? parent.Element(CdarNames.Qdt + "DateTimeString");

        return Consume(element) ? element : null;
    }

    private static string PathOf(XElement element)
    {
        var segments = new Stack<string>();
        for (XElement? current = element; current is not null; current = current.Parent)
        {
            segments.Push(current.Name.LocalName);
        }

        return "/" + string.Join('/', segments);
    }

    private static FieldSource Source(XElement element, Diagnostic? diagnostic = null) =>
        new(element.Value, LocationOf(element), diagnostic);

    private Diagnostic? Report(XElement element, string expected)
    {
        Diagnostic diagnostic = Diagnostic.Create(DiagnosticCodes.InvalidValue, element.Value.Trim(), expected) with
        {
            Location = LocationOf(element),
            Expected = expected,
            Found = element.Value.Trim(),
            AppliedFallback = "raw text preserved; typed value is null",
        };

        return diagnostics.Add(diagnostic);
    }
}
