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

        // An order change is the same document under another root, the way a credit note is to an invoice.
        UblOrderShape shape = UblOrderShape.For(document);

        using var writer = UblDocument.Open(
            destination,
            shape.Root.LocalName,
            shape.Root.NamespaceName);

        Write(document, writer, shape);
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

    private static void Write(Order order, UblDocument writer, UblOrderShape shape)
    {
        writer.Node(order.Extensions);
        string? currency = order.CurrencyCode.Value ?? order.CurrencyCode.Raw;

        if (order.SpecificationIdentifier.IsDeclared)
        {
            writer.Cbc("CustomizationID", order.SpecificationIdentifier.Value);
        }

        writer.Identifier("ProfileID", order.BusinessProcessType);
        writer.Identifier("ID", order.Number);
        writer.Identifier("SalesOrderID", order.SalesOrderNumber);
        writer.Moment("IssueDate", "IssueTime", order.IssuedAt);

        if (shape.IsChange)
        {
            writer.Identifier("SequenceNumberID", order.SequenceNumber);
        }
        else
        {
            writer.Code("OrderTypeCode", order.TypeCode);
        }

        writer.Notes(order.Notes);
        writer.Code("DocumentCurrencyCode", order.CurrencyCode);
        writer.Text("CustomerReference", order.BuyerReference);
        writer.Text("AccountingCost", order.AccountingReference);

        WritePeriod("ValidityPeriod", order.ValidityPeriod, writer);

        // An order change names the order it amends where an order names an earlier one it relates to, and
        // the schema puts them in different places.
        if (shape.IsChange)
        {
            WriteReference("OrderReference", order.OrderReference, writer);
            WriteReference("QuotationDocumentReference", order.QuotationReference, writer);
        }
        else
        {
            WriteReference("QuotationDocumentReference", order.QuotationReference, writer);
            WriteReference("OrderDocumentReference", order.OrderReference, writer);
        }
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

        WriteTotals("AnticipatedMonetaryTotal", order.Totals, writer, currency);

        foreach (OrderLine line in order.Lines)
        {
            WriteLine(line, writer, currency);
        }

    }

    internal static void WriteReference(string localName, IdentifierField identifier, UblDocument writer)
    {
        if (!identifier.IsSet)
        {
            return;
        }

        writer.StartCac(localName);
        writer.Identifier("ID", identifier);
        writer.End();
    }

    internal static void WriteAttachment(
        AdditionalDocument document,
        UblDocument writer,
        string localName = "AdditionalDocumentReference",
        bool withTypeCode = false)
    {
        writer.StartCac(localName, document.Extensions);
        writer.Identifier("ID", document.Identifier);

        if (withTypeCode)
        {
            writer.Code("DocumentTypeCode", document.TypeCode);
        }

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

    internal static void WriteParty(Party party, UblDocument writer, bool contactBesideParty = false)
    {
        writer.StartCac("Party", party.Extensions);
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

        // The buyer's role element carries the contact beside the party rather than inside it, so the
        // caller writing that role asks for it to be left out here and writes it itself.
        if (!contactBesideParty)
        {
            WriteContact(party.Contact, writer);
        }

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

        writer.StartCac("Contact", contact.Extensions);
        writer.Text("Name", contact.Name);
        writer.Text("Telephone", contact.Telephone);
        writer.Text("ElectronicMail", contact.Email);
        writer.End();
    }

    internal static void WriteAddress(PostalAddress? address, string localName, UblDocument writer)
    {
        if (address is null)
        {
            return;
        }

        writer.StartCac(localName, address.Extensions);
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

        writer.End();
    }

    private static void WritePeriod(string localName, InvoicingPeriod? period, UblDocument writer)
    {
        if (period is null || (!period.StartDate.IsSet && !period.EndDate.IsSet))
        {
            return;
        }

        writer.StartCac(localName, period.Extensions);
        writer.Date("StartDate", period.StartDate);
        writer.Date("EndDate", period.EndDate);
        writer.End();
    }

    internal static void WriteDelivery(OrderDelivery? delivery, UblDocument writer)
    {
        if (delivery is null)
        {
            return;
        }

        writer.StartCac("Delivery", delivery.Extensions);
        writer.Identifier("ID", delivery.Identifier);
        writer.Quantity("Quantity", delivery.Quantity);

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
            writer.StartCac("DeliveryParty", recipient.Extensions);
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

    internal static void WriteAllowanceCharge(
        AllowanceCharge allowanceCharge,
        UblDocument writer,
        string? currency,
        bool withTaxCategory)
    {
        writer.StartCac("AllowanceCharge", allowanceCharge.Extensions);
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

        writer.End();
    }

    internal static void WriteTotals(string localName, DocumentTotals totals, UblDocument writer, string? currency)
    {
        writer.StartCac(localName, totals.Extensions);
        writer.Amount("LineExtensionAmount", totals.LineTotalAmount, currency);
        writer.Amount("TaxExclusiveAmount", totals.TaxExclusiveAmount, currency);
        writer.Amount("TaxInclusiveAmount", totals.TaxInclusiveAmount, currency);
        writer.Amount("AllowanceTotalAmount", totals.AllowanceTotalAmount, currency);
        writer.Amount("ChargeTotalAmount", totals.ChargeTotalAmount, currency);
        writer.Amount("PrepaidAmount", totals.PrepaidAmount, currency);
        writer.Amount("PayableRoundingAmount", totals.RoundingAmount, currency);
        writer.Amount("PayableAmount", totals.DuePayableAmount, currency);
        writer.End();
    }

    private static void WriteLine(OrderLine line, UblDocument writer, string? currency)
    {
        writer.StartCac("OrderLine", line.Extensions);
        writer.Notes(line.Notes);

        writer.StartCac("LineItem");
        writer.Identifier("ID", line.Identifier);
        writer.Code("LineStatusCode", line.StatusCode);
        writer.Quantity("Quantity", line.Quantity);
        writer.Amount("LineExtensionAmount", line.NetAmount, currency);
        writer.Indicator("PartialDeliveryIndicator", line.PartialDeliveryAccepted);
        writer.Text("AccountingCost", line.AccountingReference);

        WriteDelivery(line.Delivery, writer);

        if (line.Originator is { } originator)
        {
            writer.StartCac("OriginatorParty", originator.Extensions);
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

        writer.End();
    }

    internal static void WritePrice(LinePrice? price, UblDocument writer, string? currency)
    {
        if (price is null)
        {
            return;
        }

        writer.StartCac("Price", price.Extensions);
        writer.Amount("PriceAmount", price.NetPrice, currency);
        writer.Quantity("BaseQuantity", price.BaseQuantity);
        writer.Code("PriceType", price.PriceTypeCode);

        // UBL states the discount as an allowance on the price rather than as an amount of its own.
        if (price.Discount.IsSet || price.GrossPrice.IsSet)
        {
            writer.StartCac("AllowanceCharge");
            writer.Cbc("ChargeIndicator", "false");
            writer.Amount("Amount", price.Discount, currency);
            writer.Amount("BaseAmount", price.GrossPrice, currency);
            writer.End();
        }

        writer.End();
    }

    internal static void WriteItem(OrderItem? item, UblDocument writer)
    {
        if (item is null)
        {
            return;
        }

        writer.StartCac("Item", item.Extensions);
        writer.Text("Description", item.Description);
        writer.Text("Name", item.Name);

        WriteItemIdentifier("BuyersItemIdentification", item.BuyerIdentifier, writer);
        WriteItemIdentifier("SellersItemIdentification", item.SellerIdentifier, writer);
        WriteItemIdentifier("ManufacturersItemIdentification", item.ManufacturerIdentifier, writer);
        WriteItemIdentifier("StandardItemIdentification", item.StandardIdentifier, writer);
        if (item.SpecificationDocument is { } specification)
        {
            WriteAttachment(specification, writer, "ItemSpecificationDocumentReference", withTypeCode: true);
        }
        else
        {
            WriteItemIdentifier("ItemSpecificationDocumentReference", item.SpecificationReference, writer);
        }

        foreach (CodeField classification in item.ClassificationCodes)
        {
            writer.StartCac("CommodityClassification");
            writer.Code("ItemClassificationCode", classification);
            writer.End();
        }

        if (item.TransactionActionCode.IsSet)
        {
            writer.StartCac("TransactionConditions");
            writer.Code("ActionCode", item.TransactionActionCode);
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
            writer.StartCac("AdditionalItemProperty", property.Extensions);
            writer.Identifier("ID", property.Identifier);
            writer.Text("Name", property.Name);
            writer.Code("NameCode", property.NameCode);
            writer.Text("Value", property.Value);
            writer.Quantity("ValueQuantity", property.ValueQuantity);
            writer.Text("ValueQualifier", property.ValueQualifier);
            writer.End();
        }

        foreach (OrderItemCertificate certificate in item.Certificates)
        {
            writer.StartCac("Certificate", certificate.Extensions);
            writer.Identifier("ID", certificate.Identifier);
            writer.Code("CertificateTypeCode", certificate.TypeCode);
            writer.Text("CertificateType", certificate.Type);
            writer.Text("Remarks", certificate.Remarks);

            if (certificate.Issuer is { } issuer)
            {
                writer.StartCac("IssuerParty", issuer.Extensions);
                WriteInnerParty(issuer, writer);
                writer.End();
            }

            if (certificate.DocumentReference.IsSet)
            {
                writer.StartCac("DocumentReference");
                writer.Identifier("ID", certificate.DocumentReference);
                writer.End();
            }

            writer.End();
        }

        foreach (ItemInstance instance in item.Instances)
        {
            writer.StartCac("ItemInstance", instance.Extensions);
            writer.Identifier("SerialID", instance.SerialIdentifier);

            if (instance.LotIdentifier.IsSet)
            {
                writer.StartCac("LotIdentification");
                writer.Identifier("LotNumberID", instance.LotIdentifier);
                writer.End();
            }

            writer.End();
        }

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
