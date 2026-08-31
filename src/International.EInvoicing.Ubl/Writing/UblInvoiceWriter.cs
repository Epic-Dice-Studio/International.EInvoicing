using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Xml;
using International.EInvoicing.Documents;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;
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
public sealed class UblInvoiceWriter : IDocumentWriter<EInvoice>
{
    /// <inheritdoc />
    public DocumentSyntax Syntax => DocumentSyntax.Ubl;

    /// <summary>Writes <paramref name="document"/> to <paramref name="destination"/>. The stream is left open.</summary>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    public void Write(EInvoice document, Stream destination)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(destination);

        var settings = new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "  ",
            Encoding = new System.Text.UTF8Encoding(false),
            CloseOutput = false,
        };

        using XmlWriter writer = XmlWriter.Create(destination, settings);
        Write(document, writer);
    }

    /// <summary>Writes <paramref name="document"/> and returns it as XML text.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> is <c>null</c>.</exception>
    public string WriteToString(EInvoice document)
    {
        ArgumentNullException.ThrowIfNull(document);

        using var buffer = new MemoryStream();
        Write(document, buffer);
        return System.Text.Encoding.UTF8.GetString(buffer.ToArray());
    }

    /// <inheritdoc />
    public Task WriteAsync(EInvoice document, Stream destination, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(destination);

        return DocumentStreams.WriteAllAsync(WriteToString(document), destination, cancellationToken);
    }

    private static void Write(EInvoice invoice, XmlWriter writer)
    {
        // A credit note is not an invoice with a different code in UBL: it is its own root element.
        UblDocumentShape shape = UblDocumentShape.For(invoice);

        writer.WriteStartDocument();
        writer.WriteStartElement(shape.Root.LocalName, shape.Root.NamespaceName);
        writer.WriteAttributeString("xmlns", UblNames.CacPrefix, null, UblNames.Cac.NamespaceName);
        writer.WriteAttributeString("xmlns", UblNames.CbcPrefix, null, UblNames.Cbc.NamespaceName);

        WriteDocumentLevel(invoice, shape, writer);
        WriteReferences(invoice, writer);
        WriteParties(invoice, writer);
        WriteDeliveryAndPayment(invoice, writer);

        string taxScheme = TaxSchemeOf(invoice);

        foreach (AllowanceCharge allowanceCharge in invoice.AllowancesAndCharges)
        {
            WriteAllowanceCharge(allowanceCharge, writer, taxScheme);
        }

        WriteTaxTotal(invoice, writer);
        WriteTotals(invoice.Totals, writer);

        foreach (InvoiceLine line in invoice.Lines)
        {
            WriteLine(line, shape, writer, taxScheme);
        }

        WriteExtensions(invoice.Extensions, writer);

        writer.WriteEndElement();
        writer.WriteEndDocument();
    }

    private static void WriteDocumentLevel(EInvoice invoice, UblDocumentShape shape, XmlWriter writer)
    {
        if (invoice.SpecificationIdentifier.IsDeclared)
        {
            Cbc(writer, "CustomizationID", invoice.SpecificationIdentifier.Value);
        }

        WriteIdentifier(writer, "ProfileID", invoice.BusinessProcessType);
        WriteIdentifier(writer, "ID", invoice.Number);
        WriteIdentifier(writer, "UUID", invoice.DocumentUuid);
        WriteDate(writer, "IssueDate", invoice.IssueDate);
        WriteDate(writer, "DueDate", invoice.DueDate);
        WriteCode(writer, shape.TypeCode.LocalName, invoice.TypeCode);

        foreach (InvoiceNote note in invoice.Notes)
        {
            // UBL has no element for BT-21: the subject code is a prefix on the note itself.
            WriteText(
                writer,
                "Note",
                new Values.TextField(UblNoteSubject.Join(note.SubjectCode.Value, note.Text.Value ?? string.Empty)));
        }

        WriteDate(writer, "TaxPointDate", invoice.TaxPointDate);
        WriteCode(writer, "DocumentCurrencyCode", invoice.CurrencyCode);
        WriteCode(writer, "TaxCurrencyCode", invoice.TaxAccountingCurrencyCode);
        WriteText(writer, "AccountingCost", invoice.BuyerAccountingReference);
        WriteText(writer, "BuyerReference", invoice.BuyerReference);
    }

    private static void WriteReferences(EInvoice invoice, XmlWriter writer)
    {
        WritePeriod(writer, "InvoicePeriod", invoice.Period, invoice.TaxPointDateCode);

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
        string taxScheme = TaxSchemeOf(invoice);

        WriteParty(writer, "AccountingSupplierParty", invoice.Seller, wrapped: true, taxScheme);
        WriteParty(writer, "AccountingCustomerParty", invoice.Buyer, wrapped: true, taxScheme);
        WriteParty(writer, "PayeeParty", invoice.Payee, wrapped: false, taxScheme);
        WriteParty(writer, "TaxRepresentativeParty", invoice.SellerTaxRepresentative, wrapped: false, taxScheme);
    }

    private static void WriteParty(XmlWriter writer, string elementName, Party? party, bool wrapped, string taxScheme)
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

        // BT-28: the name the party trades under, which is not necessarily its legal name below.
        if (party.TradingName.IsSet)
        {
            StartCac(writer, "PartyName");
            WriteText(writer, "Name", party.TradingName);
            writer.WriteEndElement();
        }

        WriteAddress(writer, "PostalAddress", party.Address);
        WriteTaxScheme(writer, party, taxScheme);
        WriteLegalEntity(writer, party);
        WriteContact(writer, party.Contact);

        if (wrapped)
        {
            writer.WriteEndElement();
        }

        writer.WriteEndElement();
    }

    private static void WriteTaxScheme(XmlWriter writer, Party party, string taxScheme)
    {
        if (party.VatIdentifier.IsSet)
        {
            StartCac(writer, "PartyTaxScheme");
            WriteIdentifier(writer, "CompanyID", party.VatIdentifier);
            StartCac(writer, "TaxScheme");
            Cbc(writer, "ID", taxScheme);
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

    private static void WriteAllowanceCharge(AllowanceCharge allowanceCharge, XmlWriter writer, string taxScheme)
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
            WriteDecimal(writer, "Percent", allowanceCharge.VatRate, twoDecimalsAtLeast: true);
            StartCac(writer, "TaxScheme");
            Cbc(writer, "ID", taxScheme);
            writer.WriteEndElement();
            writer.WriteEndElement();
        }

        writer.WriteEndElement();
    }

    /// <summary>
    /// The tax scheme this invoice's categories belong to: what it says, or VAT.
    /// </summary>
    /// <remarks>
    /// EN 16931's bindings say VAT and Australia and New Zealand require GST, so this is a document
    /// property rather than a constant — see <c>EInvoice.TaxSchemeIdentifier</c>.
    /// </remarks>
    private static string TaxSchemeOf(EInvoice invoice) =>
        invoice.TaxSchemeIdentifier.Value is { Length: > 0 } scheme ? scheme : "VAT";

    private static void WriteTaxTotal(EInvoice invoice, XmlWriter writer)
    {
        string taxScheme = TaxSchemeOf(invoice);

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
            WriteDecimal(writer, "Percent", entry.Rate, twoDecimalsAtLeast: true);
            WriteCode(writer, "TaxExemptionReasonCode", entry.ExemptionReasonCode);
            WriteText(writer, "TaxExemptionReason", entry.ExemptionReason);
            StartCac(writer, "TaxScheme");
            Cbc(writer, "ID", taxScheme);
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

    private static void WriteLine(InvoiceLine line, UblDocumentShape shape, XmlWriter writer, string taxScheme)
    {
        StartCac(writer, shape.Line.LocalName);
        WriteIdentifier(writer, "ID", line.Identifier);
        WriteText(writer, "Note", line.Note);
        WriteQuantity(writer, shape.Quantity.LocalName, line.Quantity);
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
            WriteAllowanceCharge(allowanceCharge, writer, taxScheme);
        }

        WriteItem(line, writer, taxScheme);
        WritePrice(line, writer);

        WriteExtensions(line.Extensions, writer);
        writer.WriteEndElement();
    }

    private static void WriteItem(InvoiceLine line, XmlWriter writer, string taxScheme)
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
            WriteDecimal(writer, "Percent", line.VatRate, twoDecimalsAtLeast: true);
            StartCac(writer, "TaxScheme");
            Cbc(writer, "ID", taxScheme);
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

    /// <summary>
    /// The invoicing period, and BT-8 with it.
    /// </summary>
    /// <remarks>
    /// UBL puts the tax point date code (BT-8) inside <c>cac:InvoicePeriod</c> as its description code,
    /// sharing the element with BG-14 — so a document may carry the code with no dates at all. Serbia's
    /// <c>RSR-05</c> requires exactly that, which is how the omission came to light.
    /// </remarks>
    private static void WritePeriod(
        XmlWriter writer,
        string elementName,
        InvoicingPeriod? period,
        CodeField taxPointDateCode = default)
    {
        bool hasDates = period is not null && (period.StartDate.IsSet || period.EndDate.IsSet);

        if (!hasDates && !taxPointDateCode.IsSet)
        {
            return;
        }

        StartCac(writer, elementName);

        if (period is not null)
        {
            WriteDate(writer, "StartDate", period.StartDate);
            WriteDate(writer, "EndDate", period.EndDate);
        }

        WriteCode(writer, "DescriptionCode", taxPointDateCode);
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
        writer.WriteString(XmlCharacters.Sanitize(field.Raw ?? FormatAmount(field.Value)));
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
        writer.WriteString(XmlCharacters.Sanitize(field.Raw ?? FormatAmount(field.Value)));
        writer.WriteEndElement();
    }

    private static void WriteDecimal(
        XmlWriter writer,
        string localName,
        Field<decimal> field,
        bool twoDecimalsAtLeast = false)
    {
        if (field.IsSet)
        {
            Cbc(writer, localName, field.Raw ?? (twoDecimalsAtLeast ? FormatAmount(field.Value) : Format(field.Value)));
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

    /// <summary>
    /// An amount, with at least two decimal places.
    /// </summary>
    /// <remarks>
    /// A decimal's natural form writes 1000 for a thousand euros, 23 for a VAT rate and 1 for a quantity —
    /// perfectly good numbers, and poor amounts. Portugal's <c>DT-CIUS-PT-094</c> and its neighbours require
    /// two decimals on each of those, and most implementations expect them everywhere. More than two are
    /// kept, since a unit price may legitimately carry them, and a field read from a document still writes
    /// back its original text — so this changes only the values this library produces itself.
    /// </remarks>
    private static string FormatAmount(decimal? value) =>
        value?.ToString("0.00###############", CultureInfo.InvariantCulture) ?? string.Empty;
}
