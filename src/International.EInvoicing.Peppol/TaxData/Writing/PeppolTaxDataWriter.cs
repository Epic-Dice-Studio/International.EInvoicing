using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Xml;
using International.EInvoicing.Model;
using International.EInvoicing.Peppol.TaxData.Model;
using International.EInvoicing.Values;
using International.EInvoicing.Xml;

namespace International.EInvoicing.Peppol.TaxData.Writing;

/// <summary>
/// Writes a tax data document, in the jurisdiction it declares.
/// </summary>
/// <remarks>
/// <para>
/// The reported document is a <em>projection</em> of the invoice: every rule that describes it is written as
/// "MUST NOT contain elements other than", so what is written here is the allowed set and nothing else.
/// Anything the invoice carries beyond it — the buyer reference, the seller's contact, payment terms — is
/// deliberately dropped rather than passed through, because passing it through is what makes the document
/// fail.
/// </para>
/// <para>
/// OpenPeppol publishes the rules but no schema, so the element order here is the one the rules themselves
/// enumerate, in the order they enumerate it. That is evidence rather than proof, and it is why the tests
/// judge what is written by the published assertions rather than by a fixture written alongside it.
/// </para>
/// </remarks>
[SuppressMessage(
    "Performance",
    "CA1822:Mark members as static",
    Justification = "An instance API so a caller can replace this writer through the registry.")]
public sealed class PeppolTaxDataWriter
{
    private const string Cbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
    private const string Cac = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";

    /// <summary>Writes <paramref name="document"/> to <paramref name="destination"/>. The stream is left open.</summary>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    public void Write(PeppolTaxData document, Stream destination)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(destination);

        var settings = new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "  ",
            Encoding = new UTF8Encoding(false),
            CloseOutput = false,
        };

        using XmlWriter writer = XmlWriter.Create(destination, settings);
        Write(document, writer);
    }

    /// <summary>Writes <paramref name="document"/> and returns it as XML text.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> is <c>null</c>.</exception>
    public string WriteToString(PeppolTaxData document)
    {
        ArgumentNullException.ThrowIfNull(document);

        using var buffer = new MemoryStream();
        Write(document, buffer);
        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    private static void Write(PeppolTaxData document, XmlWriter writer)
    {
        string pxs = document.Jurisdiction.Namespace;

        writer.WriteStartDocument();
        writer.WriteStartElement("pxs", "TaxData", pxs);
        writer.WriteAttributeString("xmlns", "cbc", null, Cbc);
        writer.WriteAttributeString("xmlns", "cac", null, Cac);

        Cbc_(writer, "CustomizationID", document.Jurisdiction.CustomizationId);
        Cbc_(writer, "ProfileID", PeppolTaxDataJurisdiction.ProfileId);
        Cbc_(writer, "UUID", document.Uuid);
        Cbc_(writer, "IssueDate", document.IssuedAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        Cbc_(writer, "IssueTime", document.IssuedAt.ToString("HH:mm:sszzz", CultureInfo.InvariantCulture));
        Pxs_(writer, pxs, "TaxDataTypeCode", document.TaxDataTypeCode);
        Pxs_(writer, pxs, "DocumentScope", document.DocumentScope);

        WriteAuthority(writer, pxs, document.Authority);

        Pxs_(writer, pxs, "ReporterRole", document.ReporterRole);

        WriteEndpoint(writer, pxs, "ReportingParty", document.ReportingParty);
        WriteEndpoint(writer, pxs, "ReceivingParty", document.ReceivingParty);
        WriteRepresentative(writer, pxs, document.ReportersRepresentative);

        writer.WriteStartElement("pxs", "ReportedTransaction", pxs);
        WriteReportedDocument(writer, pxs, document);
        writer.WriteEndElement();

        writer.WriteEndElement();
        writer.WriteEndDocument();
    }

    private static void WriteAuthority(XmlWriter writer, string pxs, PeppolTaxAuthority authority)
    {
        writer.WriteStartElement("pxs", "TaxAuthority", pxs);
        Cbc_(writer, "ID", authority.Id);
        Cbc_(writer, "Name", authority.Name);
        writer.WriteEndElement();
    }

    private static void WriteEndpoint(XmlWriter writer, string pxs, string element, PeppolTaxDataEndpoint party)
    {
        writer.WriteStartElement("pxs", element, pxs);
        writer.WriteStartElement("cbc", "EndpointID", Cbc);
        WriteAttributeIfSet(writer, "schemeID", party.SchemeId);
        writer.WriteString(XmlCharacters.Sanitize(party.Id));
        writer.WriteEndElement();
        writer.WriteEndElement();
    }

    private static void WriteRepresentative(XmlWriter writer, string pxs, PeppolTaxDataEndpoint? representative)
    {
        if (representative is null)
        {
            return;
        }

        writer.WriteStartElement("pxs", "ReportersRepresentative", pxs);
        writer.WriteStartElement("cac", "PartyIdentification", Cac);
        writer.WriteStartElement("cbc", "ID", Cbc);
        WriteAttributeIfSet(writer, "schemeID", representative.SchemeId);
        writer.WriteString(XmlCharacters.Sanitize(representative.Id));
        writer.WriteEndElement();
        writer.WriteEndElement();
        writer.WriteEndElement();
    }

    private static void WriteReportedDocument(XmlWriter writer, string pxs, PeppolTaxData document)
    {
        if (document.ReportedDocument is not { } invoice)
        {
            return;
        }

        string currency = invoice.CurrencyCode.Value ?? string.Empty;

        writer.WriteStartElement("pxs", "ReportedDocument", pxs);

        Cbc_(writer, "CustomizationID", invoice.SpecificationIdentifier.Value);
        Cbc_(writer, "ProfileID", invoice.BusinessProcessType.Value);
        Cbc_(writer, "ID", invoice.Number.Value);
        Cbc_(writer, "UUID", document.ReportedDocumentUuid);
        Cbc_(writer, "IssueDate", Date(invoice.IssueDate));
        Pxs_(writer, pxs, "DocumentTypeCode", invoice.TypeCode.Value);

        foreach (InvoiceNote note in invoice.Notes)
        {
            Cbc_(writer, "Note", note.Text.Value);
        }

        Cbc_(writer, "TaxPointDate", Date(invoice.TaxPointDate));
        Cbc_(writer, "DocumentCurrencyCode", currency);
        Cbc_(writer, "TaxCurrencyCode", invoice.TaxAccountingCurrencyCode.Value);

        WritePeriod(writer, invoice.Period);
        WriteBillingReferences(writer, invoice.PrecedingInvoices);
        WriteParty(writer, "AccountingSupplierParty", invoice.Seller, invoice.SellerTaxRepresentative, seller: true);
        WriteParty(writer, "AccountingCustomerParty", invoice.Buyer, taxRepresentative: null, seller: false);
        WriteDelivery(writer, invoice.Delivery);
        WritePaymentMeans(writer, invoice.Payment);

        foreach (AllowanceCharge allowanceCharge in invoice.AllowancesAndCharges)
        {
            WriteAllowanceCharge(writer, allowanceCharge, currency);
        }

        WriteTaxTotal(writer, invoice, currency);
        WriteMonetaryTotal(writer, pxs, invoice.Totals, currency);

        foreach (InvoiceLine line in invoice.Lines)
        {
            WriteLine(writer, pxs, line, currency);
        }

        writer.WriteEndElement();
    }

    private static void WritePeriod(XmlWriter writer, InvoicingPeriod? period)
    {
        if (period is null || (!period.StartDate.IsSet && !period.EndDate.IsSet))
        {
            return;
        }

        writer.WriteStartElement("cac", "InvoicePeriod", Cac);
        Cbc_(writer, "StartDate", Date(period.StartDate));
        Cbc_(writer, "EndDate", Date(period.EndDate));
        writer.WriteEndElement();
    }

    private static void WriteBillingReferences(XmlWriter writer, List<DocumentReference> references)
    {
        foreach (DocumentReference reference in references)
        {
            writer.WriteStartElement("cac", "BillingReference", Cac);
            writer.WriteStartElement("cac", "InvoiceDocumentReference", Cac);
            Cbc_(writer, "ID", reference.Identifier.Value);
            Cbc_(writer, "IssueDate", Date(reference.IssueDate));
            writer.WriteEndElement();
            writer.WriteEndElement();
        }
    }

    private static void WriteParty(
        XmlWriter writer,
        string element,
        Party? party,
        Party? taxRepresentative,
        bool seller)
    {
        if (party is null)
        {
            return;
        }

        writer.WriteStartElement("cac", element, Cac);
        writer.WriteStartElement("cac", "Party", Cac);

        WriteCountry(writer, "PostalAddress", party.Address?.CountryCode.Value);
        WriteVatScheme(writer, party.VatIdentifier.Value);

        if (!seller)
        {
            WriteLegalEntity(writer, party.Name.Value);
        }

        writer.WriteEndElement();

        if (!seller)
        {
            WriteTaxRepresentative(writer, taxRepresentative);
        }

        writer.WriteEndElement();

        if (seller)
        {
            WriteTaxRepresentative(writer, taxRepresentative);
        }
    }

    private static void WriteTaxRepresentative(XmlWriter writer, Party? representative)
    {
        if (representative is null)
        {
            return;
        }

        writer.WriteStartElement("cac", "TaxRepresentativeParty", Cac);
        WriteCountry(writer, "PostalAddress", representative.Address?.CountryCode.Value);
        WriteVatScheme(writer, representative.VatIdentifier.Value);
        writer.WriteEndElement();
    }

    private static void WriteCountry(XmlWriter writer, string element, string? countryCode)
    {
        if (string.IsNullOrEmpty(countryCode))
        {
            return;
        }

        writer.WriteStartElement("cac", element, Cac);
        writer.WriteStartElement("cac", "Country", Cac);
        Cbc_(writer, "IdentificationCode", countryCode);
        writer.WriteEndElement();
        writer.WriteEndElement();
    }

    private static void WriteVatScheme(XmlWriter writer, string? vatIdentifier)
    {
        if (string.IsNullOrEmpty(vatIdentifier))
        {
            return;
        }

        writer.WriteStartElement("cac", "PartyTaxScheme", Cac);
        Cbc_(writer, "CompanyID", vatIdentifier);
        writer.WriteStartElement("cac", "TaxScheme", Cac);
        Cbc_(writer, "ID", "VAT");
        writer.WriteEndElement();
        writer.WriteEndElement();
    }

    private static void WriteLegalEntity(XmlWriter writer, string? registrationName)
    {
        if (string.IsNullOrEmpty(registrationName))
        {
            return;
        }

        writer.WriteStartElement("cac", "PartyLegalEntity", Cac);
        Cbc_(writer, "RegistrationName", registrationName);
        writer.WriteEndElement();
    }

    private static void WriteDelivery(XmlWriter writer, DeliveryInformation? delivery)
    {
        if (delivery is null || !delivery.ActualDeliveryDate.IsSet)
        {
            return;
        }

        writer.WriteStartElement("cac", "Delivery", Cac);
        Cbc_(writer, "ActualDeliveryDate", Date(delivery.ActualDeliveryDate));
        writer.WriteEndElement();
    }

    private static void WritePaymentMeans(XmlWriter writer, PaymentInstructions? payment)
    {
        if (payment is null || string.IsNullOrEmpty(payment.MeansTypeCode.Value))
        {
            return;
        }

        writer.WriteStartElement("cac", "PaymentMeans", Cac);

        writer.WriteStartElement("cbc", "PaymentMeansCode", Cbc);
        WriteAttributeIfSet(writer, "name", payment.MeansText.Value);
        writer.WriteString(XmlCharacters.Sanitize(payment.MeansTypeCode.Value!));
        writer.WriteEndElement();

        Cbc_(writer, "PaymentID", payment.RemittanceInformation.Value);

        foreach (CreditTransfer transfer in payment.CreditTransfers)
        {
            writer.WriteStartElement("cac", "PayeeFinancialAccount", Cac);
            Cbc_(writer, "ID", transfer.AccountIdentifier.Value);

            if (!string.IsNullOrEmpty(transfer.ServiceProviderIdentifier.Value))
            {
                writer.WriteStartElement("cac", "FinancialInstitutionBranch", Cac);
                Cbc_(writer, "ID", transfer.ServiceProviderIdentifier.Value);
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        writer.WriteEndElement();
    }

    private static void WriteAllowanceCharge(XmlWriter writer, AllowanceCharge allowanceCharge, string currency)
    {
        writer.WriteStartElement("cac", "AllowanceCharge", Cac);
        Cbc_(writer, "ChargeIndicator", allowanceCharge.IsCharge ? "true" : "false");
        Cbc_(writer, "AllowanceChargeReasonCode", allowanceCharge.ReasonCode.Value);
        Cbc_(writer, "AllowanceChargeReason", allowanceCharge.Reason.Value);
        Cbc_(writer, "MultiplierFactorNumeric", Number(allowanceCharge.Percentage));
        WriteAmount(writer, "Amount", allowanceCharge.Amount, currency);
        WriteAmount(writer, "BaseAmount", allowanceCharge.BaseAmount, currency);
        WriteTaxCategory(writer, allowanceCharge.VatCategoryCode.Value, allowanceCharge.VatRate, exemptionReasonCode: null);
        writer.WriteEndElement();
    }

    private static void WriteTaxTotal(XmlWriter writer, EInvoice invoice, string currency)
    {
        if (!invoice.Totals.TaxAmount.IsSet && invoice.VatBreakdown.Count == 0)
        {
            return;
        }

        writer.WriteStartElement("cac", "TaxTotal", Cac);
        WriteAmount(writer, "TaxAmount", invoice.Totals.TaxAmount, currency);

        foreach (VatBreakdownEntry entry in invoice.VatBreakdown)
        {
            writer.WriteStartElement("cac", "TaxSubtotal", Cac);
            WriteAmount(writer, "TaxableAmount", entry.TaxableAmount, currency);
            WriteAmount(writer, "TaxAmount", entry.TaxAmount, currency);
            WriteTaxCategory(writer, entry.CategoryCode.Value, entry.Rate, entry.ExemptionReasonCode.Value);
            writer.WriteEndElement();
        }

        writer.WriteEndElement();
    }

    private static void WriteTaxCategory(
        XmlWriter writer,
        string? categoryCode,
        Field<decimal> rate,
        string? exemptionReasonCode)
    {
        if (string.IsNullOrEmpty(categoryCode))
        {
            return;
        }

        writer.WriteStartElement("cac", "TaxCategory", Cac);
        Cbc_(writer, "ID", categoryCode);
        Cbc_(writer, "Percent", Number(rate));
        Cbc_(writer, "TaxExemptionReasonCode", exemptionReasonCode);
        writer.WriteStartElement("cac", "TaxScheme", Cac);
        Cbc_(writer, "ID", "VAT");
        writer.WriteEndElement();
        writer.WriteEndElement();
    }

    private static void WriteMonetaryTotal(XmlWriter writer, string pxs, DocumentTotals totals, string currency)
    {
        writer.WriteStartElement("pxs", "MonetaryTotal", pxs);
        WriteAmount(writer, "LineExtensionAmount", totals.LineTotalAmount, currency);
        WriteAmount(writer, "TaxExclusiveAmount", totals.TaxExclusiveAmount, currency);
        WriteAmount(writer, "TaxInclusiveAmount", totals.TaxInclusiveAmount, currency);
        WriteAmount(writer, "AllowanceTotalAmount", totals.AllowanceTotalAmount, currency);
        WriteAmount(writer, "ChargeTotalAmount", totals.ChargeTotalAmount, currency);
        WriteAmount(writer, "PrepaidAmount", totals.PrepaidAmount, currency);
        WriteAmount(writer, "PayableRoundingAmount", totals.RoundingAmount, currency);
        WriteAmount(writer, "PayableAmount", totals.DuePayableAmount, currency);
        writer.WriteEndElement();
    }

    private static void WriteLine(XmlWriter writer, string pxs, InvoiceLine line, string currency)
    {
        writer.WriteStartElement("pxs", "DocumentLine", pxs);
        Cbc_(writer, "ID", line.Identifier.Value);
        Cbc_(writer, "Note", line.Note.Value);

        if (line.Quantity.Value is { } quantity)
        {
            writer.WriteStartElement("cbc", "InvoicedQuantity", Cbc);
            WriteAttributeIfSet(writer, "unitCode", line.Quantity.UnitCode);
            writer.WriteString(quantity.ToString(CultureInfo.InvariantCulture));
            writer.WriteEndElement();
        }

        WriteAmount(writer, "LineExtensionAmount", line.NetAmount, currency);
        WritePeriod(writer, line.Period);

        foreach (AllowanceCharge allowanceCharge in line.AllowancesAndCharges)
        {
            WriteAllowanceCharge(writer, allowanceCharge, currency);
        }

        WriteItem(writer, line);
        WritePrice(writer, line.Price, currency);

        writer.WriteEndElement();
    }

    private static void WriteItem(XmlWriter writer, InvoiceLine line)
    {
        if (line.Item is not { } item)
        {
            return;
        }

        writer.WriteStartElement("cac", "Item", Cac);
        Cbc_(writer, "Description", item.Description.Value);
        Cbc_(writer, "Name", item.Name.Value);

        foreach (CodeField classification in item.ClassificationCodes)
        {
            if (string.IsNullOrEmpty(classification.Value))
            {
                continue;
            }

            writer.WriteStartElement("cac", "CommodityClassification", Cac);
            writer.WriteStartElement("cbc", "ItemClassificationCode", Cbc);
            WriteAttributeIfSet(writer, "listID", classification.ListId);
            writer.WriteString(XmlCharacters.Sanitize(classification.Value!));
            writer.WriteEndElement();
            writer.WriteEndElement();
        }

        if (!string.IsNullOrEmpty(line.VatCategoryCode.Value))
        {
            writer.WriteStartElement("cac", "ClassifiedTaxCategory", Cac);
            Cbc_(writer, "ID", line.VatCategoryCode.Value);
            Cbc_(writer, "Percent", Number(line.VatRate));
            writer.WriteStartElement("cac", "TaxScheme", Cac);
            Cbc_(writer, "ID", "VAT");
            writer.WriteEndElement();
            writer.WriteEndElement();
        }

        writer.WriteEndElement();
    }

    private static void WritePrice(XmlWriter writer, LinePrice? price, string currency)
    {
        if (price is null || !price.NetPrice.IsSet)
        {
            return;
        }

        writer.WriteStartElement("cac", "Price", Cac);
        WriteAmount(writer, "PriceAmount", price.NetPrice, currency);
        writer.WriteEndElement();
    }

    private static void WriteAmount(XmlWriter writer, string element, AmountField amount, string currency)
    {
        if (amount.Value is not { } value)
        {
            return;
        }

        writer.WriteStartElement("cbc", element, Cbc);
        writer.WriteAttributeString("currencyID", amount.CurrencyCode ?? currency);
        writer.WriteString(value.ToString(CultureInfo.InvariantCulture));
        writer.WriteEndElement();
    }

    private static void WriteAttributeIfSet(XmlWriter writer, string name, string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            writer.WriteAttributeString(name, XmlCharacters.Sanitize(value));
        }
    }

    private static string? Date(DateField date) =>
        date.Value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string? Number(Field<decimal> field) =>
        field.Value?.ToString(CultureInfo.InvariantCulture);

    private static void Cbc_(XmlWriter writer, string element, string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            writer.WriteElementString("cbc", element, Cbc, XmlCharacters.Sanitize(value));
        }
    }

    private static void Pxs_(XmlWriter writer, string pxs, string element, string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            writer.WriteElementString("pxs", element, pxs, XmlCharacters.Sanitize(value));
        }
    }
}
