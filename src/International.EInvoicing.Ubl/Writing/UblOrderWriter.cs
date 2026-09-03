using System.Diagnostics.CodeAnalysis;
using International.EInvoicing.Documents;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;
using International.EInvoicing.Values;

namespace International.EInvoicing.Ubl.Writing;

/// <summary>
/// Writes an order as UBL 2.1.
/// </summary>
/// <remarks>
/// Element order is normative in UBL, so this writer is explicit rather than generated: the order below
/// follows <c>UBL-Order-2.1.xsd</c> and the <c>OrderLineType</c>, <c>LineItemType</c> and
/// <c>DeliveryType</c> sequences of the common components.
/// </remarks>
[SuppressMessage(
    "Performance",
    "CA1822:Mark members as static",
    Justification = "An instance API so a caller can replace this writer through the registry.")]
public sealed class UblOrderWriter : IDocumentWriter<Order>
{
    /// <inheritdoc />
    public DocumentSyntax Syntax => DocumentSyntax.Ubl;

    /// <summary>Writes <paramref name="document"/> to <paramref name="destination"/>. The stream is left open.</summary>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    public void Write(Order document, Stream destination)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(destination);

        using var writer = UblDocument.Open(
            destination,
            UblOrderNames.RootElement,
            UblOrderNames.Order.NamespaceName);

        Write(document, writer);
    }

    /// <summary>Writes <paramref name="document"/> and returns it as XML text.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> is <c>null</c>.</exception>
    public string WriteToString(Order document)
    {
        ArgumentNullException.ThrowIfNull(document);

        using var buffer = new MemoryStream();
        Write(document, buffer);
        return System.Text.Encoding.UTF8.GetString(buffer.ToArray());
    }

    /// <inheritdoc />
    public Task WriteAsync(Order document, Stream destination, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(destination);

        return DocumentStreams.WriteAllAsync(WriteToString(document), destination, cancellationToken);
    }

    private static void Write(Order order, UblDocument writer)
    {
        string? currency = order.CurrencyCode.Value ?? order.CurrencyCode.Raw;

        if (order.SpecificationIdentifier.IsDeclared)
        {
            writer.Cbc("CustomizationID", order.SpecificationIdentifier.Value);
        }

        writer.Identifier("ProfileID", order.BusinessProcessType);
        writer.Identifier("ID", order.Number);
        writer.Identifier("SalesOrderID", order.SalesOrderNumber);
        writer.Moment("IssueDate", "IssueTime", order.IssuedAt);
        writer.Code("OrderTypeCode", order.TypeCode);
        writer.Text("Note", order.Note);
        writer.Code("DocumentCurrencyCode", order.CurrencyCode);
        writer.Text("CustomerReference", order.BuyerReference);
        writer.Text("AccountingCost", order.AccountingReference);

        WritePeriod("ValidityPeriod", order.ValidityPeriod, writer);

        WriteReference("QuotationDocumentReference", order.QuotationReference, writer);
        WriteReference("OrderDocumentReference", order.OrderReference, writer);
        WriteReference("OriginatorDocumentReference", order.OriginatorReference, writer);
        WriteReference("CatalogueReference", order.CatalogueReference, writer);

        foreach (AdditionalDocument document in order.AdditionalDocuments)
        {
            WriteAttachment(document, writer);
        }

        WriteReference("Contract", order.ContractReference, writer);
        WriteReference("ProjectReference", order.ProjectReference, writer);

        WriteWrappedParty(order.Buyer, "BuyerCustomerParty", writer);
        WriteWrappedParty(order.Seller, "SellerSupplierParty", writer);
        WriteWrappedParty(order.Originator, "OriginatorCustomerParty", writer);
        WriteWrappedParty(order.Invoicee, "AccountingCustomerParty", writer);

        WriteDelivery(order.Delivery, writer);
        WriteDeliveryTerms(order, writer);

        if (order.PaymentTerms.IsSet)
        {
            writer.StartCac("PaymentTerms");
            writer.Text("Note", order.PaymentTerms);
            writer.End();
        }

        foreach (AllowanceCharge allowanceCharge in order.AllowancesAndCharges)
        {
            WriteAllowanceCharge(allowanceCharge, writer, currency, withTaxCategory: true);
        }

        if (order.TaxAmount.IsSet)
        {
            writer.StartCac("TaxTotal");
            writer.Amount("TaxAmount", order.TaxAmount, currency);
            writer.End();
        }

        WriteTotals(order.Totals, writer, currency);

        foreach (OrderLine line in order.Lines)
        {
            WriteLine(line, writer, currency);
        }

        writer.Extensions(order.Extensions);
    }

    private static void WriteReference(string localName, IdentifierField identifier, UblDocument writer)
    {
        if (!identifier.IsSet)
        {
            return;
        }

        writer.StartCac(localName);
        writer.Identifier("ID", identifier);
        writer.End();
    }

    private static void WriteAttachment(AdditionalDocument document, UblDocument writer)
    {
        writer.StartCac("AdditionalDocumentReference");
        writer.Identifier("ID", document.Identifier);
        writer.Text("DocumentType", document.Description);

        if (document.Attachment.IsSet || document.ExternalLocation.IsSet)
        {
            writer.StartCac("Attachment");
            writer.Binary("EmbeddedDocumentBinaryObject", document.Attachment);

            if (document.ExternalLocation.IsSet)
            {
                writer.StartCac("ExternalReference");
                writer.Text("URI", document.ExternalLocation);
                writer.End();
            }

            writer.End();
        }

        writer.Extensions(document.Extensions);
        writer.End();
    }

    internal static void WriteWrappedParty(Party? party, string role, UblDocument writer)
    {
        if (party is null)
        {
            return;
        }

        writer.StartCac(role);
        WriteParty(party, writer);
        writer.End();
    }

    internal static void WriteParty(Party party, UblDocument writer)
    {
        writer.StartCac("Party");
        writer.Identifier("EndpointID", party.ElectronicAddress);

        foreach (IdentifierField identifier in party.Identifiers)
        {
            writer.StartCac("PartyIdentification");
            writer.Identifier("ID", identifier);
            writer.End();
        }

        if (party.TradingName.IsSet)
        {
            writer.StartCac("PartyName");
            writer.Text("Name", party.TradingName);
            writer.End();
        }

        WriteAddress(party.Address, "PostalAddress", writer);

        if (party.VatIdentifier.IsSet || party.TaxRegistrationIdentifier.IsSet)
        {
            WriteTaxScheme(party.VatIdentifier, "VAT", writer);
            WriteTaxScheme(party.TaxRegistrationIdentifier, party.TaxRegistrationScheme.Value, writer);
        }

        if (party.Name.IsSet || party.LegalRegistrationIdentifier.IsSet || party.RegistrationAddress is not null)
        {
            writer.StartCac("PartyLegalEntity");
            writer.Text("RegistrationName", party.Name);
            writer.Identifier("CompanyID", party.LegalRegistrationIdentifier);
            WriteAddress(party.RegistrationAddress, "RegistrationAddress", writer);
            writer.End();
        }

        WriteContact(party.Contact, writer);
        writer.Extensions(party.Extensions);
        writer.End();
    }

    internal static void WriteTaxScheme(IdentifierField identifier, string? scheme, UblDocument writer)
    {
        if (!identifier.IsSet)
        {
            return;
        }

        writer.StartCac("PartyTaxScheme");
        writer.Identifier("CompanyID", identifier);
        writer.StartCac("TaxScheme");
        writer.Cbc("ID", scheme ?? "VAT");
        writer.End();
        writer.End();
    }

    internal static void WriteContact(Contact? contact, UblDocument writer)
    {
        if (contact is null)
        {
            return;
        }

        writer.StartCac("Contact");
        writer.Text("Name", contact.Name);
        writer.Text("Telephone", contact.Telephone);
        writer.Text("ElectronicMail", contact.Email);
        writer.Extensions(contact.Extensions);
        writer.End();
    }

    internal static void WriteAddress(PostalAddress? address, string localName, UblDocument writer)
    {
        if (address is null)
        {
            return;
        }

        writer.StartCac(localName);
        writer.Text("StreetName", address.Line1);
        writer.Text("AdditionalStreetName", address.Line2);
        writer.Text("CityName", address.City);
        writer.Text("PostalZone", address.PostCode);
        writer.Text("CountrySubentity", address.CountrySubdivision);

        if (address.Line3.IsSet)
        {
            writer.StartCac("AddressLine");
            writer.Text("Line", address.Line3);
            writer.End();
        }

        if (address.CountryCode.IsSet)
        {
            writer.StartCac("Country");
            writer.Code("IdentificationCode", address.CountryCode);
            writer.End();
        }

        writer.Extensions(address.Extensions);
        writer.End();
    }

    private static void WritePeriod(string localName, InvoicingPeriod? period, UblDocument writer)
    {
        if (period is null || (!period.StartDate.IsSet && !period.EndDate.IsSet))
        {
            return;
        }

        writer.StartCac(localName);
        writer.Date("StartDate", period.StartDate);
        writer.Date("EndDate", period.EndDate);
        writer.Extensions(period.Extensions);
        writer.End();
    }

    internal static void WriteDelivery(OrderDelivery? delivery, UblDocument writer)
    {
        if (delivery is null)
        {
            return;
        }

        writer.StartCac("Delivery");
        writer.Identifier("ID", delivery.Identifier);

        if (delivery.LocationIdentifier.IsSet || delivery.LocationName.IsSet || delivery.Address is not null)
        {
            writer.StartCac("DeliveryLocation");
            writer.Identifier("ID", delivery.LocationIdentifier);
            writer.Text("Name", delivery.LocationName);
            WriteAddress(delivery.Address, "Address", writer);
            writer.End();
        }

        if (delivery.RequestedFrom.IsSet || delivery.RequestedUntil.IsSet)
        {
            writer.StartCac("RequestedDeliveryPeriod");
            writer.Moment("StartDate", "StartTime", delivery.RequestedFrom);
            writer.Moment("EndDate", "EndTime", delivery.RequestedUntil);
            writer.End();
        }

        if (delivery.Recipient is { } recipient)
        {
            writer.StartCac("DeliveryParty");
            WriteInnerParty(recipient, writer);
            writer.End();
        }

        if (delivery.PromisedFrom.IsSet || delivery.PromisedUntil.IsSet)
        {
            writer.StartCac("PromisedDeliveryPeriod");
            writer.Moment("StartDate", "StartTime", delivery.PromisedFrom);
            writer.Moment("EndDate", "EndTime", delivery.PromisedUntil);
            writer.End();
        }

        if (delivery.RequestedDespatchAt.IsSet)
        {
            writer.StartCac("Despatch");
            writer.Moment("RequestedDespatchDate", "RequestedDespatchTime", delivery.RequestedDespatchAt);
            writer.End();
        }

        if (delivery.ShipmentIdentifier.IsSet || delivery.ShippingPriorityCode.IsSet)
        {
            writer.StartCac("Shipment");
            writer.Identifier("ID", delivery.ShipmentIdentifier);
            writer.Code("ShippingPriorityLevelCode", delivery.ShippingPriorityCode);
            writer.End();
        }

        writer.Extensions(delivery.Extensions);
        writer.End();
    }

    /// <summary>A delivery party is a party without the <c>cac:Party</c> wrapper the roles use.</summary>
    internal static void WriteInnerParty(Party party, UblDocument writer)
    {
        foreach (IdentifierField identifier in party.Identifiers)
        {
            writer.StartCac("PartyIdentification");
            writer.Identifier("ID", identifier);
            writer.End();
        }

        if (party.TradingName.IsSet)
        {
            writer.StartCac("PartyName");
            writer.Text("Name", party.TradingName);
            writer.End();
        }

        WriteAddress(party.Address, "PostalAddress", writer);
        WriteContact(party.Contact, writer);
        writer.Extensions(party.Extensions);
    }

    private static void WriteDeliveryTerms(Order order, UblDocument writer)
    {
        if (!order.DeliveryTermsCode.IsSet && !order.DeliveryTerms.IsSet && !order.DeliveryTermsLocation.IsSet)
        {
            return;
        }

        writer.StartCac("DeliveryTerms");
        writer.Identifier("ID", order.DeliveryTermsCode);
        writer.Text("SpecialTerms", order.DeliveryTerms);

        if (order.DeliveryTermsLocation.IsSet)
        {
            writer.StartCac("DeliveryLocation");
            writer.Identifier("ID", order.DeliveryTermsLocation);
            writer.End();
        }

        writer.End();
    }

    private static void WriteAllowanceCharge(
        AllowanceCharge allowanceCharge,
        UblDocument writer,
        string? currency,
        bool withTaxCategory)
    {
        writer.StartCac("AllowanceCharge");
        writer.Cbc("ChargeIndicator", allowanceCharge.IsCharge ? "true" : "false");
        writer.Code("AllowanceChargeReasonCode", allowanceCharge.ReasonCode);
        writer.Text("AllowanceChargeReason", allowanceCharge.Reason);
        writer.Decimal("MultiplierFactorNumeric", allowanceCharge.Percentage);
        writer.Amount("Amount", allowanceCharge.Amount, currency);
        writer.Amount("BaseAmount", allowanceCharge.BaseAmount, currency);

        if (withTaxCategory && allowanceCharge.VatCategoryCode.IsSet)
        {
            writer.StartCac("TaxCategory");
            writer.Code("ID", allowanceCharge.VatCategoryCode);
            writer.Decimal("Percent", allowanceCharge.VatRate);
            writer.StartCac("TaxScheme");
            writer.Cbc("ID", "VAT");
            writer.End();
            writer.End();
        }

        writer.Extensions(allowanceCharge.Extensions);
        writer.End();
    }

    private static void WriteTotals(DocumentTotals totals, UblDocument writer, string? currency)
    {
        writer.StartCac("AnticipatedMonetaryTotal");
        writer.Amount("LineExtensionAmount", totals.LineTotalAmount, currency);
        writer.Amount("TaxExclusiveAmount", totals.TaxExclusiveAmount, currency);
        writer.Amount("TaxInclusiveAmount", totals.TaxInclusiveAmount, currency);
        writer.Amount("AllowanceTotalAmount", totals.AllowanceTotalAmount, currency);
        writer.Amount("ChargeTotalAmount", totals.ChargeTotalAmount, currency);
        writer.Amount("PrepaidAmount", totals.PrepaidAmount, currency);
        writer.Amount("PayableRoundingAmount", totals.RoundingAmount, currency);
        writer.Amount("PayableAmount", totals.DuePayableAmount, currency);
        writer.Extensions(totals.Extensions);
        writer.End();
    }

    private static void WriteLine(OrderLine line, UblDocument writer, string? currency)
    {
        writer.StartCac("OrderLine");
        writer.Text("Note", line.Note);

        writer.StartCac("LineItem");
        writer.Identifier("ID", line.Identifier);
        writer.Quantity("Quantity", line.Quantity);
        writer.Amount("LineExtensionAmount", line.NetAmount, currency);
        writer.Indicator("PartialDeliveryIndicator", line.PartialDeliveryAccepted);
        writer.Text("AccountingCost", line.AccountingReference);

        WriteDelivery(line.Delivery, writer);

        if (line.Originator is { } originator)
        {
            writer.StartCac("OriginatorParty");
            WriteInnerParty(originator, writer);
            writer.End();
        }

        foreach (AllowanceCharge allowanceCharge in line.AllowancesAndCharges)
        {
            WriteAllowanceCharge(allowanceCharge, writer, currency, withTaxCategory: false);
        }

        WritePrice(line.Price, writer, currency);
        WriteItem(line.Item, writer);

        writer.End();

        writer.Extensions(line.Extensions);
        writer.End();
    }

    internal static void WritePrice(LinePrice? price, UblDocument writer, string? currency)
    {
        if (price is null)
        {
            return;
        }

        writer.StartCac("Price");
        writer.Amount("PriceAmount", price.NetPrice, currency);
        writer.Quantity("BaseQuantity", price.BaseQuantity);

        // UBL states the discount as an allowance on the price rather than as an amount of its own.
        if (price.Discount.IsSet || price.GrossPrice.IsSet)
        {
            writer.StartCac("AllowanceCharge");
            writer.Cbc("ChargeIndicator", "false");
            writer.Amount("Amount", price.Discount, currency);
            writer.Amount("BaseAmount", price.GrossPrice, currency);
            writer.End();
        }

        writer.Extensions(price.Extensions);
        writer.End();
    }

    internal static void WriteItem(OrderItem? item, UblDocument writer)
    {
        if (item is null)
        {
            return;
        }

        writer.StartCac("Item");
        writer.Text("Description", item.Description);
        writer.Text("Name", item.Name);

        WriteItemIdentifier("BuyersItemIdentification", item.BuyerIdentifier, writer);
        WriteItemIdentifier("SellersItemIdentification", item.SellerIdentifier, writer);
        WriteItemIdentifier("ManufacturersItemIdentification", item.ManufacturerIdentifier, writer);
        WriteItemIdentifier("StandardItemIdentification", item.StandardIdentifier, writer);
        WriteItemIdentifier("ItemSpecificationDocumentReference", item.SpecificationReference, writer);

        foreach (CodeField classification in item.ClassificationCodes)
        {
            writer.StartCac("CommodityClassification");
            writer.Code("ItemClassificationCode", classification);
            writer.End();
        }

        if (item.VatCategoryCode.IsSet)
        {
            writer.StartCac("ClassifiedTaxCategory");
            writer.Code("ID", item.VatCategoryCode);
            writer.Decimal("Percent", item.VatRate);
            writer.StartCac("TaxScheme");
            writer.Cbc("ID", "VAT");
            writer.End();
            writer.End();
        }

        foreach (OrderItemProperty property in item.Characteristics)
        {
            writer.StartCac("AdditionalItemProperty");
            writer.Identifier("ID", property.Identifier);
            writer.Text("Name", property.Name);
            writer.Text("Value", property.Value);
            writer.Quantity("ValueQuantity", property.ValueQuantity);
            writer.Text("ValueQualifier", property.ValueQualifier);
            writer.Extensions(property.Extensions);
            writer.End();
        }

        foreach (ItemInstance instance in item.Instances)
        {
            writer.StartCac("ItemInstance");
            writer.Identifier("SerialID", instance.SerialIdentifier);

            if (instance.LotIdentifier.IsSet)
            {
                writer.StartCac("LotIdentification");
                writer.Identifier("LotNumberID", instance.LotIdentifier);
                writer.End();
            }

            writer.Extensions(instance.Extensions);
            writer.End();
        }

        writer.Extensions(item.Extensions);
        writer.End();
    }

    internal static void WriteItemIdentifier(string localName, IdentifierField field, UblDocument writer)
    {
        if (!field.IsSet)
        {
            return;
        }

        writer.StartCac(localName);
        writer.Identifier("ID", field);
        writer.End();
    }
}
