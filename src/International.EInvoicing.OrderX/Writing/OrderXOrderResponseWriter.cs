using System.Text;
using System.Xml;
using International.EInvoicing.Documents;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;
using International.EInvoicing.Values;

namespace International.EInvoicing.OrderX.Writing;

/// <summary>
/// Writes an order response as Order-X.
/// </summary>
/// <remarks>
/// Every element goes out in the sequence the Cross Industry Order schema declares, read out of the schema
/// rather than inferred: the order is normative, so a plausible sequence is a rejected document.
/// </remarks>
public sealed class OrderXOrderResponseWriter : IDocumentWriter<OrderResponse>
{
    /// <inheritdoc />
    public DocumentSyntax Syntax => DocumentSyntax.OrderX;

    /// <inheritdoc />
    public void Write(OrderResponse document, Stream destination)
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
    public string WriteToString(OrderResponse document)
    {
        ArgumentNullException.ThrowIfNull(document);

        using var buffer = new MemoryStream();
        Write(document, buffer);
        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    /// <inheritdoc />
    public async Task WriteAsync(
        OrderResponse document,
        Stream destination,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(destination);

        using var buffer = new MemoryStream();
        Write(document, buffer);
        buffer.Position = 0;

        await buffer.CopyToAsync(destination, cancellationToken).ConfigureAwait(false);
    }

    private static void Write(OrderResponse response, OrderXDocument writer)
    {
        writer.StartDocument();
        writer.Node(response.Extensions);

        WriteContext(response, writer);
        WriteExchangedDocument(response, writer);

        writer.StartRsm("SupplyChainTradeTransaction");

        foreach (OrderResponseLine line in response.Lines)
        {
            WriteLine(line, writer);
        }

        WriteAgreement(response, writer);
        WriteDelivery(response, writer);
        WriteSettlement(response, writer);

        writer.End();
        writer.EndDocument();
    }

    private static void WriteContext(OrderResponse response, OrderXDocument writer)
    {
        writer.StartRsm("ExchangedDocumentContext");
        writer.Indicator("TestIndicator", response.IsTest);

        if (response.BusinessProcessType.IsSet)
        {
            writer.StartRam("BusinessProcessSpecifiedDocumentContextParameter");
            writer.Identifier("ID", response.BusinessProcessType);
            writer.End();
        }

        writer.StartRam("GuidelineSpecifiedDocumentContextParameter");
        writer.Ram("ID", response.SpecificationIdentifier.Value ?? OrderXProfiles.Comfort.Id.Value);
        writer.End();

        writer.End();
    }

    private static void WriteExchangedDocument(OrderResponse response, OrderXDocument writer)
    {
        writer.StartRsm("ExchangedDocument");
        writer.Identifier("ID", response.Number);
        writer.Text("Name", response.Name);
        writer.Code(
            "TypeCode",
            response.TypeCode.IsSet ? response.TypeCode : new CodeField(OrderXTypeCodes.OrderResponse));
        writer.Code("StatusCode", response.ResponseCode);
        writer.Moment("IssueDateTime", response.IssuedAt);
        writer.Indicator("CopyIndicator", response.IsCopy);
        writer.Code("PurposeCode", response.PurposeCode);
        writer.Code("RequestedResponseTypeCode", response.RequestedResponseTypeCode);
        OrderXCommon.WriteNotes(response.Notes, writer);
        OrderXCommon.WritePeriod("EffectiveSpecifiedPeriod", response.ValidityPeriod, writer);
        writer.End();
    }

    private static void WriteAgreement(OrderResponse response, OrderXDocument writer)
    {
        writer.StartRam("ApplicableHeaderTradeAgreement");
        writer.Text("BuyerReference", response.BuyerReference);
        OrderXCommon.WriteParty("SellerTradeParty", response.Seller, writer);
        OrderXCommon.WriteParty("BuyerTradeParty", response.Buyer, writer);
        OrderXCommon.WriteParty("BuyerRequisitionerTradeParty", response.Originator, writer);

        if (response.DeliveryTermsCode.IsSet
            || response.DeliveryTerms.IsSet
            || response.DeliveryTermsFunctionCode.IsSet
            || response.DeliveryTermsLocation.IsSet
            || response.DeliveryTermsLocationName.IsSet)
        {
            writer.StartRam("ApplicableTradeDeliveryTerms");
            writer.Identifier("DeliveryTypeCode", response.DeliveryTermsCode);
            writer.Text("Description", response.DeliveryTerms);
            writer.Code("FunctionCode", response.DeliveryTermsFunctionCode);

            if (response.DeliveryTermsLocation.IsSet || response.DeliveryTermsLocationName.IsSet)
            {
                writer.StartRam("RelevantTradeLocation");
                writer.Identifier("ID", response.DeliveryTermsLocation);
                writer.Text("Name", response.DeliveryTermsLocationName);
                writer.End();
            }

            writer.End();
        }

        OrderXCommon.WriteReference("SellerOrderReferencedDocument", response.SalesOrderNumber, writer);
        OrderXCommon.WriteReference("BuyerOrderReferencedDocument", response.OrderReference, writer);
        OrderXCommon.WriteReference("QuotationReferencedDocument", response.QuotationReference, writer);
        OrderXCommon.WriteReference("ContractReferencedDocument", response.ContractReference, writer);
        OrderXCommon.WriteReference("RequisitionReferencedDocument", response.OriginatorReference, writer);

        foreach (AdditionalDocument document in response.AdditionalDocuments)
        {
            OrderXCommon.WriteDocument("AdditionalReferencedDocument", document, writer);
        }

        OrderXCommon.WriteReference("CatalogueReferencedDocument", response.CatalogueReference, writer);
        OrderXCommon.WriteReference("BlanketOrderReferencedDocument", response.BlanketOrderReference, writer);
        OrderXCommon.WriteReference(
            "PreviousOrderChangeReferencedDocument", response.OrderChangeReference, writer);
        OrderXCommon.WriteReference(
            "PreviousOrderResponseReferencedDocument", response.PreviousOrderResponseReference, writer);

        if (response.ProjectReference.IsSet || response.ProjectName.IsSet)
        {
            writer.StartRam("SpecifiedProcuringProject");
            writer.Identifier("ID", response.ProjectReference);
            writer.Text("Name", response.ProjectName);
            writer.End();
        }

        writer.End();
    }

    private static void WriteDelivery(OrderResponse response, OrderXDocument writer)
    {
        writer.StartRam("ApplicableHeaderTradeDelivery");

        if (response.Delivery is { } delivery)
        {
            writer.Node(delivery.Extensions);
            OrderXCommon.WriteParty("ShipToTradeParty", delivery.Recipient, writer);
            OrderXCommon.WriteParty("ShipFromTradeParty", delivery.Consignor, writer);
            OrderXCommon.WritePromisedDelivery(delivery, writer);

            if (delivery.RequestedDespatchAt.IsSet)
            {
                writer.StartRam("RequestedDespatchSupplyChainEvent");
                writer.Moment("OccurrenceDateTime", delivery.RequestedDespatchAt);
                writer.End();
            }
        }

        writer.End();
    }

    private static void WriteSettlement(OrderResponse response, OrderXDocument writer)
    {
        writer.StartRam("ApplicableHeaderTradeSettlement");
        writer.Code("OrderCurrencyCode", response.CurrencyCode);
        OrderXCommon.WriteParty("InvoiceeTradeParty", response.Invoicee, writer);

        if (response.Payment is { } payment)
        {
            writer.StartRam("SpecifiedTradeSettlementPaymentMeans", payment.Extensions);
            writer.Code("TypeCode", payment.MeansTypeCode);
            writer.Text("Information", payment.MeansText);
            writer.End();
        }

        foreach (VatBreakdownEntry entry in response.VatBreakdown)
        {
            writer.StartRam("ApplicableTradeTax", entry.Extensions);
            writer.Amount("CalculatedAmount", entry.TaxAmount);
            writer.Ram("TypeCode", "VAT");
            writer.Amount("BasisAmount", entry.TaxableAmount);
            writer.Code("CategoryCode", entry.CategoryCode);
            writer.Decimal("RateApplicablePercent", entry.Rate);
            writer.End();
        }

        foreach (AllowanceCharge allowanceCharge in response.AllowancesAndCharges)
        {
            OrderXCommon.WriteAllowanceCharge(allowanceCharge, writer);
        }

        if (response.PaymentTerms.IsSet)
        {
            writer.StartRam("SpecifiedTradePaymentTerms");
            writer.Text("Description", response.PaymentTerms);
            writer.End();
        }

        DocumentTotals totals = response.Totals;
        writer.StartRam("SpecifiedTradeSettlementHeaderMonetarySummation", totals.Extensions);
        writer.Amount("LineTotalAmount", totals.LineTotalAmount);
        writer.Amount("ChargeTotalAmount", totals.ChargeTotalAmount);
        writer.Amount("AllowanceTotalAmount", totals.AllowanceTotalAmount);
        writer.Amount("TaxBasisTotalAmount", totals.TaxExclusiveAmount);
        writer.Amount("TaxTotalAmount", totals.TaxAmount, withCurrency: true, response.CurrencyCode.Value);
        writer.Amount("RoundingAmount", totals.RoundingAmount);
        writer.Amount("GrandTotalAmount", totals.TaxInclusiveAmount);
        writer.Amount("TotalPrepaidAmount", totals.PrepaidAmount);
        writer.Amount("DuePayableAmount", totals.DuePayableAmount);
        writer.End();

        if (response.AccountingReference.IsSet)
        {
            writer.StartRam("ReceivableSpecifiedTradeAccountingAccount");
            writer.Text("ID", response.AccountingReference);
            writer.End();
        }

        writer.End();
    }

    private static void WriteLine(OrderResponseLine line, OrderXDocument writer)
    {
        writer.StartRam("IncludedSupplyChainTradeLineItem", line.Extensions);

        writer.StartRam("AssociatedDocumentLineDocument");
        writer.Identifier("LineID", line.Identifier);
        writer.Code("LineStatusCode", line.StatusCode);
        OrderXCommon.WriteNotes(line.Notes, writer);
        writer.End();

        OrderXCommon.WriteItem("SpecifiedTradeProduct", line.Item, writer);
        OrderXCommon.WriteItem("SubstitutedReferencedProduct", line.SubstitutedItem, writer);

        WriteLineAgreement(line, writer);
        WriteLineDelivery(line, writer);
        WriteLineSettlement(line, writer);

        writer.End();
    }

    private static void WriteLineAgreement(OrderResponseLine line, OrderXDocument writer)
    {
        writer.StartRam("SpecifiedLineTradeAgreement");
        writer.Quantity("MaximumProductOrderableQuantity", line.MaximumBackorderQuantity);

        if (line.OrderLineReference.IsSet)
        {
            writer.StartRam("BuyerOrderReferencedDocument");
            writer.Identifier("LineID", line.OrderLineReference);
            writer.End();
        }

        foreach (AdditionalDocument document in line.AdditionalDocuments)
        {
            OrderXCommon.WriteDocument("AdditionalReferencedDocument", document, writer);
        }

        OrderXCommon.WritePrice(line.Price, writer);
        writer.End();
    }

    private static void WriteLineDelivery(OrderResponseLine line, OrderXDocument writer)
    {
        writer.StartRam("SpecifiedLineTradeDelivery");
        writer.Indicator("PartialDeliveryAllowedIndicator", line.PartialDeliveryAccepted);
        writer.Quantity("RequestedQuantity", line.RequestedQuantity);
        writer.Quantity("AgreedQuantity", line.Quantity);
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

            OrderXCommon.WritePromisedDelivery(delivery, writer);
        }

        writer.End();
    }

    private static void WriteLineSettlement(OrderResponseLine line, OrderXDocument writer)
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

        writer.End();
    }
}
