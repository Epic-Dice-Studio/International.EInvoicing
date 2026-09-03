using System.Xml.Linq;
using International.EInvoicing.Cii.Reading;
using International.EInvoicing.Configuration;
using International.EInvoicing.Diagnostics;
using International.EInvoicing.Documents;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;
using International.EInvoicing.Values;
using International.EInvoicing.Xml;
using static International.EInvoicing.OrderX.Reading.OrderXNodes;
using static International.EInvoicing.OrderX.Reading.OrderXParties;

namespace International.EInvoicing.OrderX.Reading;

/// <summary>
/// Reads an Order-X order, or an order change, into the canonical model.
/// </summary>
/// <remarks>
/// The two are the same document with a different type code — 220 and 230 — filling the same model, the
/// arrangement an invoice and a credit note already have. Reading never throws on the document: a value that
/// cannot be typed keeps its raw text, an element nobody mapped is kept verbatim where it sat, and everything
/// the reader had to give up is reported.
/// </remarks>
public sealed class OrderXOrderReader : IDocumentReader<Order>
{
    private readonly EInvoicingOptions _options;
    private readonly IProfileResolver _profiles;

    /// <summary>Creates a reader using the supplied options and profile resolver.</summary>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    public OrderXOrderReader(EInvoicingOptions options, IProfileResolver profiles)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(profiles);

        _options = options;
        _profiles = profiles;
    }

    /// <inheritdoc />
    public DocumentSyntax Syntax => DocumentSyntax.OrderX;

    private static XNamespace Ram => OrderXNames.Ram;

    private static XNamespace Rsm => OrderXNames.Rsm;

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
            diagnostics.Add(Diagnostic.Create(OrderXDiagnostics.MalformedDocument, exception.Message) with
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
        var values = new CiiValueReader(diagnostics, mapped) { Limits = _options.Limits };
        var order = new Order();

        ReadContext(root, order, values);
        ReadExchangedDocument(root, order, values, owners);

        XElement? transaction = In(values, root, Rsm + "SupplyChainTradeTransaction");

        foreach (XElement line in AllIn(values, transaction, Ram + "IncludedSupplyChainTradeLineItem"))
        {
            if (Limits.Exceeded(order.Lines.Count, values.Limits.MaxDocumentLines))
            {
                diagnostics.Add(Limits.TooMany(values.Limits.MaxDocumentLines, "order lines"));
                break;
            }

            OrderLine mappedLine = ReadLine(line, values, owners);
            owners[line] = mappedLine;
            order.Lines.Add(mappedLine);
        }

        ReadAgreement(In(values, transaction, Ram + "ApplicableHeaderTradeAgreement"), order, values, owners);
        ReadDelivery(In(values, transaction, Ram + "ApplicableHeaderTradeDelivery"), order, values, owners);
        ReadSettlement(In(values, transaction, Ram + "ApplicableHeaderTradeSettlement"), order, values, owners);

        KeepEverythingElse(root, order, mapped, owners, diagnostics);

        ProfileResolution resolution = _profiles.Resolve(order.SpecificationIdentifier, DocumentSyntax.OrderX);
        foreach (Diagnostic diagnostic in resolution.Diagnostics)
        {
            diagnostics.Add(diagnostic);
        }

        order.Profile = resolution;
        order.Diagnostics = diagnostics.Diagnostics;
        return order;
    }

    private static void ReadContext(XElement root, Order order, CiiValueReader values)
    {
        XElement? context = In(values, root, Rsm + "ExchangedDocumentContext");

        order.IsTest = values.ReadIndicator(In(values, context, Ram + "TestIndicator"));
        order.BusinessProcessType = values.ReadIdentifier(
            In(values, In(values, context, Ram + "BusinessProcessSpecifiedDocumentContextParameter"), Ram + "ID"));
        order.SpecificationIdentifier = ProfileIdentifier.FromDocument(
            In(values, In(values, context, Ram + "GuidelineSpecifiedDocumentContextParameter"), Ram + "ID")?.Value);
    }

    private static void ReadExchangedDocument(
        XElement root,
        Order order,
        CiiValueReader values,
        Dictionary<XElement, InvoiceNode> owners)
    {
        XElement? document = In(values, root, Rsm + "ExchangedDocument");

        order.Number = values.ReadIdentifier(In(values, document, Ram + "ID"));
        order.Name = values.ReadText(In(values, document, Ram + "Name"));
        order.TypeCode = values.ReadCode(In(values, document, Ram + "TypeCode"));
        order.IssuedAt = values.ReadDateTime(In(values, document, Ram + "IssueDateTime"));
        order.IsCopy = values.ReadIndicator(In(values, document, Ram + "CopyIndicator"));
        order.PurposeCode = values.ReadCode(In(values, document, Ram + "PurposeCode"));
        order.RequestedResponseTypeCode = values.ReadCode(
            In(values, document, Ram + "RequestedResponseTypeCode"));

        foreach (XElement note in AllIn(values, document, Ram + "IncludedNote"))
        {
            order.Notes.Add(new InvoiceNote
            {
                Text = values.ReadText(In(values, note, Ram + "Content")),
                SubjectCode = values.ReadCode(In(values, note, Ram + "SubjectCode")),
            });
        }

        if (In(values, document, Ram + "EffectiveSpecifiedPeriod") is { } period)
        {
            order.ValidityPeriod = ReadPeriod(period, values);
            owners[period] = order.ValidityPeriod!;
        }
    }

    private static void ReadAgreement(
        XElement? agreement,
        Order order,
        CiiValueReader values,
        Dictionary<XElement, InvoiceNode> owners)
    {
        if (agreement is null)
        {
            return;
        }

        order.BuyerReference = values.ReadText(In(values, agreement, Ram + "BuyerReference"));
        order.Seller = ReadParty(In(values, agreement, Ram + "SellerTradeParty"), values, owners);
        order.Buyer = ReadParty(In(values, agreement, Ram + "BuyerTradeParty"), values, owners);
        order.Originator = ReadParty(In(values, agreement, Ram + "BuyerRequisitionerTradeParty"), values, owners);

        if (In(values, agreement, Ram + "ApplicableTradeDeliveryTerms") is { } terms)
        {
            XElement? location = In(values, terms, Ram + "RelevantTradeLocation");

            order.DeliveryTermsCode = values.ReadIdentifier(In(values, terms, Ram + "DeliveryTypeCode"));
            order.DeliveryTerms = values.ReadText(In(values, terms, Ram + "Description"));
            order.DeliveryTermsFunctionCode = values.ReadCode(In(values, terms, Ram + "FunctionCode"));
            order.DeliveryTermsLocation = values.ReadIdentifier(In(values, location, Ram + "ID"));
            order.DeliveryTermsLocationName = values.ReadText(In(values, location, Ram + "Name"));
        }

        order.SalesOrderNumber = ReadReference(values, agreement, "SellerOrderReferencedDocument");
        order.OrderReference = ReadReference(values, agreement, "BuyerOrderReferencedDocument");
        order.QuotationReference = ReadReference(values, agreement, "QuotationReferencedDocument");
        order.ContractReference = ReadReference(values, agreement, "ContractReferencedDocument");
        order.OriginatorReference = ReadReference(values, agreement, "RequisitionReferencedDocument");
        order.CatalogueReference = ReadReference(values, agreement, "CatalogueReferencedDocument");
        order.BlanketOrderReference = ReadReference(values, agreement, "BlanketOrderReferencedDocument");
        order.PreviousOrderChangeReference =
            ReadReference(values, agreement, "PreviousOrderChangeReferencedDocument");
        order.PreviousOrderResponseReference =
            ReadReference(values, agreement, "PreviousOrderResponseReferencedDocument");

        foreach (XElement document in AllIn(values, agreement, Ram + "AdditionalReferencedDocument"))
        {
            order.AdditionalDocuments.Add(ReadDocument(document, values, owners));
        }

        if (In(values, agreement, Ram + "SpecifiedProcuringProject") is { } project)
        {
            order.ProjectReference = values.ReadIdentifier(In(values, project, Ram + "ID"));
            order.ProjectName = values.ReadText(In(values, project, Ram + "Name"));
        }
    }

    private static void ReadDelivery(
        XElement? element,
        Order order,
        CiiValueReader values,
        Dictionary<XElement, InvoiceNode> owners)
    {
        if (element is null)
        {
            return;
        }

        var delivery = new OrderDelivery
        {
            Recipient = ReadParty(In(values, element, Ram + "ShipToTradeParty"), values, owners),
            Consignor = ReadParty(In(values, element, Ram + "ShipFromTradeParty"), values, owners),
        };

        delivery.Address = delivery.Recipient?.Address;
        ReadRequestedDelivery(In(values, element, Ram + "RequestedDeliverySupplyChainEvent"), delivery, values);
        delivery.RequestedDespatchAt = values.ReadDateTime(
            In(values, In(values, element, Ram + "RequestedDespatchSupplyChainEvent"), Ram + "OccurrenceDateTime"));

        order.Delivery = delivery;
        owners[element] = delivery;
    }

    /// <summary>
    /// Reads when delivery is wanted. Order-X states either a moment or a window, so both are read into the
    /// same pair of fields — a single date is a window that begins and ends on the same day.
    /// </summary>
    private static void ReadRequestedDelivery(XElement? element, OrderDelivery delivery, CiiValueReader values)
    {
        if (element is null)
        {
            return;
        }

        if (In(values, element, Ram + "OccurrenceSpecifiedPeriod") is { } window)
        {
            delivery.RequestedFrom = values.ReadDateTime(In(values, window, Ram + "StartDateTime"));
            delivery.RequestedUntil = values.ReadDateTime(In(values, window, Ram + "EndDateTime"));
        }

        delivery.RequestedAt = values.ReadDateTime(In(values, element, Ram + "OccurrenceDateTime"));
    }

    private static void ReadSettlement(
        XElement? settlement,
        Order order,
        CiiValueReader values,
        Dictionary<XElement, InvoiceNode> owners)
    {
        if (settlement is null)
        {
            return;
        }

        order.CurrencyCode = values.ReadCode(In(values, settlement, Ram + "OrderCurrencyCode"));
        order.Invoicee = ReadParty(In(values, settlement, Ram + "InvoiceeTradeParty"), values, owners);

        if (In(values, settlement, Ram + "SpecifiedTradeSettlementPaymentMeans") is { } means)
        {
            order.Payment = new PaymentInstructions
            {
                MeansTypeCode = values.ReadCode(In(values, means, Ram + "TypeCode")),
                MeansText = values.ReadText(In(values, means, Ram + "Information")),
            };

            owners[means] = order.Payment;
        }

        foreach (XElement allowanceCharge in AllIn(values, settlement, Ram + "SpecifiedTradeAllowanceCharge"))
        {
            order.AllowancesAndCharges.Add(ReadAllowanceCharge(allowanceCharge, values, owners));
        }

        order.PaymentTerms = values.ReadText(
            In(values, In(values, settlement, Ram + "SpecifiedTradePaymentTerms"), Ram + "Description"));

        ReadTotals(In(values, settlement, Ram + "SpecifiedTradeSettlementHeaderMonetarySummation"), order, values, owners);

        order.AccountingReference = values.ReadText(
            In(values, In(values, settlement, Ram + "ReceivableSpecifiedTradeAccountingAccount"), Ram + "ID"));
    }

    private static void ReadTotals(
        XElement? element,
        Order order,
        CiiValueReader values,
        Dictionary<XElement, InvoiceNode> owners)
    {
        if (element is null)
        {
            return;
        }

        order.Totals.LineTotalAmount = values.ReadAmount(In(values, element, Ram + "LineTotalAmount"));
        order.Totals.ChargeTotalAmount = values.ReadAmount(In(values, element, Ram + "ChargeTotalAmount"));
        order.Totals.AllowanceTotalAmount = values.ReadAmount(In(values, element, Ram + "AllowanceTotalAmount"));
        order.Totals.TaxExclusiveAmount = values.ReadAmount(In(values, element, Ram + "TaxBasisTotalAmount"));
        order.Totals.TaxAmount = values.ReadAmount(In(values, element, Ram + "TaxTotalAmount"));
        order.Totals.RoundingAmount = values.ReadAmount(In(values, element, Ram + "RoundingAmount"));
        order.Totals.TaxInclusiveAmount = values.ReadAmount(In(values, element, Ram + "GrandTotalAmount"));
        order.Totals.PrepaidAmount = values.ReadAmount(In(values, element, Ram + "TotalPrepaidAmount"));
        order.Totals.DuePayableAmount = values.ReadAmount(In(values, element, Ram + "DuePayableAmount"));
        order.TaxAmount = order.Totals.TaxAmount;

        owners[element] = order.Totals;
    }

    private static OrderLine ReadLine(
        XElement element,
        CiiValueReader values,
        Dictionary<XElement, InvoiceNode> owners)
    {
        var line = new OrderLine();

        if (In(values, element, Ram + "AssociatedDocumentLineDocument") is { } document)
        {
            line.Identifier = values.ReadIdentifier(In(values, document, Ram + "LineID"));
            line.StatusCode = values.ReadCode(In(values, document, Ram + "LineStatusCode"));

            foreach (XElement note in AllIn(values, document, Ram + "IncludedNote"))
            {
                line.Notes.Add(new InvoiceNote
                {
                    Text = values.ReadText(In(values, note, Ram + "Content")),
                    SubjectCode = values.ReadCode(In(values, note, Ram + "SubjectCode")),
                });
            }
        }

        line.Item = ReadItem(In(values, element, Ram + "SpecifiedTradeProduct"), values, owners);
        ReadLineAgreement(In(values, element, Ram + "SpecifiedLineTradeAgreement"), line, values, owners);
        ReadLineDelivery(In(values, element, Ram + "SpecifiedLineTradeDelivery"), line, values, owners);
        ReadLineSettlement(In(values, element, Ram + "SpecifiedLineTradeSettlement"), line, values, owners);

        return line;
    }

    private static OrderItem? ReadItem(
        XElement? element,
        CiiValueReader values,
        Dictionary<XElement, InvoiceNode> owners)
    {
        if (element is null)
        {
            return null;
        }

        var item = new OrderItem
        {
            StandardIdentifier = values.ReadIdentifier(In(values, element, Ram + "GlobalID")),
            SellerIdentifier = values.ReadIdentifier(In(values, element, Ram + "SellerAssignedID")),
            BuyerIdentifier = values.ReadIdentifier(In(values, element, Ram + "BuyerAssignedID")),
            Name = values.ReadText(In(values, element, Ram + "Name")),
            Description = values.ReadText(In(values, element, Ram + "Description")),
            BatchIdentifier = values.ReadIdentifier(In(values, element, Ram + "BatchID")),
            BrandName = values.ReadText(In(values, element, Ram + "BrandName")),
            OriginCountryCode = values.ReadCode(
                In(values, In(values, element, Ram + "OriginTradeCountry"), Ram + "ID")),
        };

        foreach (XElement characteristic in AllIn(values, element, Ram + "ApplicableProductCharacteristic"))
        {
            item.Characteristics.Add(new OrderItemProperty
            {
                NameCode = values.ReadCode(In(values, characteristic, Ram + "TypeCode")),
                Name = values.ReadText(In(values, characteristic, Ram + "Description")),
                Value = values.ReadText(In(values, characteristic, Ram + "Value")),
                ValueQuantity = values.ReadQuantity(In(values, characteristic, Ram + "ValueMeasure")),
            });
        }

        foreach (XElement classification in AllIn(values, element, Ram + "DesignatedProductClassification"))
        {
            ItemClassification mapped = new()
            {
                Code = values.ReadCode(In(values, classification, Ram + "ClassCode")),
                Name = values.ReadText(In(values, classification, Ram + "ClassName")),
            };

            owners[classification] = mapped;
            item.Classifications.Add(mapped);
        }

        foreach (XElement instance in AllIn(values, element, Ram + "IndividualTradeProductInstance"))
        {
            item.Instances.Add(new ItemInstance
            {
                LotIdentifier = values.ReadIdentifier(In(values, instance, Ram + "BatchID")),
                SerialIdentifier = values.ReadIdentifier(In(values, instance, Ram + "SerialID")),
            });
        }

        if (In(values, element, Ram + "ApplicableSupplyChainPackaging") is { } packaging)
        {
            XElement? size = In(values, packaging, Ram + "LinearSpatialDimension");

            item.Packaging = new ItemPackaging
            {
                TypeCode = values.ReadCode(In(values, packaging, Ram + "TypeCode")),
                Width = values.ReadQuantity(In(values, size, Ram + "WidthMeasure")),
                Length = values.ReadQuantity(In(values, size, Ram + "LengthMeasure")),
                Height = values.ReadQuantity(In(values, size, Ram + "HeightMeasure")),
            };

            owners[packaging] = item.Packaging;
        }

        if (AllIn(values, element, Ram + "AdditionalReferenceReferencedDocument") is [{ } specification, ..])
        {
            item.SpecificationDocument = ReadDocument(specification, values, owners);
            item.SpecificationReference = item.SpecificationDocument.Identifier;
        }

        owners[element] = item;
        return item;
    }

    private static void ReadLineAgreement(
        XElement? agreement,
        OrderLine line,
        CiiValueReader values,
        Dictionary<XElement, InvoiceNode> owners)
    {
        if (agreement is null)
        {
            return;
        }

        line.Originator = ReadParty(In(values, agreement, Ram + "BuyerRequisitionerTradeParty"), values, owners);

        line.OrderLineReference = values.ReadIdentifier(
            In(values, In(values, agreement, Ram + "BuyerOrderReferencedDocument"), Ram + "LineID"));

        if (In(values, agreement, Ram + "QuotationReferencedDocument") is { } quotation)
        {
            line.QuotationReference = values.ReadIdentifier(In(values, quotation, Ram + "IssuerAssignedID"));
            line.QuotationLineReference = values.ReadIdentifier(In(values, quotation, Ram + "LineID"));
        }

        if (In(values, agreement, Ram + "CatalogueReferencedDocument") is { } catalogue)
        {
            line.CatalogueReference = values.ReadIdentifier(In(values, catalogue, Ram + "IssuerAssignedID"));
            line.CatalogueLineReference = values.ReadIdentifier(In(values, catalogue, Ram + "LineID"));
        }

        line.BlanketOrderLineReference = values.ReadIdentifier(
            In(values, In(values, agreement, Ram + "BlanketOrderReferencedDocument"), Ram + "LineID"));

        foreach (XElement document in AllIn(values, agreement, Ram + "AdditionalReferencedDocument"))
        {
            line.AdditionalDocuments.Add(ReadDocument(document, values, owners));
        }

        ReadPrice(agreement, line, values, owners);
    }

    private static void ReadPrice(
        XElement agreement,
        OrderLine line,
        CiiValueReader values,
        Dictionary<XElement, InvoiceNode> owners)
    {
        XElement? gross = In(values, agreement, Ram + "GrossPriceProductTradePrice");
        XElement? net = In(values, agreement, Ram + "NetPriceProductTradePrice");

        if (gross is null && net is null)
        {
            return;
        }

        var price = new LinePrice
        {
            GrossPrice = values.ReadAmount(In(values, gross, Ram + "ChargeAmount")),
            NetPrice = values.ReadAmount(In(values, net, Ram + "ChargeAmount")),
            BaseQuantity = values.ReadQuantity(In(values, net ?? gross, Ram + "BasisQuantity")),
        };

        // An allowance on the gross price is how CII says "the net is the gross less this", so it belongs to
        // the price rather than to the line's allowances — which are amounts, not per-unit reductions.
        foreach (XElement applied in AllIn(values, gross, Ram + "AppliedTradeAllowanceCharge"))
        {
            price.Adjustments.Add(ReadAllowanceCharge(applied, values, owners));
        }

        // BT-147 is one number, so it is the total of them; the reasons live in the list.
        decimal reduction = price.Adjustments
            .Where(adjustment => !adjustment.IsCharge)
            .Sum(adjustment => adjustment.Amount.Value ?? 0m);

        if (reduction != 0m)
        {
            price.Discount = new AmountField(reduction);
        }

        // Both prices state the same basis quantity, and the model keeps one.
        if (net is not null && gross is not null)
        {
            values.Consume(gross.Element(Ram + "BasisQuantity"));
        }

        line.Price = price;

        if (net is not null)
        {
            owners[net] = price;
        }
    }

    private static void ReadLineDelivery(
        XElement? element,
        OrderLine line,
        CiiValueReader values,
        Dictionary<XElement, InvoiceNode> owners)
    {
        if (element is null)
        {
            return;
        }

        line.PartialDeliveryAccepted = values.ReadIndicator(
            In(values, element, Ram + "PartialDeliveryAllowedIndicator"));
        line.Quantity = values.ReadQuantity(In(values, element, Ram + "RequestedQuantity"));
        line.PackageQuantity = values.ReadQuantity(In(values, element, Ram + "PackageQuantity"));
        line.UnitsPerPackage = values.ReadQuantity(In(values, element, Ram + "PerPackageUnitQuantity"));

        var delivery = new OrderDelivery
        {
            Recipient = ReadParty(In(values, element, Ram + "ShipToTradeParty"), values, owners),
            Consignor = ReadParty(In(values, element, Ram + "ShipFromTradeParty"), values, owners),
        };

        ReadRequestedDelivery(In(values, element, Ram + "RequestedDeliverySupplyChainEvent"), delivery, values);
        delivery.RequestedDespatchAt = values.ReadDateTime(
            In(values, In(values, element, Ram + "RequestedDespatchSupplyChainEvent"), Ram + "OccurrenceDateTime"));

        if (delivery.Recipient is not null
            || delivery.Consignor is not null
            || delivery.RequestedAt.IsSet
            || delivery.RequestedFrom.IsSet
            || delivery.RequestedUntil.IsSet
            || delivery.RequestedDespatchAt.IsSet)
        {
            line.Delivery = delivery;
        }

        owners[element] = line.Delivery ?? (InvoiceNode)line;
    }

    private static void ReadLineSettlement(
        XElement? settlement,
        OrderLine line,
        CiiValueReader values,
        Dictionary<XElement, InvoiceNode> owners)
    {
        if (settlement is null)
        {
            return;
        }

        if (In(values, settlement, Ram + "ApplicableTradeTax") is { } tax)
        {
            line.Item ??= new OrderItem();
            line.Item.VatCategoryCode = values.ReadCode(In(values, tax, Ram + "CategoryCode"));
            line.Item.VatRate = values.ReadDecimal(In(values, tax, Ram + "RateApplicablePercent"));
            values.Consume(tax.Element(Ram + "TypeCode"));
        }

        foreach (XElement allowanceCharge in AllIn(values, settlement, Ram + "SpecifiedTradeAllowanceCharge"))
        {
            line.AllowancesAndCharges.Add(ReadAllowanceCharge(allowanceCharge, values, owners));
        }

        line.NetAmount = values.ReadAmount(
            In(
                values,
                In(values, settlement, Ram + "SpecifiedTradeSettlementLineMonetarySummation"),
                Ram + "LineTotalAmount"));

        line.AccountingReference = values.ReadText(
            In(values, In(values, settlement, Ram + "ReceivableSpecifiedTradeAccountingAccount"), Ram + "ID"));
    }
}
