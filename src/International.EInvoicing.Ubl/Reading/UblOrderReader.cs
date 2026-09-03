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
/// Reads a UBL <c>Order</c> — what a buyer asked for — into the canonical model.
/// </summary>
/// <remarks>
/// The first document of the post-award chain. A despatch advice says what was sent of it and an invoice
/// says what is owed for it, so reading all three is what lets a buyer check the second two against the
/// first. Reading never throws, and an element outside the model is kept verbatim rather than dropped.
/// </remarks>
public sealed class UblOrderReader : IDocumentReader<Order>
{
    private readonly EInvoicingOptions _options;
    private readonly IProfileResolver _profiles;

    /// <summary>Creates a reader using the supplied options and profile resolver.</summary>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    public UblOrderReader(EInvoicingOptions options, IProfileResolver profiles)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(profiles);

        _options = options;
        _profiles = profiles;
    }

    /// <inheritdoc />
    public DocumentSyntax Syntax => DocumentSyntax.Ubl;

    /// <summary>Reads an order from a stream. The stream is left open.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <c>null</c>.</exception>
    public ParseResult<Order> Read(Stream stream)
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

            return diagnostics.ToResult<Order>(null);
        }

        return diagnostics.ToResult(ReadOrder(root, diagnostics));
    }

    /// <summary>Reads an order from XML text.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="xml"/> is <c>null</c>.</exception>
    public ParseResult<Order> Read(string xml)
    {
        ArgumentNullException.ThrowIfNull(xml);

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(xml));
        return Read(stream);
    }

    /// <inheritdoc />
    public async Task<ParseResult<Order>> ReadAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        byte[] content = await DocumentStreams.ReadAllAsync(stream, cancellationToken).ConfigureAwait(false);

        using var buffered = new MemoryStream(content, writable: false);
        return Read(buffered);
    }

    private Order ReadOrder(XElement root, DiagnosticCollector diagnostics)
    {
        var mapped = new HashSet<XElement>();
        var owners = new Dictionary<XElement, InvoiceNode>();
        var values = new UblValueReader(diagnostics, mapped);
        var order = new Order();

        order.SpecificationIdentifier = ProfileIdentifier.FromDocument(
            Take(root, UblNames.Cbc + "CustomizationID", mapped)?.Value);
        order.BusinessProcessType = values.ReadIdentifier(Take(root, UblNames.Cbc + "ProfileID", mapped));
        order.Number = values.ReadIdentifier(Take(root, UblNames.Cbc + "ID", mapped));
        order.SalesOrderNumber = values.ReadIdentifier(Take(root, UblNames.Cbc + "SalesOrderID", mapped));
        order.IssuedAt = UblMoment.Read(
            Take(root, UblNames.Cbc + "IssueDate", mapped),
            Take(root, UblNames.Cbc + "IssueTime", mapped));
        order.TypeCode = values.ReadCode(Take(root, UblNames.Cbc + "OrderTypeCode", mapped));
        order.Note = values.ReadText(Take(root, UblNames.Cbc + "Note", mapped));
        order.CurrencyCode = values.ReadCode(Take(root, UblNames.Cbc + "DocumentCurrencyCode", mapped));
        order.BuyerReference = values.ReadText(Take(root, UblNames.Cbc + "CustomerReference", mapped));
        order.AccountingReference = values.ReadText(Take(root, UblNames.Cbc + "AccountingCost", mapped));
        order.ValidityPeriod = ReadPeriod(Take(root, UblNames.Cac + "ValidityPeriod", mapped), values, mapped, owners);

        order.QuotationReference = Reference(root, "QuotationDocumentReference", values, mapped, owners, order);
        order.OrderReference = Reference(root, "OrderDocumentReference", values, mapped, owners, order);
        order.OriginatorReference = Reference(root, "OriginatorDocumentReference", values, mapped, owners, order);
        order.CatalogueReference = Reference(root, "CatalogueReference", values, mapped, owners, order);
        order.ContractReference = Reference(root, "Contract", values, mapped, owners, order);
        order.ProjectReference = Reference(root, "ProjectReference", values, mapped, owners, order);

        foreach (XElement attached in TakeAll(root, UblNames.Cac + "AdditionalDocumentReference", mapped))
        {
            order.AdditionalDocuments.Add(
                UblAttachments.Read(attached, values, mapped, owners, _options.Limits));
        }

        order.Buyer = WrappedParty(root, "BuyerCustomerParty", values, mapped, owners);
        order.Seller = WrappedParty(root, "SellerSupplierParty", values, mapped, owners);
        order.Originator = WrappedParty(root, "OriginatorCustomerParty", values, mapped, owners);
        order.Invoicee = WrappedParty(root, "AccountingCustomerParty", values, mapped, owners);

        order.Delivery = ReadDelivery(Take(root, UblNames.Cac + "Delivery", mapped), values, mapped, owners);
        ReadDeliveryTerms(Take(root, UblNames.Cac + "DeliveryTerms", mapped), order, values, mapped, owners);

        if (Take(root, UblNames.Cac + "PaymentTerms", mapped) is { } terms)
        {
            owners[terms] = order;
            order.PaymentTerms = values.ReadText(Take(terms, UblNames.Cbc + "Note", mapped));
        }

        foreach (XElement allowance in TakeAll(root, UblNames.Cac + "AllowanceCharge", mapped))
        {
            order.AllowancesAndCharges.Add(ReadAllowanceCharge(allowance, values, mapped, owners));
        }

        if (Take(root, UblNames.Cac + "TaxTotal", mapped) is { } tax)
        {
            owners[tax] = order;
            order.TaxAmount = values.ReadAmount(Take(tax, UblNames.Cbc + "TaxAmount", mapped));
        }

        ReadTotals(Take(root, UblNames.Cac + "AnticipatedMonetaryTotal", mapped), order.Totals, values, mapped, owners);

        foreach (XElement line in TakeAll(root, UblNames.Cac + "OrderLine", mapped))
        {
            if (Limits.Exceeded(order.Lines.Count, _options.Limits.MaxDocumentLines))
            {
                diagnostics.Add(Limits.TooMany(_options.Limits.MaxDocumentLines, "order lines"));
                break;
            }

            OrderLine mappedLine = ReadLine(line, values, mapped, owners, _options.Limits);
            owners[line] = mappedLine;
            order.Lines.Add(mappedLine);
        }

        UblExtensions.KeepEverythingElse(root, order, mapped, owners, diagnostics);

        ProfileResolution resolution = _profiles.Resolve(order.SpecificationIdentifier, DocumentSyntax.Ubl);
        foreach (Diagnostic diagnostic in resolution.Diagnostics)
        {
            diagnostics.Add(diagnostic);
        }

        order.Profile = resolution;
        order.Diagnostics = diagnostics.Diagnostics;
        return order;
    }

    /// <summary>A reference element whose only content this model keeps is its identifier.</summary>
    internal static IdentifierField Reference(
        XElement root,
        string localName,
        UblValueReader values,
        HashSet<XElement> mapped,
        Dictionary<XElement, InvoiceNode> owners,
        InvoiceNode owner)
    {
        if (Take(root, UblNames.Cac + localName, mapped) is not { } element)
        {
            return IdentifierField.Unset;
        }

        owners[element] = owner;
        return values.ReadIdentifier(Take(element, UblNames.Cbc + "ID", mapped));
    }

    internal static Party? WrappedParty(
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
        }

        return party;
    }

    private static InvoicingPeriod? ReadPeriod(
        XElement? element,
        UblValueReader values,
        HashSet<XElement> mapped,
        Dictionary<XElement, InvoiceNode> owners)
    {
        if (element is null)
        {
            return null;
        }

        var period = new InvoicingPeriod
        {
            StartDate = values.ReadDate(Take(element, UblNames.Cbc + "StartDate", mapped)),
            EndDate = values.ReadDate(Take(element, UblNames.Cbc + "EndDate", mapped)),
        };

        owners[element] = period;
        return period;
    }

    internal static OrderDelivery? ReadDelivery(
        XElement? element,
        UblValueReader values,
        HashSet<XElement> mapped,
        Dictionary<XElement, InvoiceNode> owners)
    {
        if (element is null)
        {
            return null;
        }

        var delivery = new OrderDelivery
        {
            Identifier = values.ReadIdentifier(Take(element, UblNames.Cbc + "ID", mapped)),
            Quantity = values.ReadQuantity(Take(element, UblNames.Cbc + "Quantity", mapped)),
        };

        owners[element] = delivery;

        if (Take(element, UblNames.Cac + "DeliveryLocation", mapped) is { } location)
        {
            owners[location] = delivery;
            delivery.LocationIdentifier = values.ReadIdentifier(Take(location, UblNames.Cbc + "ID", mapped));
            delivery.LocationName = values.ReadText(Take(location, UblNames.Cbc + "Name", mapped));
            delivery.Address = UblParties.ReadAddress(
                Take(location, UblNames.Cac + "Address", mapped), values, mapped, owners);
        }

        if (Take(element, UblNames.Cac + "RequestedDeliveryPeriod", mapped) is { } period)
        {
            owners[period] = delivery;
            delivery.RequestedFrom = UblMoment.Read(
                Take(period, UblNames.Cbc + "StartDate", mapped),
                Take(period, UblNames.Cbc + "StartTime", mapped));
            delivery.RequestedUntil = UblMoment.Read(
                Take(period, UblNames.Cbc + "EndDate", mapped),
                Take(period, UblNames.Cbc + "EndTime", mapped));
        }

        if (Take(element, UblNames.Cac + "PromisedDeliveryPeriod", mapped) is { } promised)
        {
            owners[promised] = delivery;
            delivery.PromisedFrom = UblMoment.Read(
                Take(promised, UblNames.Cbc + "StartDate", mapped),
                Take(promised, UblNames.Cbc + "StartTime", mapped));
            delivery.PromisedUntil = UblMoment.Read(
                Take(promised, UblNames.Cbc + "EndDate", mapped),
                Take(promised, UblNames.Cbc + "EndTime", mapped));
        }

        if (Take(element, UblNames.Cac + "Despatch", mapped) is { } despatch)
        {
            owners[despatch] = delivery;
            delivery.RequestedDespatchAt = UblMoment.Read(
                Take(despatch, UblNames.Cbc + "RequestedDespatchDate", mapped),
                Take(despatch, UblNames.Cbc + "RequestedDespatchTime", mapped));
        }

        delivery.Recipient = UblParties.Read(
            Take(element, UblNames.Cac + "DeliveryParty", mapped), values, mapped, owners);

        if (Take(element, UblNames.Cac + "Shipment", mapped) is { } shipment)
        {
            owners[shipment] = delivery;
            delivery.ShipmentIdentifier = values.ReadIdentifier(Take(shipment, UblNames.Cbc + "ID", mapped));
            delivery.ShippingPriorityCode = values.ReadCode(
                Take(shipment, UblNames.Cbc + "ShippingPriorityLevelCode", mapped));
        }

        return delivery;
    }

    private static void ReadDeliveryTerms(
        XElement? element,
        Order order,
        UblValueReader values,
        HashSet<XElement> mapped,
        Dictionary<XElement, InvoiceNode> owners)
    {
        if (element is null)
        {
            return;
        }

        owners[element] = order;
        order.DeliveryTermsCode = values.ReadIdentifier(Take(element, UblNames.Cbc + "ID", mapped));
        order.DeliveryTerms = values.ReadText(Take(element, UblNames.Cbc + "SpecialTerms", mapped));

        if (Take(element, UblNames.Cac + "DeliveryLocation", mapped) is { } location)
        {
            owners[location] = order;
            order.DeliveryTermsLocation = values.ReadIdentifier(Take(location, UblNames.Cbc + "ID", mapped));
        }
    }

    internal static void ReadTotals(
        XElement? element,
        DocumentTotals totals,
        UblValueReader values,
        HashSet<XElement> mapped,
        Dictionary<XElement, InvoiceNode> owners)
    {
        if (element is null)
        {
            return;
        }

        owners[element] = totals;
        totals.LineTotalAmount = values.ReadAmount(Take(element, UblNames.Cbc + "LineExtensionAmount", mapped));
        totals.TaxExclusiveAmount = values.ReadAmount(Take(element, UblNames.Cbc + "TaxExclusiveAmount", mapped));
        totals.TaxInclusiveAmount = values.ReadAmount(Take(element, UblNames.Cbc + "TaxInclusiveAmount", mapped));
        totals.AllowanceTotalAmount = values.ReadAmount(Take(element, UblNames.Cbc + "AllowanceTotalAmount", mapped));
        totals.ChargeTotalAmount = values.ReadAmount(Take(element, UblNames.Cbc + "ChargeTotalAmount", mapped));
        totals.PrepaidAmount = values.ReadAmount(Take(element, UblNames.Cbc + "PrepaidAmount", mapped));
        totals.RoundingAmount = values.ReadAmount(Take(element, UblNames.Cbc + "PayableRoundingAmount", mapped));
        totals.DuePayableAmount = values.ReadAmount(Take(element, UblNames.Cbc + "PayableAmount", mapped));
    }

    internal static AllowanceCharge ReadAllowanceCharge(
        XElement element,
        UblValueReader values,
        HashSet<XElement> mapped,
        Dictionary<XElement, InvoiceNode> owners)
    {
        var allowanceCharge = new AllowanceCharge
        {
            IsCharge = values.ReadIndicator(Take(element, UblNames.Cbc + "ChargeIndicator", mapped)).Value == true,
            ReasonCode = values.ReadCode(Take(element, UblNames.Cbc + "AllowanceChargeReasonCode", mapped)),
            Reason = values.ReadText(Take(element, UblNames.Cbc + "AllowanceChargeReason", mapped)),
            Percentage = values.ReadDecimal(Take(element, UblNames.Cbc + "MultiplierFactorNumeric", mapped)),
            Amount = values.ReadAmount(Take(element, UblNames.Cbc + "Amount", mapped)),
            BaseAmount = values.ReadAmount(Take(element, UblNames.Cbc + "BaseAmount", mapped)),
        };

        owners[element] = allowanceCharge;

        if (Take(element, UblNames.Cac + "TaxCategory", mapped) is { } category)
        {
            owners[category] = allowanceCharge;
            allowanceCharge.VatCategoryCode = values.ReadCode(Take(category, UblNames.Cbc + "ID", mapped));
            allowanceCharge.VatRate = values.ReadDecimal(Take(category, UblNames.Cbc + "Percent", mapped));
            Consume(Take(category, UblNames.Cac + "TaxScheme", mapped), allowanceCharge, mapped, owners);
        }

        return allowanceCharge;
    }

    private static OrderLine ReadLine(
        XElement element,
        UblValueReader values,
        HashSet<XElement> mapped,
        Dictionary<XElement, InvoiceNode> owners,
        DocumentLimits limits)
    {
        var line = new OrderLine
        {
            Note = values.ReadText(Take(element, UblNames.Cbc + "Note", mapped)),
        };

        if (Take(element, UblNames.Cac + "LineItem", mapped) is not { } item)
        {
            return line;
        }

        owners[item] = line;
        line.Identifier = values.ReadIdentifier(Take(item, UblNames.Cbc + "ID", mapped));
        line.Quantity = values.ReadQuantity(Take(item, UblNames.Cbc + "Quantity", mapped));
        line.NetAmount = values.ReadAmount(Take(item, UblNames.Cbc + "LineExtensionAmount", mapped));
        line.AccountingReference = values.ReadText(Take(item, UblNames.Cbc + "AccountingCost", mapped));
        line.PartialDeliveryAccepted = values.ReadIndicator(
            Take(item, UblNames.Cbc + "PartialDeliveryIndicator", mapped));

        line.Delivery = ReadDelivery(Take(item, UblNames.Cac + "Delivery", mapped), values, mapped, owners);
        line.Originator = UblParties.Read(
            Take(item, UblNames.Cac + "OriginatorParty", mapped), values, mapped, owners);

        foreach (XElement allowance in TakeAll(item, UblNames.Cac + "AllowanceCharge", mapped))
        {
            line.AllowancesAndCharges.Add(ReadAllowanceCharge(allowance, values, mapped, owners));
        }

        line.Price = ReadPrice(Take(item, UblNames.Cac + "Price", mapped), values, mapped, owners);
        line.Item = ReadItem(Take(item, UblNames.Cac + "Item", mapped), values, mapped, owners, limits);

        return line;
    }

    internal static LinePrice? ReadPrice(
        XElement? element,
        UblValueReader values,
        HashSet<XElement> mapped,
        Dictionary<XElement, InvoiceNode> owners)
    {
        if (element is null)
        {
            return null;
        }

        var price = new LinePrice
        {
            NetPrice = values.ReadAmount(Take(element, UblNames.Cbc + "PriceAmount", mapped)),
            BaseQuantity = values.ReadQuantity(Take(element, UblNames.Cbc + "BaseQuantity", mapped)),
            PriceTypeCode = values.ReadCode(Take(element, UblNames.Cbc + "PriceType", mapped)),
        };

        owners[element] = price;

        // UBL states the discount as an allowance on the price; the model holds the two amounts it carries.
        if (Take(element, UblNames.Cac + "AllowanceCharge", mapped) is { } allowance)
        {
            owners[allowance] = price;
            Consume(Take(allowance, UblNames.Cbc + "ChargeIndicator", mapped), price, mapped, owners);
            price.Discount = values.ReadAmount(Take(allowance, UblNames.Cbc + "Amount", mapped));
            price.GrossPrice = values.ReadAmount(Take(allowance, UblNames.Cbc + "BaseAmount", mapped));
        }

        return price;
    }

    internal static OrderItem? ReadItem(
        XElement? element,
        UblValueReader values,
        HashSet<XElement> mapped,
        Dictionary<XElement, InvoiceNode> owners,
        DocumentLimits limits)
    {
        if (element is null)
        {
            return null;
        }

        var item = new OrderItem
        {
            Description = values.ReadText(Take(element, UblNames.Cbc + "Description", mapped)),
            Name = values.ReadText(Take(element, UblNames.Cbc + "Name", mapped)),
        };

        owners[element] = item;

        item.BuyerIdentifier = Nested(element, "BuyersItemIdentification", values, mapped, owners, item);
        item.SellerIdentifier = Nested(element, "SellersItemIdentification", values, mapped, owners, item);
        item.ManufacturerIdentifier = Nested(element, "ManufacturersItemIdentification", values, mapped, owners, item);
        item.StandardIdentifier = Nested(element, "StandardItemIdentification", values, mapped, owners, item);
        if (Take(element, UblNames.Cac + "ItemSpecificationDocumentReference", mapped) is { } specification)
        {
            AdditionalDocument document = UblAttachments.Read(specification, values, mapped, owners, limits);
            item.SpecificationReference = document.Identifier;
            item.SpecificationDocument = document.Attachment.IsSet || document.Description.IsSet ? document : null;
        }

        foreach (XElement classification in TakeAll(element, UblNames.Cac + "CommodityClassification", mapped))
        {
            owners[classification] = item;
            item.ClassificationCodes.Add(
                values.ReadCode(Take(classification, UblNames.Cbc + "ItemClassificationCode", mapped)));
        }

        if (Take(element, UblNames.Cac + "ClassifiedTaxCategory", mapped) is { } category)
        {
            owners[category] = item;
            item.VatCategoryCode = values.ReadCode(Take(category, UblNames.Cbc + "ID", mapped));
            item.VatRate = values.ReadDecimal(Take(category, UblNames.Cbc + "Percent", mapped));
            Consume(Take(category, UblNames.Cac + "TaxScheme", mapped), item, mapped, owners);
        }

        if (Take(element, UblNames.Cac + "TransactionConditions", mapped) is { } conditions)
        {
            owners[conditions] = item;
            item.TransactionActionCode = values.ReadCode(Take(conditions, UblNames.Cbc + "ActionCode", mapped));
        }

        foreach (XElement certificate in TakeAll(element, UblNames.Cac + "Certificate", mapped))
        {
            var mappedCertificate = new OrderItemCertificate
            {
                Identifier = values.ReadIdentifier(Take(certificate, UblNames.Cbc + "ID", mapped)),
                TypeCode = values.ReadCode(Take(certificate, UblNames.Cbc + "CertificateTypeCode", mapped)),
                Type = values.ReadText(Take(certificate, UblNames.Cbc + "CertificateType", mapped)),
                Remarks = values.ReadText(Take(certificate, UblNames.Cbc + "Remarks", mapped)),
            };

            owners[certificate] = mappedCertificate;
            mappedCertificate.Issuer = UblParties.Read(
                Take(certificate, UblNames.Cac + "IssuerParty", mapped), values, mapped, owners);

            if (Take(certificate, UblNames.Cac + "DocumentReference", mapped) is { } certificateDocument)
            {
                owners[certificateDocument] = mappedCertificate;
                mappedCertificate.DocumentReference = values.ReadIdentifier(
                    Take(certificateDocument, UblNames.Cbc + "ID", mapped));
            }
            item.Certificates.Add(mappedCertificate);
        }

        foreach (XElement property in TakeAll(element, UblNames.Cac + "AdditionalItemProperty", mapped))
        {
            var characteristic = new OrderItemProperty
            {
                Identifier = values.ReadIdentifier(Take(property, UblNames.Cbc + "ID", mapped)),
                Name = values.ReadText(Take(property, UblNames.Cbc + "Name", mapped)),
                NameCode = values.ReadCode(Take(property, UblNames.Cbc + "NameCode", mapped)),
                Value = values.ReadText(Take(property, UblNames.Cbc + "Value", mapped)),
                ValueQualifier = values.ReadText(Take(property, UblNames.Cbc + "ValueQualifier", mapped)),
                ValueQuantity = values.ReadQuantity(Take(property, UblNames.Cbc + "ValueQuantity", mapped)),
            };

            owners[property] = characteristic;
            item.Characteristics.Add(characteristic);
        }

        foreach (XElement instance in TakeAll(element, UblNames.Cac + "ItemInstance", mapped))
        {
            var mappedInstance = new ItemInstance
            {
                SerialIdentifier = values.ReadIdentifier(Take(instance, UblNames.Cbc + "SerialID", mapped)),
            };

            owners[instance] = mappedInstance;

            if (Take(instance, UblNames.Cac + "LotIdentification", mapped) is { } lot)
            {
                owners[lot] = mappedInstance;
                mappedInstance.LotIdentifier = values.ReadIdentifier(Take(lot, UblNames.Cbc + "LotNumberID", mapped));
            }

            item.Instances.Add(mappedInstance);
        }

        return item;
    }

    private static IdentifierField Nested(
        XElement parent,
        string wrapper,
        UblValueReader values,
        HashSet<XElement> mapped,
        Dictionary<XElement, InvoiceNode> owners,
        InvoiceNode owner)
    {
        if (Take(parent, UblNames.Cac + wrapper, mapped) is not { } element)
        {
            return IdentifierField.Unset;
        }

        owners[element] = owner;
        return values.ReadIdentifier(Take(element, UblNames.Cbc + "ID", mapped));
    }

    /// <summary>Claims an element the model does not carry, so it is not kept twice as extension data.</summary>
    private static void Consume(
        XElement? element,
        InvoiceNode owner,
        HashSet<XElement> mapped,
        Dictionary<XElement, InvoiceNode> owners)
    {
        if (element is null)
        {
            return;
        }

        owners[element] = owner;

        foreach (XElement child in element.Descendants())
        {
            mapped.Add(child);
        }
    }

    internal static XElement? Take(XElement? parent, XName name, HashSet<XElement> mapped)
    {
        XElement? element = parent?.Element(name);
        if (element is not null)
        {
            mapped.Add(element);
        }

        return element;
    }

    internal static List<XElement> TakeAll(XElement parent, XName name, HashSet<XElement> mapped)
    {
        List<XElement> elements = [.. parent.Elements(name)];
        foreach (XElement element in elements)
        {
            mapped.Add(element);
        }

        return elements;
    }
}
