using System.Xml.Linq;
using International.EInvoicing.Cii.Reading;
using International.EInvoicing.Configuration;
using International.EInvoicing.Diagnostics;
using International.EInvoicing.Documents;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;
using International.EInvoicing.Xml;
using static International.EInvoicing.OrderX.Reading.OrderXNodes;
using static International.EInvoicing.OrderX.Reading.OrderXParties;

namespace International.EInvoicing.OrderX.Reading;

/// <summary>
/// Reads an Order-X order response — what the seller says about an order — into the canonical model.
/// </summary>
/// <remarks>
/// The third document Order-X carries, and the same transaction shape as the other two: one root element,
/// told apart by the type code 231. What is its own is the answer — a status on the document and a status on
/// each line, and an agreed quantity beside the requested one, because a response that is not a plain
/// acceptance is precisely the difference between the two.
/// </remarks>
public sealed class OrderXOrderResponseReader : IDocumentReader<OrderResponse>
{
    private readonly EInvoicingOptions _options;
    private readonly IProfileResolver _profiles;

    /// <summary>Creates a reader using the supplied options and profile resolver.</summary>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    public OrderXOrderResponseReader(EInvoicingOptions options, IProfileResolver profiles)
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

    /// <summary>Reads a response from a stream. The stream is left open.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <c>null</c>.</exception>
    public ParseResult<OrderResponse> Read(Stream stream)
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

            return diagnostics.ToResult<OrderResponse>(null);
        }

        return diagnostics.ToResult(ReadResponse(root, diagnostics));
    }

    /// <summary>Reads a response from XML text.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="xml"/> is <c>null</c>.</exception>
    public ParseResult<OrderResponse> Read(string xml)
    {
        ArgumentNullException.ThrowIfNull(xml);

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(xml));
        return Read(stream);
    }

    /// <inheritdoc />
    public async Task<ParseResult<OrderResponse>> ReadAsync(
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        byte[] content = await DocumentStreams.ReadAllAsync(stream, cancellationToken).ConfigureAwait(false);

        using var buffered = new MemoryStream(content, writable: false);
        return Read(buffered);
    }

    private OrderResponse ReadResponse(XElement root, DiagnosticCollector diagnostics)
    {
        var mapped = new HashSet<XElement>();
        var owners = new Dictionary<XElement, InvoiceNode>();
        var values = new CiiValueReader(diagnostics, mapped) { Limits = _options.Limits };
        var response = new OrderResponse();

        ReadContext(root, response, values);
        ReadExchangedDocument(root, response, values, owners);

        XElement? transaction = In(values, root, Rsm + "SupplyChainTradeTransaction");

        foreach (XElement line in AllIn(values, transaction, Ram + "IncludedSupplyChainTradeLineItem"))
        {
            if (Limits.Exceeded(response.Lines.Count, values.Limits.MaxDocumentLines))
            {
                diagnostics.Add(Limits.TooMany(values.Limits.MaxDocumentLines, "response lines"));
                break;
            }

            OrderResponseLine mappedLine = ReadLine(line, values, owners);
            owners[line] = mappedLine;
            response.Lines.Add(mappedLine);
        }

        ReadAgreement(In(values, transaction, Ram + "ApplicableHeaderTradeAgreement"), response, values, owners);
        ReadDelivery(In(values, transaction, Ram + "ApplicableHeaderTradeDelivery"), response, values, owners);
        ReadSettlement(In(values, transaction, Ram + "ApplicableHeaderTradeSettlement"), response, values, owners);

        KeepEverythingElse(root, response, mapped, owners, diagnostics);

        ProfileResolution resolution = _profiles.Resolve(response.SpecificationIdentifier, DocumentSyntax.OrderX);
        foreach (Diagnostic diagnostic in resolution.Diagnostics)
        {
            diagnostics.Add(diagnostic);
        }

        response.Profile = resolution;
        response.Diagnostics = diagnostics.Diagnostics;
        return response;
    }

    private static void ReadContext(XElement root, OrderResponse response, CiiValueReader values)
    {
        XElement? context = In(values, root, Rsm + "ExchangedDocumentContext");

        response.IsTest = values.ReadIndicator(In(values, context, Ram + "TestIndicator"));
        response.BusinessProcessType = values.ReadIdentifier(
            In(values, In(values, context, Ram + "BusinessProcessSpecifiedDocumentContextParameter"), Ram + "ID"));
        response.SpecificationIdentifier = ProfileIdentifier.FromDocument(
            In(values, In(values, context, Ram + "GuidelineSpecifiedDocumentContextParameter"), Ram + "ID")?.Value);
    }

    private static void ReadExchangedDocument(
        XElement root,
        OrderResponse response,
        CiiValueReader values,
        Dictionary<XElement, InvoiceNode> owners)
    {
        XElement? document = In(values, root, Rsm + "ExchangedDocument");

        response.Number = values.ReadIdentifier(In(values, document, Ram + "ID"));
        response.Name = values.ReadText(In(values, document, Ram + "Name"));
        response.TypeCode = values.ReadCode(In(values, document, Ram + "TypeCode"));

        // BT-that-matters: the seller's answer to the order as a whole.
        response.ResponseCode = values.ReadCode(In(values, document, Ram + "StatusCode"));
        response.IssuedAt = values.ReadDateTime(In(values, document, Ram + "IssueDateTime"));
        response.IsCopy = values.ReadIndicator(In(values, document, Ram + "CopyIndicator"));
        response.PurposeCode = values.ReadCode(In(values, document, Ram + "PurposeCode"));
        response.RequestedResponseTypeCode = values.ReadCode(
            In(values, document, Ram + "RequestedResponseTypeCode"));

        foreach (XElement note in AllIn(values, document, Ram + "IncludedNote"))
        {
            response.Notes.Add(new InvoiceNote
            {
                Text = values.ReadText(In(values, note, Ram + "Content")),
                SubjectCode = values.ReadCode(In(values, note, Ram + "SubjectCode")),
            });
        }

        if (In(values, document, Ram + "EffectiveSpecifiedPeriod") is { } period)
        {
            response.ValidityPeriod = ReadPeriod(period, values);
            owners[period] = response.ValidityPeriod!;
        }
    }

    private static void ReadAgreement(
        XElement? agreement,
        OrderResponse response,
        CiiValueReader values,
        Dictionary<XElement, InvoiceNode> owners)
    {
        if (agreement is null)
        {
            return;
        }

        response.BuyerReference = values.ReadText(In(values, agreement, Ram + "BuyerReference"));
        response.Seller = ReadParty(In(values, agreement, Ram + "SellerTradeParty"), values, owners);
        response.Buyer = ReadParty(In(values, agreement, Ram + "BuyerTradeParty"), values, owners);
        response.Originator = ReadParty(
            In(values, agreement, Ram + "BuyerRequisitionerTradeParty"), values, owners);

        if (In(values, agreement, Ram + "ApplicableTradeDeliveryTerms") is { } terms)
        {
            XElement? location = In(values, terms, Ram + "RelevantTradeLocation");

            response.DeliveryTermsCode = values.ReadIdentifier(In(values, terms, Ram + "DeliveryTypeCode"));
            response.DeliveryTerms = values.ReadText(In(values, terms, Ram + "Description"));
            response.DeliveryTermsFunctionCode = values.ReadCode(In(values, terms, Ram + "FunctionCode"));
            response.DeliveryTermsLocation = values.ReadIdentifier(In(values, location, Ram + "ID"));
            response.DeliveryTermsLocationName = values.ReadText(In(values, location, Ram + "Name"));
        }

        response.SalesOrderNumber = ReadReference(values, agreement, "SellerOrderReferencedDocument");
        response.OrderReference = ReadReference(values, agreement, "BuyerOrderReferencedDocument");
        response.QuotationReference = ReadReference(values, agreement, "QuotationReferencedDocument");
        response.ContractReference = ReadReference(values, agreement, "ContractReferencedDocument");
        response.OriginatorReference = ReadReference(values, agreement, "RequisitionReferencedDocument");
        response.CatalogueReference = ReadReference(values, agreement, "CatalogueReferencedDocument");
        response.BlanketOrderReference = ReadReference(values, agreement, "BlanketOrderReferencedDocument");
        response.OrderChangeReference =
            ReadReference(values, agreement, "PreviousOrderChangeReferencedDocument");
        response.PreviousOrderResponseReference =
            ReadReference(values, agreement, "PreviousOrderResponseReferencedDocument");

        foreach (XElement document in AllIn(values, agreement, Ram + "AdditionalReferencedDocument"))
        {
            response.AdditionalDocuments.Add(ReadDocument(document, values, owners));
        }

        if (In(values, agreement, Ram + "SpecifiedProcuringProject") is { } project)
        {
            response.ProjectReference = values.ReadIdentifier(In(values, project, Ram + "ID"));
            response.ProjectName = values.ReadText(In(values, project, Ram + "Name"));
        }
    }

    private static void ReadDelivery(
        XElement? element,
        OrderResponse response,
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
        ReadPromisedDelivery(In(values, element, Ram + "RequestedDeliverySupplyChainEvent"), delivery, values);
        delivery.RequestedDespatchAt = values.ReadDateTime(
            In(values, In(values, element, Ram + "RequestedDespatchSupplyChainEvent"), Ram + "OccurrenceDateTime"));

        response.Delivery = delivery;
        owners[element] = delivery;
    }

    /// <summary>
    /// When the seller undertakes to deliver.
    /// </summary>
    /// <remarks>
    /// The same element the order used to <em>ask</em>, read into the promised pair rather than the
    /// requested one: on a response it is an undertaking, not a request, and the model keeps the two apart
    /// so a buyer can compare what they asked for with what they were promised.
    /// </remarks>
    private static void ReadPromisedDelivery(XElement? element, OrderDelivery delivery, CiiValueReader values)
    {
        if (element is null)
        {
            return;
        }

        if (In(values, element, Ram + "OccurrenceSpecifiedPeriod") is { } window)
        {
            delivery.PromisedFrom = values.ReadDateTime(In(values, window, Ram + "StartDateTime"));
            delivery.PromisedUntil = values.ReadDateTime(In(values, window, Ram + "EndDateTime"));
        }

        delivery.PromisedAt = values.ReadDateTime(In(values, element, Ram + "OccurrenceDateTime"));
    }

    private static void ReadSettlement(
        XElement? settlement,
        OrderResponse response,
        CiiValueReader values,
        Dictionary<XElement, InvoiceNode> owners)
    {
        if (settlement is null)
        {
            return;
        }

        response.CurrencyCode = values.ReadCode(In(values, settlement, Ram + "OrderCurrencyCode"));
        response.Invoicee = ReadParty(In(values, settlement, Ram + "InvoiceeTradeParty"), values, owners);

        if (In(values, settlement, Ram + "SpecifiedTradeSettlementPaymentMeans") is { } means)
        {
            response.Payment = new PaymentInstructions
            {
                MeansTypeCode = values.ReadCode(In(values, means, Ram + "TypeCode")),
                MeansText = values.ReadText(In(values, means, Ram + "Information")),
            };

            owners[means] = response.Payment;
        }

        foreach (XElement tax in AllIn(values, settlement, Ram + "ApplicableTradeTax"))
        {
            response.VatBreakdown.Add(new VatBreakdownEntry
            {
                TaxAmount = values.ReadAmount(In(values, tax, Ram + "CalculatedAmount")),
                TaxableAmount = values.ReadAmount(In(values, tax, Ram + "BasisAmount")),
                CategoryCode = values.ReadCode(In(values, tax, Ram + "CategoryCode")),
                Rate = values.ReadDecimal(In(values, tax, Ram + "RateApplicablePercent")),
            });

            values.Consume(tax.Element(Ram + "TypeCode"));
        }

        foreach (XElement allowanceCharge in AllIn(values, settlement, Ram + "SpecifiedTradeAllowanceCharge"))
        {
            response.AllowancesAndCharges.Add(ReadAllowanceCharge(allowanceCharge, values, owners));
        }

        response.PaymentTerms = values.ReadText(
            In(values, In(values, settlement, Ram + "SpecifiedTradePaymentTerms"), Ram + "Description"));

        ReadTotals(
            In(values, settlement, Ram + "SpecifiedTradeSettlementHeaderMonetarySummation"),
            response,
            values,
            owners);

        response.AccountingReference = values.ReadText(
            In(values, In(values, settlement, Ram + "ReceivableSpecifiedTradeAccountingAccount"), Ram + "ID"));
    }

    private static void ReadTotals(
        XElement? element,
        OrderResponse response,
        CiiValueReader values,
        Dictionary<XElement, InvoiceNode> owners)
    {
        if (element is null)
        {
            return;
        }

        response.Totals.LineTotalAmount = values.ReadAmount(In(values, element, Ram + "LineTotalAmount"));
        response.Totals.ChargeTotalAmount = values.ReadAmount(In(values, element, Ram + "ChargeTotalAmount"));
        response.Totals.AllowanceTotalAmount = values.ReadAmount(In(values, element, Ram + "AllowanceTotalAmount"));
        response.Totals.TaxExclusiveAmount = values.ReadAmount(In(values, element, Ram + "TaxBasisTotalAmount"));
        response.Totals.TaxAmount = values.ReadAmount(In(values, element, Ram + "TaxTotalAmount"));
        response.Totals.RoundingAmount = values.ReadAmount(In(values, element, Ram + "RoundingAmount"));
        response.Totals.TaxInclusiveAmount = values.ReadAmount(In(values, element, Ram + "GrandTotalAmount"));
        response.Totals.PrepaidAmount = values.ReadAmount(In(values, element, Ram + "TotalPrepaidAmount"));
        response.Totals.DuePayableAmount = values.ReadAmount(In(values, element, Ram + "DuePayableAmount"));
        response.TaxAmount = response.Totals.TaxAmount;

        owners[element] = response.Totals;
    }

    private static OrderResponseLine ReadLine(
        XElement element,
        CiiValueReader values,
        Dictionary<XElement, InvoiceNode> owners)
    {
        var line = new OrderResponseLine();

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
        line.SubstitutedItem = ReadItem(
            In(values, element, Ram + "SubstitutedReferencedProduct"), values, owners);
        line.SubstitutedIdentifier = line.SubstitutedItem?.SellerIdentifier ?? default;

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

        owners[element] = item;
        return item;
    }

    private static void ReadLineAgreement(
        XElement? agreement,
        OrderResponseLine line,
        CiiValueReader values,
        Dictionary<XElement, InvoiceNode> owners)
    {
        if (agreement is null)
        {
            return;
        }

        line.OrderLineReference = values.ReadIdentifier(
            In(values, In(values, agreement, Ram + "BuyerOrderReferencedDocument"), Ram + "LineID"));
        line.MaximumBackorderQuantity = values.ReadQuantity(
            In(values, agreement, Ram + "MaximumProductOrderableQuantity"));

        foreach (XElement document in AllIn(values, agreement, Ram + "AdditionalReferencedDocument"))
        {
            line.AdditionalDocuments.Add(ReadDocument(document, values, owners));
        }

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

        foreach (XElement applied in AllIn(values, gross, Ram + "AppliedTradeAllowanceCharge"))
        {
            price.Adjustments.Add(ReadAllowanceCharge(applied, values, owners));
        }

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
        OrderResponseLine line,
        CiiValueReader values,
        Dictionary<XElement, InvoiceNode> owners)
    {
        if (element is null)
        {
            return;
        }

        // The requested quantity is what was asked for; the agreed one is the answer. Keeping both is what
        // lets a buyer see that they will get eighty of the hundred they ordered.
        line.PartialDeliveryAccepted = values.ReadIndicator(
            In(values, element, Ram + "PartialDeliveryAllowedIndicator"));
        line.RequestedQuantity = values.ReadQuantity(In(values, element, Ram + "RequestedQuantity"));
        line.Quantity = values.ReadQuantity(In(values, element, Ram + "AgreedQuantity"));
        line.PackageQuantity = values.ReadQuantity(In(values, element, Ram + "PackageQuantity"));
        line.UnitsPerPackage = values.ReadQuantity(In(values, element, Ram + "PerPackageUnitQuantity"));

        var delivery = new OrderDelivery
        {
            Recipient = ReadParty(In(values, element, Ram + "ShipToTradeParty"), values, owners),
            Consignor = ReadParty(In(values, element, Ram + "ShipFromTradeParty"), values, owners),
        };

        ReadPromisedDelivery(In(values, element, Ram + "RequestedDeliverySupplyChainEvent"), delivery, values);
        delivery.RequestedDespatchAt = values.ReadDateTime(
            In(values, In(values, element, Ram + "RequestedDespatchSupplyChainEvent"), Ram + "OccurrenceDateTime"));

        if (delivery.Recipient is not null
            || delivery.Consignor is not null
            || delivery.PromisedAt.IsSet
            || delivery.PromisedFrom.IsSet
            || delivery.PromisedUntil.IsSet
            || delivery.RequestedDespatchAt.IsSet)
        {
            line.Delivery = delivery;
        }

        owners[element] = line.Delivery ?? (InvoiceNode)line;
    }

    private static void ReadLineSettlement(
        XElement? settlement,
        OrderResponseLine line,
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
    }
}
