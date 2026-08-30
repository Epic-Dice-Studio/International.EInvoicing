using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Xml;
using International.EInvoicing.Model;
using International.EInvoicing.Values;

using International.EInvoicing.Xml;

namespace International.EInvoicing.Cii.Writing;

/// <summary>
/// Writes a canonical invoice as UN/CEFACT Cross Industry Invoice (D22B).
/// </summary>
/// <remarks>
/// <para>
/// Element order is normative in CII, as it is in UBL: the schema declares sequences, and a document whose
/// elements are right but out of order is rejected by the receiver. The order below follows
/// <c>CrossIndustryInvoice_100pD22B.xsd</c>.
/// </para>
/// <para>
/// A field read from a document and not modified is written back from its raw text — including a date's
/// original <c>format</c> code — so a document that passes through unchanged comes out equivalent.
/// </para>
/// </remarks>
[SuppressMessage(
    "Performance",
    "CA1822:Mark members as static",
    Justification = "An instance API so a caller can replace this writer through the registry.")]
public sealed class CiiInvoiceWriter
{
    private const string VatTaxTypeCode = "VAT";

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
        writer.WriteStartElement(CiiNames.RsmPrefix, "CrossIndustryInvoice", CiiNames.Rsm.NamespaceName);
        writer.WriteAttributeString("xmlns", CiiNames.RamPrefix, null, CiiNames.Ram.NamespaceName);
        writer.WriteAttributeString("xmlns", CiiNames.QdtPrefix, null, CiiNames.Qdt.NamespaceName);
        writer.WriteAttributeString("xmlns", CiiNames.UdtPrefix, null, CiiNames.Udt.NamespaceName);

        WriteContext(invoice, writer);
        WriteDocument(invoice, writer);

        StartRsm(writer, "SupplyChainTradeTransaction");
        foreach (InvoiceLine line in invoice.Lines)
        {
            WriteLine(line, writer);
        }

        WriteAgreement(invoice, writer);
        WriteDelivery(invoice, writer);
        WriteSettlement(invoice, writer);
        writer.WriteEndElement();

        WriteExtensions(invoice.Extensions, writer);

        writer.WriteEndElement();
        writer.WriteEndDocument();
    }

    private static void WriteContext(EInvoice invoice, XmlWriter writer)
    {
        StartRsm(writer, "ExchangedDocumentContext");

        if (invoice.BusinessProcessType.IsSet)
        {
            StartRam(writer, "BusinessProcessSpecifiedDocumentContextParameter");
            WriteIdentifier(writer, "ID", invoice.BusinessProcessType);
            writer.WriteEndElement();
        }

        if (invoice.SpecificationIdentifier.IsDeclared)
        {
            StartRam(writer, "GuidelineSpecifiedDocumentContextParameter");
            Ram(writer, "ID", invoice.SpecificationIdentifier.Value);
            writer.WriteEndElement();
        }

        writer.WriteEndElement();
    }

    private static void WriteDocument(EInvoice invoice, XmlWriter writer)
    {
        StartRsm(writer, "ExchangedDocument");
        WriteIdentifier(writer, "ID", invoice.Number);
        WriteCode(writer, "TypeCode", invoice.TypeCode);
        WriteDate(writer, "IssueDateTime", invoice.IssueDate);

        foreach (InvoiceNote note in invoice.Notes)
        {
            StartRam(writer, "IncludedNote");
            WriteText(writer, "Content", note.Text);
            WriteCode(writer, "SubjectCode", note.SubjectCode);
            writer.WriteEndElement();
        }

        writer.WriteEndElement();
    }

    private static void WriteLine(InvoiceLine line, XmlWriter writer)
    {
        StartRam(writer, "IncludedSupplyChainTradeLineItem");

        StartRam(writer, "AssociatedDocumentLineDocument");
        WriteIdentifier(writer, "LineID", line.Identifier);
        if (line.Note.IsSet)
        {
            StartRam(writer, "IncludedNote");
            WriteText(writer, "Content", line.Note);
            writer.WriteEndElement();
        }

        writer.WriteEndElement();

        WriteItem(line.Item, writer);
        WriteLineAgreement(line, writer);

        StartRam(writer, "SpecifiedLineTradeDelivery");
        WriteQuantity(writer, "BilledQuantity", line.Quantity);
        writer.WriteEndElement();

        WriteLineSettlement(line, writer);
        WriteExtensions(line.Extensions, writer);
        writer.WriteEndElement();
    }

    private static void WriteItem(Item? item, XmlWriter writer)
    {
        if (item is null)
        {
            return;
        }

        StartRam(writer, "SpecifiedTradeProduct");
        WriteIdentifier(writer, "GlobalID", item.StandardIdentifier);
        WriteIdentifier(writer, "SellerAssignedID", item.SellerIdentifier);
        WriteIdentifier(writer, "BuyerAssignedID", item.BuyerIdentifier);
        WriteText(writer, "Name", item.Name);
        WriteText(writer, "Description", item.Description);

        foreach (ItemCharacteristic characteristic in item.Characteristics)
        {
            StartRam(writer, "ApplicableProductCharacteristic");
            WriteText(writer, "Description", characteristic.Name);
            WriteText(writer, "Value", characteristic.Value);
            writer.WriteEndElement();
        }

        foreach (CodeField classification in item.ClassificationCodes.Where(c => c.IsSet))
        {
            StartRam(writer, "DesignatedProductClassification");
            WriteCode(writer, "ClassCode", classification);
            writer.WriteEndElement();
        }

        if (item.OriginCountryCode.IsSet)
        {
            StartRam(writer, "OriginTradeCountry");
            WriteCode(writer, "ID", item.OriginCountryCode);
            writer.WriteEndElement();
        }

        WriteExtensions(item.Extensions, writer);
        writer.WriteEndElement();
    }

    private static void WriteLineAgreement(InvoiceLine line, XmlWriter writer)
    {
        StartRam(writer, "SpecifiedLineTradeAgreement");

        if (line.OrderLineReference.IsSet)
        {
            StartRam(writer, "BuyerOrderReferencedDocument");
            WriteIdentifier(writer, "LineID", line.OrderLineReference);
            writer.WriteEndElement();
        }

        if (line.Price is { } price)
        {
            if (price.GrossPrice.IsSet || price.Discount.IsSet)
            {
                StartRam(writer, "GrossPriceProductTradePrice");
                WriteAmount(writer, "ChargeAmount", price.GrossPrice);
                WriteQuantity(writer, "BasisQuantity", price.BaseQuantity);

                if (price.Discount.IsSet)
                {
                    StartRam(writer, "AppliedTradeAllowanceCharge");
                    WriteIndicator(writer, "ChargeIndicator", false);
                    WriteAmount(writer, "ActualAmount", price.Discount);
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
            }

            if (price.NetPrice.IsSet || price.BaseQuantity.IsSet)
            {
                StartRam(writer, "NetPriceProductTradePrice");
                WriteAmount(writer, "ChargeAmount", price.NetPrice);
                WriteQuantity(writer, "BasisQuantity", price.BaseQuantity);
                writer.WriteEndElement();
            }
        }

        writer.WriteEndElement();
    }

    private static void WriteLineSettlement(InvoiceLine line, XmlWriter writer)
    {
        StartRam(writer, "SpecifiedLineTradeSettlement");

        if (line.VatCategoryCode.IsSet || line.VatRate.IsSet)
        {
            StartRam(writer, "ApplicableTradeTax");
            Ram(writer, "TypeCode", VatTaxTypeCode);
            WriteCode(writer, "CategoryCode", line.VatCategoryCode);
            WriteDecimal(writer, "RateApplicablePercent", line.VatRate);
            writer.WriteEndElement();
        }

        WritePeriod(writer, "BillingSpecifiedPeriod", line.Period);

        foreach (AllowanceCharge allowanceCharge in line.AllowancesAndCharges)
        {
            WriteAllowanceCharge(allowanceCharge, writer);
        }

        StartRam(writer, "SpecifiedTradeSettlementLineMonetarySummation");
        WriteAmount(writer, "LineTotalAmount", line.NetAmount);
        writer.WriteEndElement();

        if (line.BuyerAccountingReference.IsSet)
        {
            StartRam(writer, "ReceivableSpecifiedTradeAccountingAccount");
            WriteText(writer, "ID", line.BuyerAccountingReference);
            writer.WriteEndElement();
        }

        writer.WriteEndElement();
    }

    private static void WriteAgreement(EInvoice invoice, XmlWriter writer)
    {
        StartRam(writer, "ApplicableHeaderTradeAgreement");
        WriteText(writer, "BuyerReference", invoice.BuyerReference);
        WriteParty(writer, "SellerTradeParty", invoice.Seller);
        WriteParty(writer, "BuyerTradeParty", invoice.Buyer);
        WriteParty(writer, "SellerTaxRepresentativeTradeParty", invoice.SellerTaxRepresentative);
        WriteReferencedDocument(writer, "BuyerOrderReferencedDocument", invoice.PurchaseOrderReference);
        WriteReferencedDocument(writer, "SellerOrderReferencedDocument", invoice.SalesOrderReference);
        WriteReferencedDocument(writer, "ContractReferencedDocument", invoice.ContractReference);

        foreach (AdditionalDocument document in invoice.AdditionalDocuments)
        {
            WriteAdditionalDocument(document, writer);
        }

        if (invoice.ProjectReference.IsSet)
        {
            StartRam(writer, "SpecifiedProcuringProject");
            WriteIdentifier(writer, "ID", invoice.ProjectReference);
            writer.WriteEndElement();
        }

        writer.WriteEndElement();
    }

    private static void WriteDelivery(EInvoice invoice, XmlWriter writer)
    {
        StartRam(writer, "ApplicableHeaderTradeDelivery");

        if (invoice.Delivery is { } delivery)
        {
            if (delivery.Name.IsSet || delivery.LocationIdentifier.IsSet || delivery.Address is not null)
            {
                StartRam(writer, "ShipToTradeParty");
                WriteIdentifier(writer, "ID", delivery.LocationIdentifier);
                WriteText(writer, "Name", delivery.Name);
                WriteAddress(writer, "PostalTradeAddress", delivery.Address);
                writer.WriteEndElement();
            }

            if (delivery.ActualDeliveryDate.IsSet)
            {
                StartRam(writer, "ActualDeliverySupplyChainEvent");
                WriteDate(writer, "OccurrenceDateTime", delivery.ActualDeliveryDate);
                writer.WriteEndElement();
            }
        }

        WriteReferencedDocument(writer, "DespatchAdviceReferencedDocument", invoice.DespatchAdviceReference);
        WriteReferencedDocument(writer, "ReceivingAdviceReferencedDocument", invoice.ReceivingAdviceReference);
        writer.WriteEndElement();
    }

    private static void WriteSettlement(EInvoice invoice, XmlWriter writer)
    {
        StartRam(writer, "ApplicableHeaderTradeSettlement");

        if (invoice.Payment?.DirectDebit?.CreditorIdentifier is { IsSet: true } creditor)
        {
            WriteIdentifier(writer, "CreditorReferenceID", creditor);
        }

        WriteText(writer, "PaymentReference", invoice.Payment?.RemittanceInformation ?? TextField.Unset);
        WriteCode(writer, "TaxCurrencyCode", invoice.TaxAccountingCurrencyCode);
        WriteCode(writer, "InvoiceCurrencyCode", invoice.CurrencyCode);
        WriteParty(writer, "PayeeTradeParty", invoice.Payee);
        WritePaymentMeans(invoice.Payment, writer);

        foreach (VatBreakdownEntry entry in invoice.VatBreakdown)
        {
            StartRam(writer, "ApplicableTradeTax");
            WriteAmount(writer, "CalculatedAmount", entry.TaxAmount);
            Ram(writer, "TypeCode", VatTaxTypeCode);
            WriteText(writer, "ExemptionReason", entry.ExemptionReason);
            WriteAmount(writer, "BasisAmount", entry.TaxableAmount);
            WriteCode(writer, "CategoryCode", entry.CategoryCode);
            WriteCode(writer, "ExemptionReasonCode", entry.ExemptionReasonCode);
            WriteDecimal(writer, "RateApplicablePercent", entry.Rate);
            writer.WriteEndElement();
        }

        WritePeriod(writer, "BillingSpecifiedPeriod", invoice.Period);

        foreach (AllowanceCharge allowanceCharge in invoice.AllowancesAndCharges)
        {
            WriteAllowanceCharge(allowanceCharge, writer);
        }

        if (invoice.PaymentTerms.IsSet || invoice.DueDate.IsSet)
        {
            StartRam(writer, "SpecifiedTradePaymentTerms");
            WriteText(writer, "Description", invoice.PaymentTerms);
            WriteDate(writer, "DueDateDateTime", invoice.DueDate);
            writer.WriteEndElement();
        }

        WriteTotals(invoice.Totals, writer);

        foreach (DocumentReference preceding in invoice.PrecedingInvoices)
        {
            StartRam(writer, "InvoiceReferencedDocument");
            WriteIdentifier(writer, "IssuerAssignedID", preceding.Identifier);
            WriteDate(writer, "FormattedIssueDateTime", preceding.IssueDate, CiiNames.QdtPrefix, CiiNames.Qdt.NamespaceName);
            writer.WriteEndElement();
        }

        if (invoice.BuyerAccountingReference.IsSet)
        {
            StartRam(writer, "ReceivableSpecifiedTradeAccountingAccount");
            WriteText(writer, "ID", invoice.BuyerAccountingReference);
            writer.WriteEndElement();
        }

        writer.WriteEndElement();
    }

    private static void WritePaymentMeans(PaymentInstructions? payment, XmlWriter writer)
    {
        if (payment is null || (!payment.MeansTypeCode.IsSet && payment.CreditTransfers.Count == 0))
        {
            return;
        }

        StartRam(writer, "SpecifiedTradeSettlementPaymentMeans");
        WriteCode(writer, "TypeCode", payment.MeansTypeCode);
        WriteText(writer, "Information", payment.MeansText);

        foreach (CreditTransfer transfer in payment.CreditTransfers)
        {
            StartRam(writer, "PayeePartyCreditorFinancialAccount");
            WriteAccountIdentifier(writer, transfer.AccountIdentifier);
            WriteText(writer, "AccountName", transfer.AccountName);
            writer.WriteEndElement();

            if (transfer.ServiceProviderIdentifier.IsSet)
            {
                StartRam(writer, "PayeeSpecifiedCreditorFinancialInstitution");
                WriteIdentifier(writer, "BICID", transfer.ServiceProviderIdentifier);
                writer.WriteEndElement();
            }
        }

        writer.WriteEndElement();
    }

    private static void WriteTotals(DocumentTotals totals, XmlWriter writer)
    {
        StartRam(writer, "SpecifiedTradeSettlementHeaderMonetarySummation");
        WriteAmount(writer, "LineTotalAmount", totals.LineTotalAmount);
        WriteAmount(writer, "ChargeTotalAmount", totals.ChargeTotalAmount);
        WriteAmount(writer, "AllowanceTotalAmount", totals.AllowanceTotalAmount);
        WriteAmount(writer, "TaxBasisTotalAmount", totals.TaxExclusiveAmount);
        WriteAmount(writer, "TaxTotalAmount", totals.TaxAmount);
        WriteAmount(writer, "RoundingAmount", totals.RoundingAmount);
        WriteAmount(writer, "GrandTotalAmount", totals.TaxInclusiveAmount);
        WriteAmount(writer, "TotalPrepaidAmount", totals.PrepaidAmount);
        WriteAmount(writer, "DuePayableAmount", totals.DuePayableAmount);
        writer.WriteEndElement();
    }

    private static void WriteAllowanceCharge(AllowanceCharge allowanceCharge, XmlWriter writer)
    {
        StartRam(writer, "SpecifiedTradeAllowanceCharge");
        WriteIndicator(writer, "ChargeIndicator", allowanceCharge.IsCharge);
        WriteDecimal(writer, "CalculationPercent", allowanceCharge.Percentage);
        WriteAmount(writer, "BasisAmount", allowanceCharge.BaseAmount);
        WriteAmount(writer, "ActualAmount", allowanceCharge.Amount);
        WriteCode(writer, "ReasonCode", allowanceCharge.ReasonCode);
        WriteText(writer, "Reason", allowanceCharge.Reason);

        if (allowanceCharge.VatCategoryCode.IsSet || allowanceCharge.VatRate.IsSet)
        {
            StartRam(writer, "CategoryTradeTax");
            Ram(writer, "TypeCode", VatTaxTypeCode);
            WriteCode(writer, "CategoryCode", allowanceCharge.VatCategoryCode);
            WriteDecimal(writer, "RateApplicablePercent", allowanceCharge.VatRate);
            writer.WriteEndElement();
        }

        writer.WriteEndElement();
    }

    private static void WriteAdditionalDocument(AdditionalDocument document, XmlWriter writer)
    {
        StartRam(writer, "AdditionalReferencedDocument");
        WriteIdentifier(writer, "IssuerAssignedID", document.Identifier);
        WriteText(writer, "URIID", document.ExternalLocation);

        // The CII type code of a supporting document has no business term, so it travels as extension data.
        WriteExtensions(document.Extensions, writer);
        WriteText(writer, "Name", document.Description);

        if (document.Attachment.Value is { } bytes)
        {
            writer.WriteStartElement(CiiNames.RamPrefix, "AttachmentBinaryObject", CiiNames.Ram.NamespaceName);
            WriteAttributeIfSet(writer, "mimeCode", document.Attachment.MimeCode);
            WriteAttributeIfSet(writer, "filename", document.Attachment.Filename);
            writer.WriteString(XmlCharacters.Sanitize(document.Attachment.Raw ?? Convert.ToBase64String(bytes)));
            writer.WriteEndElement();
        }

        writer.WriteEndElement();
    }

    private static void WriteParty(XmlWriter writer, string elementName, Party? party)
    {
        if (party is null)
        {
            return;
        }

        StartRam(writer, elementName);

        foreach (IdentifierField identifier in party.Identifiers.Where(i => i.IsSet))
        {
            WriteIdentifier(writer, identifier.SchemeId is null ? "ID" : "GlobalID", identifier);
        }

        WriteText(writer, "Name", party.Name);
        WriteText(writer, "Description", party.AdditionalLegalInformation);

        if (party.LegalRegistrationIdentifier.IsSet || party.TradingName.IsSet)
        {
            StartRam(writer, "SpecifiedLegalOrganization");
            WriteIdentifier(writer, "ID", party.LegalRegistrationIdentifier);
            WriteText(writer, "TradingBusinessName", party.TradingName);
            writer.WriteEndElement();
        }

        WriteContact(writer, party.Contact);
        WriteAddress(writer, "PostalTradeAddress", party.Address);

        if (party.ElectronicAddress.IsSet)
        {
            StartRam(writer, "URIUniversalCommunication");
            WriteIdentifier(writer, "URIID", party.ElectronicAddress);
            writer.WriteEndElement();
        }

        WriteTaxRegistration(writer, party.VatIdentifier, "VA");
        WriteTaxRegistration(writer, party.TaxRegistrationIdentifier, "FC");
        writer.WriteEndElement();
    }

    /// <summary>
    /// Writes BT-84 as the element CII expects. The scheme discriminates the two, and is not written as an
    /// attribute: in CII the element name already carries that meaning.
    /// </summary>
    private static void WriteAccountIdentifier(XmlWriter writer, IdentifierField account)
    {
        if (!account.IsSet)
        {
            return;
        }

        bool proprietary = string.Equals(
            account.SchemeId,
            CreditTransferSchemes.Proprietary,
            StringComparison.Ordinal);

        Ram(writer, proprietary ? "ProprietaryID" : "IBANID", account.Raw ?? account.Value ?? string.Empty);
    }

    private static void WriteTaxRegistration(XmlWriter writer, IdentifierField identifier, string scheme)
    {
        if (!identifier.IsSet)
        {
            return;
        }

        StartRam(writer, "SpecifiedTaxRegistration");
        writer.WriteStartElement(CiiNames.RamPrefix, "ID", CiiNames.Ram.NamespaceName);
        writer.WriteAttributeString("schemeID", XmlCharacters.Sanitize(identifier.SchemeId ?? scheme));
        writer.WriteString(XmlCharacters.Sanitize(identifier.Raw ?? identifier.Value ?? string.Empty));
        writer.WriteEndElement();
        writer.WriteEndElement();
    }

    private static void WriteContact(XmlWriter writer, Contact? contact)
    {
        if (contact is null)
        {
            return;
        }

        StartRam(writer, "DefinedTradeContact");
        WriteText(writer, "PersonName", contact.Name);

        if (contact.Telephone.IsSet)
        {
            StartRam(writer, "TelephoneUniversalCommunication");
            WriteText(writer, "CompleteNumber", contact.Telephone);
            writer.WriteEndElement();
        }

        if (contact.Email.IsSet)
        {
            StartRam(writer, "EmailURIUniversalCommunication");
            WriteText(writer, "URIID", contact.Email);
            writer.WriteEndElement();
        }

        writer.WriteEndElement();
    }

    private static void WriteAddress(XmlWriter writer, string elementName, PostalAddress? address)
    {
        if (address is null)
        {
            return;
        }

        StartRam(writer, elementName);
        WriteText(writer, "PostcodeCode", address.PostCode);
        WriteText(writer, "LineOne", address.Line1);
        WriteText(writer, "LineTwo", address.Line2);
        WriteText(writer, "LineThree", address.Line3);
        WriteText(writer, "CityName", address.City);
        WriteCode(writer, "CountryID", address.CountryCode);
        WriteText(writer, "CountrySubDivisionName", address.CountrySubdivision);
        writer.WriteEndElement();
    }

    private static void WriteReferencedDocument(XmlWriter writer, string elementName, IdentifierField identifier)
    {
        if (!identifier.IsSet)
        {
            return;
        }

        StartRam(writer, elementName);
        WriteIdentifier(writer, "IssuerAssignedID", identifier);
        writer.WriteEndElement();
    }

    private static void WritePeriod(XmlWriter writer, string elementName, InvoicingPeriod? period)
    {
        if (period is null || (!period.StartDate.IsSet && !period.EndDate.IsSet))
        {
            return;
        }

        StartRam(writer, elementName);
        WriteDate(writer, "StartDateTime", period.StartDate);
        WriteDate(writer, "EndDateTime", period.EndDate);
        writer.WriteEndElement();
    }

    private static void WriteExtensions(ExtensionData extensions, XmlWriter writer)
    {
        foreach (ExtensionElement element in extensions)
        {
            writer.WriteRaw(element.Xml);
        }
    }

    private static void StartRsm(XmlWriter writer, string localName) =>
        writer.WriteStartElement(CiiNames.RsmPrefix, localName, CiiNames.Rsm.NamespaceName);

    private static void StartRam(XmlWriter writer, string localName) =>
        writer.WriteStartElement(CiiNames.RamPrefix, localName, CiiNames.Ram.NamespaceName);

    private static void Ram(XmlWriter writer, string localName, string value) =>
        writer.WriteElementString(CiiNames.RamPrefix, localName, CiiNames.Ram.NamespaceName, XmlCharacters.Sanitize(value));

    private static void WriteText(XmlWriter writer, string localName, TextField field)
    {
        if (!field.IsSet)
        {
            return;
        }

        writer.WriteStartElement(CiiNames.RamPrefix, localName, CiiNames.Ram.NamespaceName);
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

        writer.WriteStartElement(CiiNames.RamPrefix, localName, CiiNames.Ram.NamespaceName);
        WriteAttributeIfSet(writer, "listID", field.ListId);
        WriteAttributeIfSet(writer, "listVersionID", field.ListVersionId);
        writer.WriteString(XmlCharacters.Sanitize(field.Raw ?? field.Value ?? string.Empty));
        writer.WriteEndElement();
    }

    private static void WriteIdentifier(XmlWriter writer, string localName, IdentifierField field)
    {
        if (!field.IsSet)
        {
            return;
        }

        writer.WriteStartElement(CiiNames.RamPrefix, localName, CiiNames.Ram.NamespaceName);
        WriteAttributeIfSet(writer, "schemeID", field.SchemeId);
        writer.WriteString(XmlCharacters.Sanitize(field.Raw ?? field.Value ?? string.Empty));
        writer.WriteEndElement();
    }

    private static void WriteAmount(XmlWriter writer, string localName, AmountField field)
    {
        if (!field.IsSet)
        {
            return;
        }

        writer.WriteStartElement(CiiNames.RamPrefix, localName, CiiNames.Ram.NamespaceName);
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

        writer.WriteStartElement(CiiNames.RamPrefix, localName, CiiNames.Ram.NamespaceName);
        WriteAttributeIfSet(writer, "unitCode", field.UnitCode);
        writer.WriteString(XmlCharacters.Sanitize(field.Raw ?? Format(field.Value)));
        writer.WriteEndElement();
    }

    private static void WriteDecimal(XmlWriter writer, string localName, Field<decimal> field)
    {
        if (field.IsSet)
        {
            Ram(writer, localName, field.Raw ?? Format(field.Value));
        }
    }

    private static void WriteIndicator(XmlWriter writer, string localName, bool value)
    {
        StartRam(writer, localName);
        writer.WriteElementString(
            CiiNames.UdtPrefix,
            "Indicator",
            CiiNames.Udt.NamespaceName, XmlCharacters.Sanitize(value ? "true" : "false"));
        writer.WriteEndElement();
    }

    /// <summary>
    /// Writes a CII date, which is a wrapper element containing a <c>udt:DateTimeString</c> whose
    /// <c>format</c> attribute says how to read it. The original format code is preserved when there is one.
    /// </summary>
    private static void WriteDate(
        XmlWriter writer,
        string localName,
        DateField field,
        string prefix = CiiNames.UdtPrefix,
        string? namespaceName = null)
    {
        if (!field.IsSet)
        {
            return;
        }

        StartRam(writer, localName);
        writer.WriteStartElement(prefix, "DateTimeString", namespaceName ?? CiiNames.Udt.NamespaceName);
        writer.WriteAttributeString("format", XmlCharacters.Sanitize(field.FormatCode ?? DateField.FormatCcyyMmDd));
        writer.WriteString(XmlCharacters.Sanitize(field.Raw ?? field.Value?.ToString("yyyyMMdd", CultureInfo.InvariantCulture) ?? string.Empty));
        writer.WriteEndElement();
        writer.WriteEndElement();
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
