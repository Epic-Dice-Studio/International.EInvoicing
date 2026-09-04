using System.Text;
using System.Xml;
using International.EInvoicing.Documents;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;
using International.EInvoicing.Values;
using International.EInvoicing.Xml;

namespace International.EInvoicing.OrderX.Writing;

/// <summary>
/// Writes an order, or an order change, as Order-X.
/// </summary>
/// <remarks>
/// Every element goes out in the sequence the Cross Industry Order schema declares, which was read out of
/// the schema rather than inferred from a sample: the order is normative, so a document written in a
/// plausible order is one a receiver rejects.
/// </remarks>
public sealed class OrderXOrderWriter : IDocumentWriter<Order>
{
    /// <inheritdoc />
    public DocumentSyntax Syntax => DocumentSyntax.OrderX;

    /// <inheritdoc />
    public void Write(Order document, Stream destination)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(destination);

        var settings = new XmlWriterSettings
        {
            Indent = true,
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            CloseOutput = false,
        };

        using XmlWriter xml = XmlWriter.Create(destination, settings);
        using var writer = new OrderXDocument(xml);

        Write(document, writer);
        xml.Flush();
    }

    /// <inheritdoc />
    public string WriteToString(Order document)
    {
        ArgumentNullException.ThrowIfNull(document);

        using var buffer = new MemoryStream();
        Write(document, buffer);
        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    /// <inheritdoc />
    public async Task WriteAsync(Order document, Stream destination, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(destination);

        using var buffer = new MemoryStream();
        Write(document, buffer);
        buffer.Position = 0;

        await buffer.CopyToAsync(destination, cancellationToken).ConfigureAwait(false);
    }

    private static void Write(Order order, OrderXDocument writer)
    {
        writer.StartDocument();
        writer.Node(order.Extensions);

        WriteContext(order, writer);
        WriteExchangedDocument(order, writer);

        writer.StartRsm("SupplyChainTradeTransaction");

        foreach (OrderLine line in order.Lines)
        {
            WriteLine(line, writer);
        }

        WriteAgreement(order, writer);
        WriteDelivery(order, writer);
        WriteSettlement(order, writer);

        writer.End();
        writer.EndDocument();
    }

    private static void WriteContext(Order order, OrderXDocument writer)
    {
        writer.StartRsm("ExchangedDocumentContext");
        writer.Indicator("TestIndicator", order.IsTest);

        if (order.BusinessProcessType.IsSet)
        {
            writer.StartRam("BusinessProcessSpecifiedDocumentContextParameter");
            writer.Identifier("ID", order.BusinessProcessType);
            writer.End();
        }

        writer.StartRam("GuidelineSpecifiedDocumentContextParameter");
        writer.Ram("ID", order.SpecificationIdentifier.Value ?? OrderXProfiles.Comfort.Id.Value);
        writer.End();

        writer.End();
    }

    private static void WriteExchangedDocument(Order order, OrderXDocument writer)
    {
        writer.StartRsm("ExchangedDocument");
        writer.Identifier("ID", order.Number);
        writer.Text("Name", order.Name);
        writer.Code("TypeCode", order.TypeCode.IsSet ? order.TypeCode : new CodeField(OrderXTypeCodes.Order));
        writer.Moment("IssueDateTime", order.IssuedAt);
        writer.Indicator("CopyIndicator", order.IsCopy);
        writer.Code("PurposeCode", order.PurposeCode);
        writer.Code("RequestedResponseTypeCode", order.RequestedResponseTypeCode);
        OrderXCommon.WriteNotes(order.Notes, writer);
        OrderXCommon.WritePeriod("EffectiveSpecifiedPeriod", order.ValidityPeriod, writer);
        writer.End();
    }


    private static void WriteAgreement(Order order, OrderXDocument writer)
    {
        writer.StartRam("ApplicableHeaderTradeAgreement");
        writer.Text("BuyerReference", order.BuyerReference);
        OrderXCommon.WriteParty("SellerTradeParty", order.Seller, writer);
        OrderXCommon.WriteParty("BuyerTradeParty", order.Buyer, writer);
        OrderXCommon.WriteParty("BuyerRequisitionerTradeParty", order.Originator, writer);

        if (order.DeliveryTermsCode.IsSet
            || order.DeliveryTerms.IsSet
            || order.DeliveryTermsFunctionCode.IsSet
            || order.DeliveryTermsLocation.IsSet
            || order.DeliveryTermsLocationName.IsSet)
        {
            writer.StartRam("ApplicableTradeDeliveryTerms");
            writer.Identifier("DeliveryTypeCode", order.DeliveryTermsCode);
            writer.Text("Description", order.DeliveryTerms);
            writer.Code("FunctionCode", order.DeliveryTermsFunctionCode);

            if (order.DeliveryTermsLocation.IsSet || order.DeliveryTermsLocationName.IsSet)
            {
                writer.StartRam("RelevantTradeLocation");
                writer.Identifier("ID", order.DeliveryTermsLocation);
                writer.Text("Name", order.DeliveryTermsLocationName);
                writer.End();
            }

            writer.End();
        }

        OrderXCommon.WriteReference("SellerOrderReferencedDocument", order.SalesOrderNumber, writer);
        OrderXCommon.WriteReference("BuyerOrderReferencedDocument", order.OrderReference, writer);
        OrderXCommon.WriteReference("QuotationReferencedDocument", order.QuotationReference, writer);
        OrderXCommon.WriteReference("ContractReferencedDocument", order.ContractReference, writer);
        OrderXCommon.WriteReference("RequisitionReferencedDocument", order.OriginatorReference, writer);

        foreach (AdditionalDocument document in order.AdditionalDocuments)
        {
            OrderXCommon.WriteDocument("AdditionalReferencedDocument", document, writer);
        }

        OrderXCommon.WriteReference("CatalogueReferencedDocument", order.CatalogueReference, writer);
        OrderXCommon.WriteReference("BlanketOrderReferencedDocument", order.BlanketOrderReference, writer);
        OrderXCommon.WriteReference("PreviousOrderChangeReferencedDocument", order.PreviousOrderChangeReference, writer);
        OrderXCommon.WriteReference("PreviousOrderResponseReferencedDocument", order.PreviousOrderResponseReference, writer);

        if (order.ProjectReference.IsSet || order.ProjectName.IsSet)
        {
            writer.StartRam("SpecifiedProcuringProject");
            writer.Identifier("ID", order.ProjectReference);
            writer.Text("Name", order.ProjectName);
            writer.End();
        }

        writer.End();
    }

    private static void WriteDelivery(Order order, OrderXDocument writer)
    {
        OrderDelivery? delivery = order.Delivery;

        writer.StartRam("ApplicableHeaderTradeDelivery");

        if (delivery is not null)
        {
            writer.Node(delivery.Extensions);
            OrderXCommon.WriteParty("ShipToTradeParty", delivery.Recipient, writer);
            OrderXCommon.WriteParty("ShipFromTradeParty", delivery.Consignor, writer);
            WriteRequestedDelivery(delivery, writer);

            if (delivery.RequestedDespatchAt.IsSet)
            {
                writer.StartRam("RequestedDespatchSupplyChainEvent");
                writer.Moment("OccurrenceDateTime", delivery.RequestedDespatchAt);
                writer.End();
            }
        }

        writer.End();
    }

    /// <summary>
    /// Writes when delivery is wanted: a moment when the two ends are the same, a window when they differ.
    /// </summary>
    /// <remarks>
    /// Order-X allows both and the model keeps one pair of fields, so which of the two is written is decided
    /// by what the pair says rather than by what the document happened to arrive as.
    /// </remarks>
    private static void WriteRequestedDelivery(OrderDelivery delivery, OrderXDocument writer)
    {
        if (!delivery.RequestedFrom.IsSet && !delivery.RequestedUntil.IsSet && !delivery.RequestedAt.IsSet)
        {
            return;
        }

        writer.StartRam("RequestedDeliverySupplyChainEvent");

        writer.Moment("OccurrenceDateTime", delivery.RequestedAt);

        if (delivery.RequestedFrom.IsSet || delivery.RequestedUntil.IsSet)
        {
            writer.StartRam("OccurrenceSpecifiedPeriod");
            writer.Moment("StartDateTime", delivery.RequestedFrom);
            writer.Moment("EndDateTime", delivery.RequestedUntil);
            writer.End();
        }

        writer.End();
    }

    private static void WriteSettlement(Order order, OrderXDocument writer)
    {
        writer.StartRam("ApplicableHeaderTradeSettlement");
        writer.Code("OrderCurrencyCode", order.CurrencyCode);
        OrderXCommon.WriteParty("InvoiceeTradeParty", order.Invoicee, writer);

        if (order.Payment is { } payment)
        {
            writer.StartRam("SpecifiedTradeSettlementPaymentMeans", payment.Extensions);
            writer.Code("TypeCode", payment.MeansTypeCode);
            writer.Text("Information", payment.MeansText);
            writer.End();
        }

        foreach (AllowanceCharge allowanceCharge in order.AllowancesAndCharges)
        {
            OrderXCommon.WriteAllowanceCharge(allowanceCharge, writer);
        }

        if (order.PaymentTerms.IsSet)
        {
            writer.StartRam("SpecifiedTradePaymentTerms");
            writer.Text("Description", order.PaymentTerms);
            writer.End();
        }

        WriteTotals(order, writer);

        if (order.AccountingReference.IsSet)
        {
            writer.StartRam("ReceivableSpecifiedTradeAccountingAccount");
            writer.Text("ID", order.AccountingReference);
            writer.End();
        }

        writer.End();
    }

    private static void WriteTotals(Order order, OrderXDocument writer)
    {
        DocumentTotals totals = order.Totals;

        writer.StartRam("SpecifiedTradeSettlementHeaderMonetarySummation", totals.Extensions);
        writer.Amount("LineTotalAmount", totals.LineTotalAmount);
        writer.Amount("ChargeTotalAmount", totals.ChargeTotalAmount);
        writer.Amount("AllowanceTotalAmount", totals.AllowanceTotalAmount);
        writer.Amount("TaxBasisTotalAmount", totals.TaxExclusiveAmount);
        writer.Amount("TaxTotalAmount", totals.TaxAmount, withCurrency: true, order.CurrencyCode.Value);
        writer.Amount("RoundingAmount", totals.RoundingAmount);
        writer.Amount("GrandTotalAmount", totals.TaxInclusiveAmount);
        writer.Amount("TotalPrepaidAmount", totals.PrepaidAmount);
        writer.Amount("DuePayableAmount", totals.DuePayableAmount);
        writer.End();
    }

    private static void WriteLine(OrderLine line, OrderXDocument writer)
    {
        writer.StartRam("IncludedSupplyChainTradeLineItem", line.Extensions);

        writer.StartRam("AssociatedDocumentLineDocument");
        writer.Identifier("LineID", line.Identifier);
        writer.Code("LineStatusCode", line.StatusCode);
        OrderXCommon.WriteNotes(line.Notes, writer);
        writer.End();

        OrderXCommon.WriteItem("SpecifiedTradeProduct", line.Item, writer);
        WriteLineAgreement(line, writer);
        WriteLineDelivery(line, writer);
        WriteLineSettlement(line, writer);

        writer.End();
    }


    private static void WriteLineAgreement(OrderLine line, OrderXDocument writer)
    {
        writer.StartRam("SpecifiedLineTradeAgreement");
        OrderXCommon.WriteParty("BuyerRequisitionerTradeParty", line.Originator, writer);

        if (line.OrderLineReference.IsSet)
        {
            writer.StartRam("BuyerOrderReferencedDocument");
            writer.Identifier("LineID", line.OrderLineReference);
            writer.End();
        }

        if (line.QuotationReference.IsSet || line.QuotationLineReference.IsSet)
        {
            writer.StartRam("QuotationReferencedDocument");
            writer.Identifier("IssuerAssignedID", line.QuotationReference);
            writer.Identifier("LineID", line.QuotationLineReference);
            writer.End();
        }

        foreach (AdditionalDocument document in line.AdditionalDocuments)
        {
            OrderXCommon.WriteDocument("AdditionalReferencedDocument", document, writer);
        }

        OrderXCommon.WritePrice(line.Price, writer);

        if (line.CatalogueReference.IsSet || line.CatalogueLineReference.IsSet)
        {
            writer.StartRam("CatalogueReferencedDocument");
            writer.Identifier("IssuerAssignedID", line.CatalogueReference);
            writer.Identifier("LineID", line.CatalogueLineReference);
            writer.End();
        }

        if (line.BlanketOrderLineReference.IsSet)
        {
            writer.StartRam("BlanketOrderReferencedDocument");
            writer.Identifier("LineID", line.BlanketOrderLineReference);
            writer.End();
        }

        writer.End();
    }

    private static void WriteLineDelivery(OrderLine line, OrderXDocument writer)
    {
        writer.StartRam("SpecifiedLineTradeDelivery");
        writer.Indicator("PartialDeliveryAllowedIndicator", line.PartialDeliveryAccepted);
        writer.Quantity("RequestedQuantity", line.Quantity);
        writer.Quantity("PackageQuantity", line.PackageQuantity);
        writer.Quantity("PerPackageUnitQuantity", line.UnitsPerPackage);

        if (line.Delivery is { } delivery)
        {
            OrderXCommon.WriteParty("ShipToTradeParty", delivery.Recipient, writer);
            OrderXCommon.WriteParty("ShipFromTradeParty", delivery.Consignor, writer);

            if (delivery.RequestedDespatchAt.IsSet)
            {
                writer.StartRam("RequestedDespatchSupplyChainEvent");
                writer.Moment("OccurrenceDateTime", delivery.RequestedDespatchAt);
                writer.End();
            }

            WriteRequestedDelivery(delivery, writer);
        }

        writer.End();
    }

    private static void WriteLineSettlement(OrderLine line, OrderXDocument writer)
    {
        writer.StartRam("SpecifiedLineTradeSettlement");

        if (line.Item is { } item && (item.VatCategoryCode.IsSet || item.VatRate.IsSet))
        {
            writer.StartRam("ApplicableTradeTax");
            writer.Ram("TypeCode", "VAT");
            writer.Code("CategoryCode", item.VatCategoryCode);
            writer.Decimal("RateApplicablePercent", item.VatRate);
            writer.End();
        }

        foreach (AllowanceCharge allowanceCharge in line.AllowancesAndCharges)
        {
            OrderXCommon.WriteAllowanceCharge(allowanceCharge, writer);
        }

        if (line.NetAmount.IsSet)
        {
            writer.StartRam("SpecifiedTradeSettlementLineMonetarySummation");
            writer.Amount("LineTotalAmount", line.NetAmount);
            writer.End();
        }

        if (line.AccountingReference.IsSet)
        {
            writer.StartRam("ReceivableSpecifiedTradeAccountingAccount");
            writer.Text("ID", line.AccountingReference);
            writer.End();
        }

        writer.End();
    }









}
