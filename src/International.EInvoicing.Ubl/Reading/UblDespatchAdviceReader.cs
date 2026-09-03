using System.Globalization;
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
/// Reads a UBL <c>DespatchAdvice</c> — what was actually sent — into the canonical model.
/// </summary>
/// <remarks>
/// The document an invoice is reconciled against: an invoice says what is owed, an order says what was
/// asked for, and only this says what left the warehouse. Reading never throws on the document, and an
/// element outside the model is kept verbatim rather than dropped.
/// </remarks>
public sealed class UblDespatchAdviceReader : IDocumentReader<DespatchAdvice>
{
    private readonly EInvoicingOptions _options;
    private readonly IProfileResolver _profiles;

    /// <summary>Creates a reader using the supplied options and profile resolver.</summary>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    public UblDespatchAdviceReader(EInvoicingOptions options, IProfileResolver profiles)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(profiles);

        _options = options;
        _profiles = profiles;
    }

    /// <inheritdoc />
    public DocumentSyntax Syntax => DocumentSyntax.Ubl;

    /// <summary>Reads a despatch advice from a stream. The stream is left open.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <c>null</c>.</exception>
    public ParseResult<DespatchAdvice> Read(Stream stream)
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

            return diagnostics.ToResult<DespatchAdvice>(null);
        }

        return diagnostics.ToResult(ReadDespatchAdvice(root, diagnostics));
    }

    /// <summary>Reads a despatch advice from XML text.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="xml"/> is <c>null</c>.</exception>
    public ParseResult<DespatchAdvice> Read(string xml)
    {
        ArgumentNullException.ThrowIfNull(xml);

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(xml));
        return Read(stream);
    }

    /// <inheritdoc />
    public async Task<ParseResult<DespatchAdvice>> ReadAsync(
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        byte[] content = await DocumentStreams.ReadAllAsync(stream, cancellationToken).ConfigureAwait(false);

        using var buffered = new MemoryStream(content, writable: false);
        return Read(buffered);
    }

    private DespatchAdvice ReadDespatchAdvice(XElement root, DiagnosticCollector diagnostics)
    {
        var mapped = new HashSet<XElement>();
        var owners = new Dictionary<XElement, InvoiceNode>();
        var values = new UblValueReader(diagnostics, mapped);
        var advice = new DespatchAdvice();

        advice.SpecificationIdentifier = ProfileIdentifier.FromDocument(
            Take(root, UblNames.Cbc + "CustomizationID", mapped)?.Value);
        advice.BusinessProcessType = values.ReadIdentifier(Take(root, UblNames.Cbc + "ProfileID", mapped));
        advice.Number = values.ReadIdentifier(Take(root, UblNames.Cbc + "ID", mapped));
        advice.IssuedAt = UblMoment.Read(
            Take(root, UblNames.Cbc + "IssueDate", mapped),
            Take(root, UblNames.Cbc + "IssueTime", mapped));
        advice.TypeCode = values.ReadCode(Take(root, UblNames.Cbc + "DespatchAdviceTypeCode", mapped));
        advice.Note = values.ReadText(Take(root, UblNames.Cbc + "Note", mapped));

        if (Take(root, UblNames.Cac + "OrderReference", mapped) is { } order)
        {
            owners[order] = advice;
            advice.OrderReference = values.ReadIdentifier(Take(order, UblNames.Cbc + "ID", mapped));
        }

        advice.DespatchParty = ReadWrappedParty(root, "DespatchSupplierParty", values, mapped, owners);
        advice.DeliveryParty = ReadWrappedParty(root, "DeliveryCustomerParty", values, mapped, owners);
        advice.BuyerParty = ReadWrappedParty(root, "BuyerCustomerParty", values, mapped, owners);
        advice.SellerParty = ReadWrappedParty(root, "SellerSupplierParty", values, mapped, owners);
        advice.OriginatorParty = ReadWrappedParty(root, "OriginatorCustomerParty", values, mapped, owners);

        foreach (XElement reference in TakeAll(root, UblNames.Cac + "AdditionalDocumentReference", mapped))
        {
            advice.AdditionalDocuments.Add(UblAttachments.Read(reference, values, mapped, owners, _options.Limits));
        }

        advice.Shipment = ReadShipment(Take(root, UblNames.Cac + "Shipment", mapped), values, mapped, owners);

        foreach (XElement line in TakeAll(root, UblNames.Cac + "DespatchLine", mapped))
        {
            if (Limits.Exceeded(advice.Lines.Count, _options.Limits.MaxDocumentLines))
            {
                diagnostics.Add(Limits.TooMany(_options.Limits.MaxDocumentLines, "despatch lines"));
                break;
            }

            DespatchLine mappedLine = ReadLine(line, values, mapped, owners, _options.Limits);
            owners[line] = mappedLine;
            advice.Lines.Add(mappedLine);
        }

        UblExtensions.KeepEverythingElse(root, advice, mapped, owners, diagnostics);

        ProfileResolution resolution = _profiles.Resolve(advice.SpecificationIdentifier, DocumentSyntax.Ubl);
        foreach (Diagnostic diagnostic in resolution.Diagnostics)
        {
            diagnostics.Add(diagnostic);
        }

        advice.Profile = resolution;
        advice.Diagnostics = diagnostics.Diagnostics;
        return advice;
    }

    /// <summary>
    /// A party inside the role element that names it — <c>cac:DespatchSupplierParty/cac:Party</c>.
    /// </summary>
    /// <remarks>
    /// The delivery role carries a contact of its own, beside the party rather than inside it, and it is the
    /// person the driver calls. It is read onto the party so a caller has one place to look.
    /// </remarks>
    private static Party? ReadWrappedParty(
        XElement root,
        string role,
        UblValueReader values,
        HashSet<XElement> mapped,
        Dictionary<XElement, InvoiceNode> owners)
    {
        if (Take(root, UblNames.Cac + role, mapped) is not { } wrapper)
        {
            return null;
        }

        Party? party = UblParties.Read(Take(wrapper, UblNames.Cac + "Party", mapped), values, mapped, owners);

        if (party is not null)
        {
            owners[wrapper] = party;

            if (Take(wrapper, UblNames.Cac + "DeliveryContact", mapped) is { } contact)
            {
                party.Contact ??= UblParties.ReadContact(contact, values, mapped, owners);
            }
        }

        return party;
    }

    private static Shipment? ReadShipment(
        XElement? element,
        UblValueReader values,
        HashSet<XElement> mapped,
        Dictionary<XElement, InvoiceNode> owners)
    {
        if (element is null)
        {
            return null;
        }

        var shipment = new Shipment
        {
            Identifier = values.ReadIdentifier(Take(element, UblNames.Cbc + "ID", mapped)),
            Information = values.ReadText(Take(element, UblNames.Cbc + "Information", mapped)),
            GrossWeight = values.ReadQuantity(Take(element, UblNames.Cbc + "GrossWeightMeasure", mapped)),
            GrossVolume = values.ReadQuantity(Take(element, UblNames.Cbc + "GrossVolumeMeasure", mapped)),
            HandlingUnitCount = values.ReadQuantity(
                Take(element, UblNames.Cbc + "TotalTransportHandlingUnitQuantity", mapped)),
        };

        owners[element] = shipment;

        if (Take(element, UblNames.Cac + "ShipmentStage", mapped) is { } stage)
        {
            owners[stage] = shipment;
            shipment.TransportModeCode = values.ReadCode(Take(stage, UblNames.Cbc + "TransportModeCode", mapped));
        }

        if (Take(element, UblNames.Cac + "Consignment", mapped) is { } consignment)
        {
            owners[consignment] = shipment;
            shipment.ConsignmentIdentifier = values.ReadIdentifier(Take(consignment, UblNames.Cbc + "ID", mapped));
            shipment.ConsignmentInformation = values.ReadText(
                Take(consignment, UblNames.Cbc + "Information", mapped));
            shipment.Carrier = UblParties.Read(
                Take(consignment, UblNames.Cac + "CarrierParty", mapped), values, mapped, owners);
        }

        if (Take(element, UblNames.Cac + "Delivery", mapped) is { } delivery)
        {
            owners[delivery] = shipment;
            shipment.TrackingIdentifier = values.ReadIdentifier(Take(delivery, UblNames.Cbc + "TrackingID", mapped));

            if (Take(delivery, UblNames.Cac + "EstimatedDeliveryPeriod", mapped) is { } period)
            {
                owners[period] = shipment;
                shipment.EstimatedDeliveryFrom = UblMoment.Read(
                    Take(period, UblNames.Cbc + "StartDate", mapped),
                    Take(period, UblNames.Cbc + "StartTime", mapped));
                shipment.EstimatedDeliveryUntil = UblMoment.Read(
                    Take(period, UblNames.Cbc + "EndDate", mapped),
                    Take(period, UblNames.Cbc + "EndTime", mapped));
            }

            if (Take(delivery, UblNames.Cac + "Despatch", mapped) is { } despatch)
            {
                owners[despatch] = shipment;
                shipment.DespatchedAt = UblMoment.Read(
                    Take(despatch, UblNames.Cbc + "ActualDespatchDate", mapped),
                    Take(despatch, UblNames.Cbc + "ActualDespatchTime", mapped));
                shipment.DespatchAddress = UblParties.ReadAddress(
                    Take(despatch, UblNames.Cac + "DespatchAddress", mapped), values, mapped, owners);
            }
        }

        foreach (XElement unit in TakeAll(element, UblNames.Cac + "TransportHandlingUnit", mapped))
        {
            shipment.HandlingUnits.Add(ReadHandlingUnit(unit, values, mapped, owners));
        }

        return shipment;
    }

    private static DespatchLine ReadLine(
        XElement element,
        UblValueReader values,
        HashSet<XElement> mapped,
        Dictionary<XElement, InvoiceNode> owners,
        DocumentLimits limits)
    {
        var line = new DespatchLine
        {
            Identifier = values.ReadIdentifier(Take(element, UblNames.Cbc + "ID", mapped)),
            Note = values.ReadText(Take(element, UblNames.Cbc + "Note", mapped)),
            DeliveredQuantity = values.ReadQuantity(Take(element, UblNames.Cbc + "DeliveredQuantity", mapped)),
            OutstandingQuantity = values.ReadQuantity(Take(element, UblNames.Cbc + "OutstandingQuantity", mapped)),
            OutstandingReason = values.ReadText(Take(element, UblNames.Cbc + "OutstandingReason", mapped)),
        };

        if (Take(element, UblNames.Cac + "OrderLineReference", mapped) is { } reference)
        {
            owners[reference] = line;
            line.OrderLineReference = values.ReadIdentifier(Take(reference, UblNames.Cbc + "LineID", mapped));
            line.SalesOrderLineReference = values.ReadIdentifier(
                Take(reference, UblNames.Cbc + "SalesOrderLineID", mapped));

            if (Take(reference, UblNames.Cac + "OrderReference", mapped) is { } order)
            {
                owners[order] = line;
                line.OrderReference = values.ReadIdentifier(Take(order, UblNames.Cbc + "ID", mapped));
            }
        }

        foreach (XElement attached in TakeAll(element, UblNames.Cac + "DocumentReference", mapped))
        {
            line.AdditionalDocuments.Add(UblAttachments.Read(attached, values, mapped, owners, limits));
        }

        line.Item = ReadItem(Take(element, UblNames.Cac + "Item", mapped), values, mapped, owners);

        // A despatch line's own shipment says how these goods are packed, not where they are going.
        line.Packaging = ReadShipment(Take(element, UblNames.Cac + "Shipment", mapped), values, mapped, owners);

        return line;
    }

    private static DespatchItem? ReadItem(
        XElement? element,
        UblValueReader values,
        HashSet<XElement> mapped,
        Dictionary<XElement, InvoiceNode> owners)
    {
        if (element is null)
        {
            return null;
        }

        var item = new DespatchItem
        {
            Name = values.ReadText(Take(element, UblNames.Cbc + "Name", mapped)),
            Description = values.ReadText(Take(element, UblNames.Cbc + "Description", mapped)),
        };

        owners[element] = item;

        (item.BuyerIdentifier, item.BuyerIdentifierExtension) =
            ReadItemIdentification(element, "BuyersItemIdentification", values, mapped, owners, item);
        (item.SellerIdentifier, item.SellerIdentifierExtension) =
            ReadItemIdentification(element, "SellersItemIdentification", values, mapped, owners, item);
        (item.StandardIdentifier, item.StandardIdentifierExtension) =
            ReadItemIdentification(element, "StandardItemIdentification", values, mapped, owners, item);

        foreach (XElement classification in TakeAll(element, UblNames.Cac + "CommodityClassification", mapped))
        {
            owners[classification] = item;
            item.ClassificationCodes.Add(
                values.ReadCode(Take(classification, UblNames.Cbc + "ItemClassificationCode", mapped)));
        }

        if (Take(element, UblNames.Cac + "HazardousItem", mapped) is { } hazard)
        {
            owners[hazard] = item;
            item.DangerousGoodsCode = values.ReadCode(Take(hazard, UblNames.Cbc + "UNDGCode", mapped));
            item.HazardClass = values.ReadCode(Take(hazard, UblNames.Cbc + "HazardClassID", mapped));
        }

        foreach (XElement property in TakeAll(element, UblNames.Cac + "AdditionalItemProperty", mapped))
        {
            item.Characteristics.Add(ReadCharacteristic(property, values, mapped, owners));
        }

        foreach (XElement instance in TakeAll(element, UblNames.Cac + "ItemInstance", mapped))
        {
            item.Instances.Add(ReadInstance(instance, values, mapped, owners));
        }

        return item;
    }

    private static ItemCharacteristic ReadCharacteristic(
        XElement element,
        UblValueReader values,
        HashSet<XElement> mapped,
        Dictionary<XElement, InvoiceNode> owners)
    {
        var characteristic = new ItemCharacteristic
        {
            Name = values.ReadText(Take(element, UblNames.Cbc + "Name", mapped)),
            Value = values.ReadText(Take(element, UblNames.Cbc + "Value", mapped)),
        };

        owners[element] = characteristic;
        return characteristic;
    }

    /// <summary>An article number and the extension that qualifies it, which UBL keeps in one wrapper.</summary>
    private static (IdentifierField Identifier, IdentifierField Extension) ReadItemIdentification(
        XElement parent,
        string wrapper,
        UblValueReader values,
        HashSet<XElement> mapped,
        Dictionary<XElement, InvoiceNode> owners,
        InvoiceNode owner)
    {
        if (Take(parent, UblNames.Cac + wrapper, mapped) is not { } element)
        {
            return (IdentifierField.Unset, IdentifierField.Unset);
        }

        owners[element] = owner;

        return (
            values.ReadIdentifier(Take(element, UblNames.Cbc + "ID", mapped)),
            values.ReadIdentifier(Take(element, UblNames.Cbc + "ExtendedID", mapped)));
    }

    private static ItemInstance ReadInstance(
        XElement element,
        UblValueReader values,
        HashSet<XElement> mapped,
        Dictionary<XElement, InvoiceNode> owners)
    {
        var instance = new ItemInstance
        {
            ProductTraceIdentifier = values.ReadIdentifier(Take(element, UblNames.Cbc + "ProductTraceID", mapped)),
            ManufactureDate = values.ReadDate(Take(element, UblNames.Cbc + "ManufactureDate", mapped)),
            BestBeforeDate = values.ReadDate(Take(element, UblNames.Cbc + "BestBeforeDate", mapped)),
            SerialIdentifier = values.ReadIdentifier(Take(element, UblNames.Cbc + "SerialID", mapped)),
        };

        owners[element] = instance;

        foreach (XElement property in TakeAll(element, UblNames.Cac + "AdditionalItemProperty", mapped))
        {
            instance.Characteristics.Add(ReadCharacteristic(property, values, mapped, owners));
        }

        if (Take(element, UblNames.Cac + "LotIdentification", mapped) is { } lot)
        {
            owners[lot] = instance;
            instance.LotIdentifier = values.ReadIdentifier(Take(lot, UblNames.Cbc + "LotNumberID", mapped));
            instance.LotExpiryDate = values.ReadDate(Take(lot, UblNames.Cbc + "ExpiryDate", mapped));
        }

        return instance;
    }

    private static TransportHandlingUnit ReadHandlingUnit(
        XElement element,
        UblValueReader values,
        HashSet<XElement> mapped,
        Dictionary<XElement, InvoiceNode> owners)
    {
        var unit = new TransportHandlingUnit
        {
            Identifier = values.ReadIdentifier(Take(element, UblNames.Cbc + "ID", mapped)),
            TypeCode = values.ReadCode(Take(element, UblNames.Cbc + "TransportHandlingUnitTypeCode", mapped)),
            Hazardous = values.ReadIndicator(Take(element, UblNames.Cbc + "HazardousRiskIndicator", mapped)),
            ShippingMarks = values.ReadText(Take(element, UblNames.Cbc + "ShippingMarks", mapped)),
        };

        owners[element] = unit;

        if (Take(element, UblNames.Cac + "MeasurementDimension", mapped) is { } dimension)
        {
            owners[dimension] = unit;
            unit.MeasuredAttribute = values.ReadCode(Take(dimension, UblNames.Cbc + "AttributeID", mapped));
            unit.Measure = values.ReadQuantity(Take(dimension, UblNames.Cbc + "Measure", mapped));
        }

        foreach (XElement package in TakeAll(element, UblNames.Cac + "Package", mapped))
        {
            var mappedPackage = new Package
            {
                Identifier = values.ReadIdentifier(Take(package, UblNames.Cbc + "ID", mapped)),
                PackagingTypeCode = values.ReadCode(Take(package, UblNames.Cbc + "PackagingTypeCode", mapped)),
            };

            owners[package] = mappedPackage;
            unit.Packages.Add(mappedPackage);
        }

        return unit;
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
