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
        WriteNotes(order.Notes, writer);
        WritePeriod("EffectiveSpecifiedPeriod", order.ValidityPeriod, writer);
        writer.End();
    }

    private static void WriteNotes(IEnumerable<InvoiceNote> notes, OrderXDocument writer)
    {
        foreach (InvoiceNote note in notes)
        {
            writer.StartRam("IncludedNote", note.Extensions);
            writer.Text("Content", note.Text);
            writer.Code("SubjectCode", note.SubjectCode);
            writer.End();
        }
    }

    private static void WriteAgreement(Order order, OrderXDocument writer)
    {
        writer.StartRam("ApplicableHeaderTradeAgreement");
        writer.Text("BuyerReference", order.BuyerReference);
        WriteParty("SellerTradeParty", order.Seller, writer);
        WriteParty("BuyerTradeParty", order.Buyer, writer);
        WriteParty("BuyerRequisitionerTradeParty", order.Originator, writer);

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

        WriteReference("SellerOrderReferencedDocument", order.SalesOrderNumber, writer);
        WriteReference("BuyerOrderReferencedDocument", order.OrderReference, writer);
        WriteReference("QuotationReferencedDocument", order.QuotationReference, writer);
        WriteReference("ContractReferencedDocument", order.ContractReference, writer);
        WriteReference("RequisitionReferencedDocument", order.OriginatorReference, writer);

        foreach (AdditionalDocument document in order.AdditionalDocuments)
        {
            WriteDocument("AdditionalReferencedDocument", document, writer);
        }

        WriteReference("CatalogueReferencedDocument", order.CatalogueReference, writer);
        WriteReference("BlanketOrderReferencedDocument", order.BlanketOrderReference, writer);
        WriteReference("PreviousOrderChangeReferencedDocument", order.PreviousOrderChangeReference, writer);
        WriteReference("PreviousOrderResponseReferencedDocument", order.PreviousOrderResponseReference, writer);

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
            WriteParty("ShipToTradeParty", delivery.Recipient, writer);
            WriteParty("ShipFromTradeParty", delivery.Consignor, writer);
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
        WriteParty("InvoiceeTradeParty", order.Invoicee, writer);

        if (order.Payment is { } payment)
        {
            writer.StartRam("SpecifiedTradeSettlementPaymentMeans", payment.Extensions);
            writer.Code("TypeCode", payment.MeansTypeCode);
            writer.Text("Information", payment.MeansText);
            writer.End();
        }

        foreach (AllowanceCharge allowanceCharge in order.AllowancesAndCharges)
        {
            WriteAllowanceCharge(allowanceCharge, writer);
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
        writer.Amount("TaxTotalAmount", totals.TaxAmount);
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
        WriteNotes(line.Notes, writer);
        writer.End();

        WriteItem(line.Item, writer);
        WriteLineAgreement(line, writer);
        WriteLineDelivery(line, writer);
        WriteLineSettlement(line, writer);

        writer.End();
    }

    private static void WriteItem(OrderItem? item, OrderXDocument writer)
    {
        if (item is null)
        {
            return;
        }

        writer.StartRam("SpecifiedTradeProduct", item.Extensions);
        writer.Identifier("GlobalID", item.StandardIdentifier);
        writer.Identifier("SellerAssignedID", item.SellerIdentifier);
        writer.Identifier("BuyerAssignedID", item.BuyerIdentifier);
        writer.Text("Name", item.Name);
        writer.Text("Description", item.Description);
        writer.Identifier("BatchID", item.BatchIdentifier);
        writer.Text("BrandName", item.BrandName);

        foreach (OrderItemProperty characteristic in item.Characteristics)
        {
            writer.StartRam("ApplicableProductCharacteristic", characteristic.Extensions);
            writer.Code("TypeCode", characteristic.NameCode);
            writer.Text("Description", characteristic.Name);
            writer.Quantity("ValueMeasure", characteristic.ValueQuantity);
            writer.Text("Value", characteristic.Value);
            writer.End();
        }

        foreach (ItemClassification classification in item.Classifications.Where(c => c.Code.IsSet))
        {
            writer.StartRam("DesignatedProductClassification", classification.Extensions);
            writer.Code("ClassCode", classification.Code);
            writer.Text("ClassName", classification.Name);
            writer.End();
        }

        foreach (ItemInstance instance in item.Instances)
        {
            writer.StartRam("IndividualTradeProductInstance", instance.Extensions);
            writer.Identifier("BatchID", instance.LotIdentifier);
            writer.Identifier("SerialID", instance.SerialIdentifier);
            writer.End();
        }

        if (item.Packaging is { } packaging)
        {
            writer.StartRam("ApplicableSupplyChainPackaging", packaging.Extensions);
            writer.Code("TypeCode", packaging.TypeCode);

            if (packaging.Width.IsSet || packaging.Length.IsSet || packaging.Height.IsSet)
            {
                writer.StartRam("LinearSpatialDimension");
                writer.Quantity("WidthMeasure", packaging.Width);
                writer.Quantity("LengthMeasure", packaging.Length);
                writer.Quantity("HeightMeasure", packaging.Height);
                writer.End();
            }

            writer.End();
        }

        if (item.OriginCountryCode.IsSet)
        {
            writer.StartRam("OriginTradeCountry");
            writer.Code("ID", item.OriginCountryCode);
            writer.End();
        }

        if (item.SpecificationDocument is { } specification)
        {
            WriteDocument("AdditionalReferenceReferencedDocument", specification, writer);
        }

        writer.End();
    }

    private static void WriteLineAgreement(OrderLine line, OrderXDocument writer)
    {
        writer.StartRam("SpecifiedLineTradeAgreement");
        WriteParty("BuyerRequisitionerTradeParty", line.Originator, writer);

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
            WriteDocument("AdditionalReferencedDocument", document, writer);
        }

        WritePrice(line, writer);

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

    /// <summary>
    /// Writes the gross and net prices. A per-unit discount belongs to the gross price, because that is what
    /// makes the net follow from it; a line's allowances are amounts and go in the settlement.
    /// </summary>
    private static void WritePrice(OrderLine line, OrderXDocument writer)
    {
        if (line.Price is not { } price)
        {
            return;
        }

        if (price.GrossPrice.IsSet || price.Discount.IsSet || price.Adjustments.Count > 0)
        {
            writer.StartRam("GrossPriceProductTradePrice");
            writer.Amount("ChargeAmount", price.GrossPrice);
            writer.Quantity("BasisQuantity", price.BaseQuantity);

            // The list is the full account when there is one. A model filled from a syntax that carries only
            // BT-147 has no list, and the single discount stands in for it.
            if (price.Adjustments.Count > 0)
            {
                foreach (AllowanceCharge adjustment in price.Adjustments)
                {
                    WriteAppliedAdjustment(adjustment, writer);
                }
            }
            else if (price.Discount.IsSet)
            {
                writer.StartRam("AppliedTradeAllowanceCharge");
                writer.Indicator("ChargeIndicator", new IndicatorField(false));
                writer.Amount("ActualAmount", price.Discount);
                writer.End();
            }

            writer.End();
        }

        if (price.NetPrice.IsSet || price.BaseQuantity.IsSet)
        {
            writer.StartRam("NetPriceProductTradePrice", price.Extensions);
            writer.Amount("ChargeAmount", price.NetPrice);
            writer.Quantity("BasisQuantity", price.BaseQuantity);
            writer.End();
        }
    }

    private static void WriteAppliedAdjustment(AllowanceCharge adjustment, OrderXDocument writer)
    {
        writer.StartRam("AppliedTradeAllowanceCharge", adjustment.Extensions);
        writer.Indicator("ChargeIndicator", new IndicatorField(adjustment.IsCharge));
        writer.Decimal("CalculationPercent", adjustment.Percentage);
        writer.Amount("BasisAmount", adjustment.BaseAmount);
        writer.Amount("ActualAmount", adjustment.Amount);
        writer.Code("ReasonCode", adjustment.ReasonCode);
        writer.Text("Reason", adjustment.Reason);
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
            WriteParty("ShipToTradeParty", delivery.Recipient, writer);
            WriteParty("ShipFromTradeParty", delivery.Consignor, writer);

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
            WriteAllowanceCharge(allowanceCharge, writer);
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

    private static void WriteAllowanceCharge(AllowanceCharge allowanceCharge, OrderXDocument writer)
    {
        writer.StartRam("SpecifiedTradeAllowanceCharge", allowanceCharge.Extensions);
        writer.Indicator("ChargeIndicator", new IndicatorField(allowanceCharge.IsCharge));
        writer.Decimal("CalculationPercent", allowanceCharge.Percentage);
        writer.Amount("BasisAmount", allowanceCharge.BaseAmount);
        writer.Amount("ActualAmount", allowanceCharge.Amount);
        writer.Code("ReasonCode", allowanceCharge.ReasonCode);
        writer.Text("Reason", allowanceCharge.Reason);

        if (allowanceCharge.VatCategoryCode.IsSet || allowanceCharge.VatRate.IsSet)
        {
            writer.StartRam("CategoryTradeTax");
            writer.Ram("TypeCode", "VAT");
            writer.Code("CategoryCode", allowanceCharge.VatCategoryCode);
            writer.Decimal("RateApplicablePercent", allowanceCharge.VatRate);
            writer.End();
        }

        writer.End();
    }

    private static void WriteParty(string elementName, Party? party, OrderXDocument writer)
    {
        if (party is null)
        {
            return;
        }

        writer.StartRam(elementName, party.Extensions);

        // A party's identifiers are one list in the model, and two elements in CII: a GLN or another scheme
        // the parties agreed goes in GlobalID, and everything else in ID.
        foreach (IdentifierField identifier in party.Identifiers.Where(id => id.IsSet && id.SchemeId is null))
        {
            writer.Identifier("ID", identifier);
        }

        foreach (IdentifierField identifier in party.Identifiers.Where(id => id.IsSet && id.SchemeId is not null))
        {
            writer.Identifier("GlobalID", identifier);
        }

        writer.Text("Name", party.Name);
        writer.Text("Description", party.AdditionalLegalInformation);

        if (party.LegalRegistrationIdentifier.IsSet || party.TradingName.IsSet)
        {
            writer.StartRam("SpecifiedLegalOrganization");
            writer.Identifier("ID", party.LegalRegistrationIdentifier);
            writer.Text("TradingBusinessName", party.TradingName);
            writer.End();
        }

        WriteContact(party.Contact, writer);
        WriteAddress(party.Address, writer);

        if (party.ElectronicAddress.IsSet)
        {
            writer.StartRam("URIUniversalCommunication");
            writer.Identifier("URIID", party.ElectronicAddress);
            writer.End();
        }

        WriteTaxRegistration(party.VatIdentifier, "VA", writer);
        WriteTaxRegistration(party.TaxRegistrationIdentifier, "FC", writer);

        writer.End();
    }

    private static void WriteTaxRegistration(IdentifierField identifier, string scheme, OrderXDocument writer)
    {
        if (!identifier.IsSet)
        {
            return;
        }

        writer.StartRam("SpecifiedTaxRegistration");
        writer.Identifier("ID", identifier.SchemeId is null ? identifier with { SchemeId = scheme } : identifier);
        writer.End();
    }

    private static void WriteContact(Contact? contact, OrderXDocument writer)
    {
        if (contact is null)
        {
            return;
        }

        writer.StartRam("DefinedTradeContact", contact.Extensions);
        writer.Text("PersonName", contact.Name);
        writer.Text("DepartmentName", contact.Department);
        writer.Code("TypeCode", contact.TypeCode);

        if (contact.Telephone.IsSet)
        {
            writer.StartRam("TelephoneUniversalCommunication");
            writer.Text("CompleteNumber", contact.Telephone);
            writer.End();
        }

        if (contact.Email.IsSet)
        {
            writer.StartRam("EmailURIUniversalCommunication");
            writer.Text("URIID", contact.Email);
            writer.End();
        }

        writer.End();
    }

    private static void WriteAddress(PostalAddress? address, OrderXDocument writer)
    {
        if (address is null)
        {
            return;
        }

        writer.StartRam("PostalTradeAddress", address.Extensions);
        writer.Text("PostcodeCode", address.PostCode);
        writer.Text("LineOne", address.Line1);
        writer.Text("LineTwo", address.Line2);
        writer.Text("LineThree", address.Line3);
        writer.Text("CityName", address.City);
        writer.Code("CountryID", address.CountryCode);
        writer.Text("CountrySubDivisionName", address.CountrySubdivision);
        writer.End();
    }

    private static void WriteReference(string elementName, IdentifierField identifier, OrderXDocument writer)
    {
        if (!identifier.IsSet)
        {
            return;
        }

        writer.StartRam(elementName);
        writer.Identifier("IssuerAssignedID", identifier);
        writer.End();
    }

    private static void WriteDocument(string elementName, AdditionalDocument document, OrderXDocument writer)
    {
        writer.StartRam(elementName, document.Extensions);
        writer.Identifier("IssuerAssignedID", document.Identifier);
        writer.Text("URIID", document.ExternalLocation);
        writer.Identifier("LineID", document.LineReference);
        writer.Code("TypeCode", document.TypeCode);
        writer.Text("Name", document.Description);
        writer.Code("ReferenceTypeCode", document.ReferenceTypeCode);
        writer.End();
    }

    private static void WritePeriod(string elementName, InvoicingPeriod? period, OrderXDocument writer)
    {
        if (period is null)
        {
            return;
        }

        writer.StartRam(elementName, period.Extensions);
        WriteDate("StartDateTime", period.StartDate, writer);
        WriteDate("EndDateTime", period.EndDate, writer);
        writer.End();
    }

    private static void WriteDate(string elementName, DateField field, OrderXDocument writer)
    {
        if (field.IsSet)
        {
            writer.Moment(
                elementName,
                new DateTimeField(
                    field.Value is { } date ? new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero) : null,
                    field.FormatCode ?? DateField.FormatCcyyMmDd,
                    field.Source));
        }
    }
}
