using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Xml;
using International.EInvoicing.Model;
using International.EInvoicing.Values;

using International.EInvoicing.Xml;

namespace International.EInvoicing.Ubl.Writing;

/// <summary>
/// Writes a canonical invoice as UBL 2.1.
/// </summary>
/// <remarks>
/// <para>
/// Element order is normative in UBL: the schema declares sequences, and a document whose elements are right
/// but out of order is rejected by the receiver's schema validation. The order below follows
/// <c>UBL-Invoice-2.1.xsd</c> and is the reason this writer is explicit rather than generated.
/// </para>
/// <para>
/// A field that was read from a document and not modified is written back from its raw text, so a document
/// that passes through unchanged comes out equivalent to the one that went in.
/// </para>
/// </remarks>
[SuppressMessage(
    "Performance",
    "CA1822:Mark members as static",
    Justification = "An instance API so a caller can replace this writer through the registry.")]
public sealed class UblInvoiceWriter
{
    /// <summary>Writes <paramref name="invoice"/> to <paramref name="stream"/>. The stream is left open.</summary>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    public void Write(EInvoice invoice, Stream stream)
    {
        ArgumentNullException.ThrowIfNull(invoice);
        ArgumentNullException.ThrowIfNull(stream);

        var settings = new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "  ",
            Encoding = new System.Text.UTF8Encoding(false),
            CloseOutput = false,
        };

        using XmlWriter writer = XmlWriter.Create(stream, settings);
        Write(invoice, writer);
    }

    /// <summary>Writes <paramref name="invoice"/> and returns it as XML text.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="invoice"/> is <c>null</c>.</exception>
    public string WriteToString(EInvoice invoice)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        using var stream = new MemoryStream();
        Write(invoice, stream);
        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void Write(EInvoice invoice, XmlWriter writer)
    {
        writer.WriteStartDocument();
        writer.WriteStartElement("Invoice", UblNames.Invoice.NamespaceName);
        writer.WriteAttributeString("xmlns", UblNames.CacPrefix, null, UblNames.Cac.NamespaceName);
        writer.WriteAttributeString("xmlns", UblNames.CbcPrefix, null, UblNames.Cbc.NamespaceName);

        WriteDocumentLevel(invoice, writer);
        WriteReferences(invoice, writer);
        WriteParties(invoice, writer);
        WriteDeliveryAndPayment(invoice, writer);

        foreach (AllowanceCharge allowanceCharge in invoice.AllowancesAndCharges)
        {
            WriteAllowanceCharge(allowanceCharge, writer);
        }

        WriteTaxTotal(invoice, writer);
        WriteTotals(invoice.Totals, writer);

        foreach (InvoiceLine line in invoice.Lines)
        {
            WriteLine(line, writer);
        }

        WriteExtensions(invoice.Extensions, writer);

        writer.WriteEndElement();
        writer.WriteEndDocument();
    }

    private static void WriteDocumentLevel(EInvoice invoice, XmlWriter writer)
    {
        if (invoice.SpecificationIdentifier.IsDeclared)
        {
            Cbc(writer, "CustomizationID", invoice.SpecificationIdentifier.Value);
        }

        WriteIdentifier(writer, "ProfileID", invoice.BusinessProcessType);
        WriteIdentifier(writer, "ID", invoice.Number);
        WriteDate(writer, "IssueDate", invoice.IssueDate);
        WriteDate(writer, "DueDate", invoice.DueDate);
        WriteCode(writer, "InvoiceTypeCode", invoice.TypeCode);

        foreach (InvoiceNote note in invoice.Notes)
        {
            WriteText(writer, "Note", note.Text);
        }

        WriteDate(writer, "TaxPointDate", invoice.TaxPointDate);
        WriteCode(writer, "DocumentCurrencyCode", invoice.CurrencyCode);
        WriteCode(writer, "TaxCurrencyCode", invoice.TaxAccountingCurrencyCode);
        WriteText(writer, "AccountingCost", invoice.BuyerAccountingReference);
        WriteText(writer, "BuyerReference", invoice.BuyerReference);
    }

    private static void WriteReferences(EInvoice invoice, XmlWriter writer)
    {
        WritePeriod(writer, "InvoicePeriod", invoice.Period);

        if (invoice.PurchaseOrderReference.IsSet || invoice.SalesOrderReference.IsSet)
        {
            StartCac(writer, "OrderReference");
            WriteIdentifier(writer, "ID", invoice.PurchaseOrderReference);
            WriteIdentifier(writer, "SalesOrderID", invoice.SalesOrderReference);
            writer.WriteEndElement();
        }

        foreach (DocumentReference preceding in invoice.PrecedingInvoices)
        {
            StartCac(writer, "BillingReference");
            StartCac(writer, "InvoiceDocumentReference");
            WriteIdentifier(writer, "ID", preceding.Identifier);
            WriteDate(writer, "IssueDate", preceding.IssueDate);
            writer.WriteEndElement();
            writer.WriteEndElement();
        }

        if (invoice.ContractReference.IsSet)
        {
            StartCac(writer, "ContractDocumentReference");
            WriteIdentifier(writer, "ID", invoice.ContractReference);
            writer.WriteEndElement();
        }

        foreach (AdditionalDocument document in invoice.AdditionalDocuments)
        {
            WriteAdditionalDocument(document, writer);
        }

        if (invoice.ProjectReference.IsSet)
        {
            StartCac(writer, "ProjectReference");
            WriteIdentifier(writer, "ID", invoice.ProjectReference);
            writer.WriteEndElement();
        }
    }

    private static void WriteAdditionalDocument(AdditionalDocument document, XmlWriter writer)
    {
        StartCac(writer, "AdditionalDocumentReference");
        WriteIdentifier(writer, "ID", document.Identifier);
        WriteText(writer, "DocumentDescription", document.Description);

        if (document.Attachment.IsSet || document.ExternalLocation.IsSet)
        {
            StartCac(writer, "Attachment");

            if (document.Attachment.Value is { } bytes)
            {
                writer.WriteStartElement(UblNames.CbcPrefix, "EmbeddedDocumentBinaryObject", UblNames.Cbc.NamespaceName);
                WriteAttributeIfSet(writer, "mimeCode", document.Attachment.MimeCode);
                WriteAttributeIfSet(writer, "filename", document.Attachment.Filename);
                writer.WriteString(XmlCharacters.Sanitize(document.Attachment.Raw ?? Convert.ToBase64String(bytes)));
                writer.WriteEndElement();
            }

            if (document.ExternalLocation.IsSet)
            {
                StartCac(writer, "ExternalReference");
                WriteText(writer, "URI", document.ExternalLocation);
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        WriteExtensions(document.Extensions, writer);
        writer.WriteEndElement();
    }

    private static void WriteParties(EInvoice invoice, XmlWriter writer)
    {
        WriteParty(writer, "AccountingSupplierParty", invoice.Seller, wrapped: true);
        WriteParty(writer, "AccountingCustomerParty", invoice.Buyer, wrapped: true);
        WriteParty(writer, "PayeeParty", invoice.Payee, wrapped: false);
        WriteParty(writer, "TaxRepresentativeParty", invoice.SellerTaxRepresentative, wrapped: false);
    }

    private static void WriteParty(XmlWriter writer, string elementName, Party? party, bool wrapped)
    {
        if (party is null)
        {
            return;
        }

        StartCac(writer, elementName);

        if (wrapped)
        {
            StartCac(writer, "Party");
        }

        WriteIdentifier(writer, "EndpointID", party.ElectronicAddress);

        foreach (IdentifierField identifier in party.Identifiers.Where(i => i.IsSet))
        {
            StartCac(writer, "PartyIdentification");
            WriteIdentifier(writer, "ID", identifier);
            writer.WriteEndElement();
        }

        if (party.Name.IsSet)
        {
            StartCac(writer, "PartyName");
            WriteText(writer, "Name", party.Name);
            writer.WriteEndElement();
        }

        WriteAddress(writer, "PostalAddress", party.Address);
        WriteTaxScheme(writer, party);
        WriteLegalEntity(writer, party);
        WriteContact(writer, party.Contact);

        if (wrapped)
        {
            writer.WriteEndElement();
        }

        writer.WriteEndElement();
    }

    private static void WriteTaxScheme(XmlWriter writer, Party party)
    {
        if (party.VatIdentifier.IsSet)
        {
            StartCac(writer, "PartyTaxScheme");
            WriteIdentifier(writer, "CompanyID", party.VatIdentifier);
            StartCac(writer, "TaxScheme");
            Cbc(writer, "ID", "VAT");
            writer.WriteEndElement();
            writer.WriteEndElement();
        }

        if (!party.TaxRegistrationIdentifier.IsSet)
        {
            return;
        }

        StartCac(writer, "PartyTaxScheme");
        WriteIdentifier(writer, "CompanyID", party.TaxRegistrationIdentifier);
        StartCac(writer, "TaxScheme");
        Cbc(writer, "ID", "FC");
        writer.WriteEndElement();
        writer.WriteEndElement();
    }

    private static void WriteLegalEntity(XmlWriter writer, Party party)
    {
        if (!party.Name.IsSet && !party.LegalRegistrationIdentifier.IsSet)
        {
            return;
        }

        StartCac(writer, "PartyLegalEntity");
        WriteText(writer, "RegistrationName", party.Name);
        WriteIdentifier(writer, "CompanyID", party.LegalRegistrationIdentifier);
        WriteText(writer, "CompanyLegalForm", party.AdditionalLegalInformation);
        writer.WriteEndElement();
    }

    private static void WriteContact(XmlWriter writer, Contact? contact)
    {
        if (contact is null)
        {
            return;
        }

        StartCac(writer, "Contact");
        WriteText(writer, "Name", contact.Name);
        WriteText(writer, "Telephone", contact.Telephone);
        WriteText(writer, "ElectronicMail", contact.Email);
        writer.WriteEndElement();
    }

    private static void WriteAddress(XmlWriter writer, string elementName, PostalAddress? address)
    {
        if (address is null)
        {
            return;
        }

        StartCac(writer, elementName);
        WriteText(writer, "StreetName", address.Line1);
        WriteText(writer, "AdditionalStreetName", address.Line2);
        WriteText(writer, "CityName", address.City);
        WriteText(writer, "PostalZone", address.PostCode);
        WriteText(writer, "CountrySubentity", address.CountrySubdivision);

        if (address.Line3.IsSet)
        {
            StartCac(writer, "AddressLine");
            WriteText(writer, "Line", address.Line3);
            writer.WriteEndElement();
        }

        if (address.CountryCode.IsSet)
        {
            StartCac(writer, "Country");
            WriteCode(writer, "IdentificationCode", address.CountryCode);
            writer.WriteEndElement();
        }

        writer.WriteEndElement();
    }

    private static void WriteDeliveryAndPayment(EInvoice invoice, XmlWriter writer)
    {
        if (invoice.Delivery is { } delivery)
        {
            StartCac(writer, "Delivery");
            WriteDate(writer, "ActualDeliveryDate", delivery.ActualDeliveryDate);

            if (delivery.LocationIdentifier.IsSet || delivery.Address is not null)
            {
                StartCac(writer, "DeliveryLocation");
                WriteIdentifier(writer, "ID", delivery.LocationIdentifier);
                WriteAddress(writer, "Address", delivery.Address);
                writer.WriteEndElement();
            }

            if (delivery.Name.IsSet)
            {
                StartCac(writer, "DeliveryParty");
                StartCac(writer, "PartyName");
                WriteText(writer, "Name", delivery.Name);
                writer.WriteEndElement();
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        if (invoice.Payment is not { } payment)
        {
            return;
        }

        StartCac(writer, "PaymentMeans");
        WriteCode(writer, "PaymentMeansCode", payment.MeansTypeCode);
        WriteText(writer, "PaymentID", payment.RemittanceInformation);

        foreach (CreditTransfer transfer in payment.CreditTransfers)
        {
            StartCac(writer, "PayeeFinancialAccount");
            WriteIdentifier(writer, "ID", transfer.AccountIdentifier);
            WriteText(writer, "Name", transfer.AccountName);

            if (transfer.ServiceProviderIdentifier.IsSet)
            {
                StartCac(writer, "FinancialInstitutionBranch");
                WriteIdentifier(writer, "ID", transfer.ServiceProviderIdentifier);
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        writer.WriteEndElement();

        if (invoice.PaymentTerms.IsSet)
        {
            StartCac(writer, "PaymentTerms");
            WriteText(writer, "Note", invoice.PaymentTerms);
            writer.WriteEndElement();
        }
    }

    private static void WriteAllowanceCharge(AllowanceCharge allowanceCharge, XmlWriter writer)
    {
        StartCac(writer, "AllowanceCharge");
        Cbc(writer, "ChargeIndicator", allowanceCharge.IsCharge ? "true" : "false");
        WriteCode(writer, "AllowanceChargeReasonCode", allowanceCharge.ReasonCode);
        WriteText(writer, "AllowanceChargeReason", allowanceCharge.Reason);
        WriteDecimal(writer, "MultiplierFactorNumeric", allowanceCharge.Percentage);
        WriteAmount(writer, "Amount", allowanceCharge.Amount);
        WriteAmount(writer, "BaseAmount", allowanceCharge.BaseAmount);

        if (allowanceCharge.VatCategoryCode.IsSet)
        {
            StartCac(writer, "TaxCategory");
            WriteCode(writer, "ID", allowanceCharge.VatCategoryCode);
            WriteDecimal(writer, "Percent", allowanceCharge.VatRate);
            StartCac(writer, "TaxScheme");
            Cbc(writer, "ID", "VAT");
            writer.WriteEndElement();
            writer.WriteEndElement();
        }

        writer.WriteEndElement();
    }

    private static void WriteTaxTotal(EInvoice invoice, XmlWriter writer)
    {
        if (!invoice.Totals.TaxAmount.IsSet && invoice.VatBreakdown.Count == 0)
        {
            return;
        }

        StartCac(writer, "TaxTotal");
        WriteAmount(writer, "TaxAmount", invoice.Totals.TaxAmount);

        foreach (VatBreakdownEntry entry in invoice.VatBreakdown)
        {
            StartCac(writer, "TaxSubtotal");
            WriteAmount(writer, "TaxableAmount", entry.TaxableAmount);
            WriteAmount(writer, "TaxAmount", entry.TaxAmount);
            StartCac(writer, "TaxCategory");
            WriteCode(writer, "ID", entry.CategoryCode);
            WriteDecimal(writer, "Percent", entry.Rate);
            WriteCode(writer, "TaxExemptionReasonCode", entry.ExemptionReasonCode);
            WriteText(writer, "TaxExemptionReason", entry.ExemptionReason);
            StartCac(writer, "TaxScheme");
            Cbc(writer, "ID", "VAT");
            writer.WriteEndElement();
            writer.WriteEndElement();
            writer.WriteEndElement();
        }

        writer.WriteEndElement();
    }

    private static void WriteTotals(DocumentTotals totals, XmlWriter writer)
    {
        StartCac(writer, "LegalMonetaryTotal");
        WriteAmount(writer, "LineExtensionAmount", totals.LineTotalAmount);
        WriteAmount(writer, "TaxExclusiveAmount", totals.TaxExclusiveAmount);
        WriteAmount(writer, "TaxInclusiveAmount", totals.TaxInclusiveAmount);
        WriteAmount(writer, "AllowanceTotalAmount", totals.AllowanceTotalAmount);
        WriteAmount(writer, "ChargeTotalAmount", totals.ChargeTotalAmount);
        WriteAmount(writer, "PrepaidAmount", totals.PrepaidAmount);
        WriteAmount(writer, "PayableRoundingAmount", totals.RoundingAmount);
        WriteAmount(writer, "PayableAmount", totals.DuePayableAmount);
        writer.WriteEndElement();
    }

    private static void WriteLine(InvoiceLine line, XmlWriter writer)
    {
        StartCac(writer, "InvoiceLine");
        WriteIdentifier(writer, "ID", line.Identifier);
        WriteText(writer, "Note", line.Note);
        WriteQuantity(writer, "InvoicedQuantity", line.Quantity);
        WriteAmount(writer, "LineExtensionAmount", line.NetAmount);
        WriteText(writer, "AccountingCost", line.BuyerAccountingReference);
        WritePeriod(writer, "InvoicePeriod", line.Period);

        if (line.OrderLineReference.IsSet)
        {
            StartCac(writer, "OrderLineReference");
            WriteIdentifier(writer, "LineID", line.OrderLineReference);
            writer.WriteEndElement();
        }

        foreach (AllowanceCharge allowanceCharge in line.AllowancesAndCharges)
        {
            WriteAllowanceCharge(allowanceCharge, writer);
        }

        WriteItem(line, writer);
        WritePrice(line, writer);

        WriteExtensions(line.Extensions, writer);
        writer.WriteEndElement();
    }

    private static void WriteItem(InvoiceLine line, XmlWriter writer)
    {
        if (line.Item is not { } item)
        {
            return;
        }

        StartCac(writer, "Item");
        WriteText(writer, "Description", item.Description);
        WriteText(writer, "Name", item.Name);

        if (item.BuyerIdentifier.IsSet)
        {
            StartCac(writer, "BuyersItemIdentification");
            WriteIdentifier(writer, "ID", item.BuyerIdentifier);
            writer.WriteEndElement();
        }

        if (item.SellerIdentifier.IsSet)
        {
            StartCac(writer, "SellersItemIdentification");
            WriteIdentifier(writer, "ID", item.SellerIdentifier);
            writer.WriteEndElement();
        }

        if (item.StandardIdentifier.IsSet)
        {
            StartCac(writer, "StandardItemIdentification");
            WriteIdentifier(writer, "ID", item.StandardIdentifier);
            writer.WriteEndElement();
        }

        if (item.OriginCountryCode.IsSet)
        {
            StartCac(writer, "OriginCountry");
            WriteCode(writer, "IdentificationCode", item.OriginCountryCode);
            writer.WriteEndElement();
        }

        foreach (CodeField classification in item.ClassificationCodes.Where(c => c.IsSet))
        {
            StartCac(writer, "CommodityClassification");
            WriteCode(writer, "ItemClassificationCode", classification);
            writer.WriteEndElement();
        }

        if (line.VatCategoryCode.IsSet)
        {
            StartCac(writer, "ClassifiedTaxCategory");
            WriteCode(writer, "ID", line.VatCategoryCode);
            WriteDecimal(writer, "Percent", line.VatRate);
            StartCac(writer, "TaxScheme");
            Cbc(writer, "ID", "VAT");
            writer.WriteEndElement();
            writer.WriteEndElement();
        }

        foreach (ItemCharacteristic characteristic in item.Characteristics)
        {
            StartCac(writer, "AdditionalItemProperty");
            WriteText(writer, "Name", characteristic.Name);
            WriteText(writer, "Value", characteristic.Value);
            writer.WriteEndElement();
        }

        WriteExtensions(item.Extensions, writer);
        writer.WriteEndElement();
    }

    private static void WritePrice(InvoiceLine line, XmlWriter writer)
    {
        if (line.Price is not { } price)
        {
            return;
        }

        StartCac(writer, "Price");
        WriteAmount(writer, "PriceAmount", price.NetPrice);
        WriteQuantity(writer, "BaseQuantity", price.BaseQuantity);

        if (price.Discount.IsSet || price.GrossPrice.IsSet)
        {
            StartCac(writer, "AllowanceCharge");
            Cbc(writer, "ChargeIndicator", "false");
            WriteAmount(writer, "Amount", price.Discount);
            WriteAmount(writer, "BaseAmount", price.GrossPrice);
            writer.WriteEndElement();
        }

        writer.WriteEndElement();
    }

    private static void WritePeriod(XmlWriter writer, string elementName, InvoicingPeriod? period)
    {
        if (period is null || (!period.StartDate.IsSet && !period.EndDate.IsSet))
        {
            return;
        }

        StartCac(writer, elementName);
        WriteDate(writer, "StartDate", period.StartDate);
        WriteDate(writer, "EndDate", period.EndDate);
        writer.WriteEndElement();
    }

    private static void WriteExtensions(ExtensionData extensions, XmlWriter writer)
    {
        foreach (ExtensionElement element in extensions)
        {
            writer.WriteRaw(element.Xml);
        }
    }

    private static void StartCac(XmlWriter writer, string localName) =>
        writer.WriteStartElement(UblNames.CacPrefix, localName, UblNames.Cac.NamespaceName);

    private static void Cbc(XmlWriter writer, string localName, string value) =>
        writer.WriteElementString(UblNames.CbcPrefix, localName, UblNames.Cbc.NamespaceName, XmlCharacters.Sanitize(value));

    private static void WriteText(XmlWriter writer, string localName, TextField field)
    {
        if (!field.IsSet)
        {
            return;
        }

        writer.WriteStartElement(UblNames.CbcPrefix, localName, UblNames.Cbc.NamespaceName);
        WriteAttributeIfSet(writer, "languageID", field.LanguageId);
        writer.WriteString(XmlCharacters.Sanitize(field.Raw ?? field.Value ?? string.Empty));
        writer.WriteEndElement();
    }

    private static void WriteCode(XmlWriter writer, string localName, CodeField field)
    {
        if (!field.IsSet)
        {
            return;
        }

        writer.WriteStartElement(UblNames.CbcPrefix, localName, UblNames.Cbc.NamespaceName);
        WriteAttributeIfSet(writer, "listID", field.ListId);
        WriteAttributeIfSet(writer, "listVersionID", field.ListVersionId);
        WriteAttributeIfSet(writer, "listAgencyID", field.ListAgencyId);
        writer.WriteString(XmlCharacters.Sanitize(field.Raw ?? field.Value ?? string.Empty));
        writer.WriteEndElement();
    }

    private static void WriteIdentifier(XmlWriter writer, string localName, IdentifierField field)
    {
        if (!field.IsSet)
        {
            return;
        }

        writer.WriteStartElement(UblNames.CbcPrefix, localName, UblNames.Cbc.NamespaceName);
        WriteAttributeIfSet(writer, "schemeID", field.SchemeId);
        WriteAttributeIfSet(writer, "schemeAgencyID", field.SchemeAgencyId);
        WriteAttributeIfSet(writer, "schemeVersionID", field.SchemeVersionId);
        writer.WriteString(XmlCharacters.Sanitize(field.Raw ?? field.Value ?? string.Empty));
        writer.WriteEndElement();
    }

    private static void WriteAmount(XmlWriter writer, string localName, AmountField field)
    {
        if (!field.IsSet)
        {
            return;
        }

        writer.WriteStartElement(UblNames.CbcPrefix, localName, UblNames.Cbc.NamespaceName);
        WriteAttributeIfSet(writer, "currencyID", field.CurrencyCode);
        writer.WriteString(XmlCharacters.Sanitize(field.Raw ?? Format(field.Value)));
        writer.WriteEndElement();
    }

    private static void WriteQuantity(XmlWriter writer, string localName, QuantityField field)
    {
        if (!field.IsSet)
        {
            return;
        }

        writer.WriteStartElement(UblNames.CbcPrefix, localName, UblNames.Cbc.NamespaceName);
        WriteAttributeIfSet(writer, "unitCode", field.UnitCode);
        WriteAttributeIfSet(writer, "unitCodeListVersionID", field.UnitCodeListVersion);
        writer.WriteString(XmlCharacters.Sanitize(field.Raw ?? Format(field.Value)));
        writer.WriteEndElement();
    }

    private static void WriteDecimal(XmlWriter writer, string localName, Field<decimal> field)
    {
        if (field.IsSet)
        {
            Cbc(writer, localName, field.Raw ?? Format(field.Value));
        }
    }

    private static void WriteDate(XmlWriter writer, string localName, DateField field)
    {
        if (!field.IsSet)
        {
            return;
        }

        Cbc(writer, localName, field.Raw ?? field.Value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty);
    }

    private static void WriteAttributeIfSet(XmlWriter writer, string name, string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            writer.WriteAttributeString(name, value);
        }
    }

    private static string Format(decimal? value) =>
        value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
}
