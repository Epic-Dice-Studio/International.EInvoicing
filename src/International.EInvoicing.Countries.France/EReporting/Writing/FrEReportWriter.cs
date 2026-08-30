using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Xml;
using International.EInvoicing.Countries.France.EReporting.Model;
using International.EInvoicing.Model;
using International.EInvoicing.Values;

namespace International.EInvoicing.Countries.France.EReporting.Writing;

/// <summary>
/// Writes a French e-reporting transmission.
/// </summary>
/// <remarks>
/// Flux 10 carries no XML namespace — not on the root, not on anything below it — and its element order is
/// the schema's. A field read from a document and not modified is written back from its raw text.
/// </remarks>
[SuppressMessage(
    "Performance",
    "CA1822:Mark members as static",
    Justification = "An instance API so a caller can replace this writer through the registry.")]
public sealed class FrEReportWriter
{
    /// <summary>Writes <paramref name="report"/> to <paramref name="stream"/>. The stream is left open.</summary>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    public void Write(FrEReport report, Stream stream)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(stream);

        var settings = new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "  ",
            Encoding = new System.Text.UTF8Encoding(false),
            CloseOutput = false,
        };

        using XmlWriter writer = XmlWriter.Create(stream, settings);
        Write(report, writer);
    }

    /// <summary>Writes <paramref name="report"/> and returns it as XML text.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="report"/> is <c>null</c>.</exception>
    public string WriteToString(FrEReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        using var stream = new MemoryStream();
        Write(report, stream);
        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void Write(FrEReport report, XmlWriter writer)
    {
        writer.WriteStartDocument();
        writer.WriteStartElement("Report");

        WriteDocument(writer, report.Document);

        if (report.Transactions is { } transactions)
        {
            WriteTransactions(writer, transactions);
        }

        if (report.Payments is { } payments)
        {
            WritePayments(writer, payments);
        }

        WriteExtensions(writer, report.Extensions);
        writer.WriteEndElement();
        writer.WriteEndDocument();
    }

    private static void WriteDocument(XmlWriter writer, FrEReportDocument document)
    {
        writer.WriteStartElement("ReportDocument");
        Identifier(writer, "Id", document.Identifier);
        Text(writer, "Name", document.Name);

        if (document.IssuedAt.IsSet)
        {
            writer.WriteStartElement("IssueDateTime");
            writer.WriteElementString("DateTimeString", Moment(document.IssuedAt));
            writer.WriteEndElement();
        }

        Code(writer, "TypeCode", document.TypeCode);
        WriteHeaderParty(writer, "Sender", document.Sender);
        WriteHeaderParty(writer, "Issuer", document.Issuer);
        WriteExtensions(writer, document.Extensions);
        writer.WriteEndElement();
    }

    private static void WriteHeaderParty(XmlWriter writer, string element, FrEReportParty? party)
    {
        if (party is null)
        {
            return;
        }

        writer.WriteStartElement(element);
        Identifier(writer, "Id", party.Identifier, "schemeId");
        Text(writer, "Name", party.Name);
        Code(writer, "RoleCode", party.RoleCode);

        if (party.ElectronicAddress.IsSet)
        {
            writer.WriteStartElement("URIUniversalCommunication");
            Identifier(writer, "URIID", party.ElectronicAddress);
            writer.WriteEndElement();
        }

        WriteExtensions(writer, party.Extensions);
        writer.WriteEndElement();
    }

    private static void WriteTransactions(XmlWriter writer, FrTransactionsReport report)
    {
        writer.WriteStartElement("TransactionsReport");
        WritePeriod(writer, "ReportPeriod", report.Period);

        foreach (FrReportedInvoice invoice in report.Invoices)
        {
            WriteInvoice(writer, invoice);
        }

        foreach (FrTransactionSummary summary in report.Summaries)
        {
            WriteSummary(writer, summary);
        }

        WriteExtensions(writer, report.Extensions);
        writer.WriteEndElement();
    }

    private static void WriteInvoice(XmlWriter writer, FrReportedInvoice invoice)
    {
        writer.WriteStartElement("Invoice");
        Identifier(writer, "ID", invoice.Identifier);
        Date(writer, "IssueDate", invoice.IssueDate);
        Code(writer, "TypeCode", invoice.TypeCode);
        Code(writer, "CurrencyCode", invoice.CurrencyCode);
        Date(writer, "DueDate", invoice.DueDate);
        Code(writer, "TaxDueDateTypeCode", invoice.TaxDueDateTypeCode);

        foreach (FrReportedNote note in invoice.Notes)
        {
            writer.WriteStartElement("IncludedNote");
            Code(writer, "Subject", note.SubjectCode);
            Text(writer, "Content", note.Content);
            writer.WriteEndElement();
        }

        writer.WriteStartElement("BusinessProcess");
        Identifier(writer, "ID", invoice.BusinessProcess.Identifier);
        Identifier(writer, "TypeID", invoice.BusinessProcess.ProfileIdentifier);
        writer.WriteEndElement();

        foreach (FrReportedDocumentReference reference in invoice.ReferencedDocuments)
        {
            writer.WriteStartElement("ReferencedDocument");
            Identifier(writer, "ID", reference.Identifier);
            Date(writer, "IssueDate", reference.IssueDate);
            writer.WriteEndElement();
        }

        WriteTradeParty(writer, "Seller", invoice.Seller);
        WriteTradeParty(writer, "Buyer", invoice.Buyer);

        if (invoice.SellerTaxRepresentative is { } representative)
        {
            writer.WriteStartElement("SellerTaxRepresentative");
            Identifier(writer, "TaxRegistrationId", representative.Identifier, "schemeId");
            writer.WriteEndElement();
        }

        foreach (FrReportedDelivery delivery in invoice.Deliveries)
        {
            writer.WriteStartElement("Delivery");
            Date(writer, "Date", delivery.Date);
            WriteLocation(writer, delivery.Location);
            writer.WriteEndElement();
        }

        if (invoice.InvoicePeriod is { } period)
        {
            WritePeriod(writer, "InvoicePeriod", period);
        }

        foreach (FrReportedAllowanceCharge allowance in invoice.AllowancesAndCharges)
        {
            WriteAllowance(writer, allowance, withTax: true);
        }

        writer.WriteStartElement("MonetaryTotal");
        Amount(writer, "TaxExclusiveAmount", invoice.Totals.TaxExclusiveAmount, currencyAttribute: null);
        Amount(writer, "TaxAmount", invoice.Totals.TaxAmount, "CurrencyCode");
        writer.WriteEndElement();

        foreach (FrReportedTaxSubtotal subtotal in invoice.TaxSubtotals)
        {
            writer.WriteStartElement("TaxSubTotal");
            Amount(writer, "TaxableAmount", subtotal.TaxableAmount, currencyAttribute: null);
            Amount(writer, "TaxAmount", subtotal.TaxAmount, currencyAttribute: null);
            writer.WriteStartElement("TaxCategory");
            Code(writer, "Code", subtotal.CategoryCode);
            Number(writer, "Percent", subtotal.Percent);
            Text(writer, "TaxExemptionReason", subtotal.ExemptionReason);
            Code(writer, "TaxExemptionReasonCode", subtotal.ExemptionReasonCode);
            writer.WriteEndElement();
            writer.WriteEndElement();
        }

        foreach (FrReportedInvoiceLine line in invoice.Lines)
        {
            WriteLine(writer, line);
        }

        WriteExtensions(writer, invoice.Extensions);
        writer.WriteEndElement();
    }

    private static void WriteTradeParty(XmlWriter writer, string element, FrReportedParty? party)
    {
        if (party is null)
        {
            return;
        }

        writer.WriteStartElement(element);
        Identifier(writer, "CompanyId", party.CompanyIdentifier, "schemeId");

        if (party.TaxRegistration is { } registration)
        {
            Identifier(writer, "TaxRegistrationId", registration.Identifier, "qualifyingId");
        }

        if (party.CountryCode.IsSet)
        {
            writer.WriteStartElement("PostalAddress");
            Code(writer, "CountryId", party.CountryCode);
            writer.WriteEndElement();
        }

        WriteExtensions(writer, party.Extensions);
        writer.WriteEndElement();
    }

    private static void WriteLocation(XmlWriter writer, FrPostalLocation? location)
    {
        if (location is null)
        {
            return;
        }

        writer.WriteStartElement("Location");
        Text(writer, "LineOne", location.LineOne);
        Text(writer, "LineTwo", location.LineTwo);
        Text(writer, "LineThree", location.LineThree);
        Text(writer, "CityName", location.CityName);
        Text(writer, "PostalZone", location.PostalZone);
        Text(writer, "CountrySubentity", location.CountrySubentity);
        Code(writer, "CountryId", location.CountryCode);
        writer.WriteEndElement();
    }

    private static void WriteAllowance(XmlWriter writer, FrReportedAllowanceCharge allowance, bool withTax)
    {
        writer.WriteStartElement("AllowanceCharge");
        writer.WriteAttributeString(
            "ChargeIndicator",
            allowance.IsCharge.Raw ?? (allowance.IsCharge.Value == true ? "true" : "false"));

        Amount(writer, "Amount", allowance.Amount, currencyAttribute: null);

        if (withTax)
        {
            Code(writer, "TaxCategoryCode", allowance.TaxCategoryCode);
            Number(writer, "TaxPercent", allowance.TaxPercent);
        }

        writer.WriteEndElement();
    }

    private static void WriteLine(XmlWriter writer, FrReportedInvoiceLine line)
    {
        writer.WriteStartElement("Line");

        foreach (FrReportedNote note in line.Notes)
        {
            writer.WriteStartElement("Note");
            Code(writer, "Code", note.SubjectCode);
            Text(writer, "Comment", note.Content);
            writer.WriteEndElement();
        }

        if (line.BilledQuantity.IsSet)
        {
            writer.WriteStartElement("BilledQuantity");

            if (!string.IsNullOrEmpty(line.BilledQuantity.UnitCode))
            {
                writer.WriteAttributeString("UnitCode", line.BilledQuantity.UnitCode);
            }

            writer.WriteString(line.BilledQuantity.Raw ?? Decimal(line.BilledQuantity.Value));
            writer.WriteEndElement();
        }

        if (line.ReferencedDocument is { } reference)
        {
            writer.WriteStartElement("ReferencedDocument");
            Identifier(writer, "ID", reference.Identifier);
            Date(writer, "IssueDate", reference.IssueDate);
            writer.WriteEndElement();
        }

        if (line.Delivery is { } delivery)
        {
            writer.WriteStartElement("Delivery");
            Text(writer, "Name", delivery.Name);
            WriteLocation(writer, delivery.Location);
            writer.WriteEndElement();
        }

        if (line.InvoicePeriod is { } period)
        {
            WritePeriod(writer, "InvoicePeriod", period);
        }

        foreach (FrReportedAllowanceCharge allowance in line.AllowancesAndCharges)
        {
            WriteAllowance(writer, allowance, withTax: false);
        }

        if (line.Price is { } price)
        {
            writer.WriteStartElement("Price");
            Amount(writer, "PriceAmount", price.NetAmount, currencyAttribute: null);
            Amount(writer, "AllowanceChargeAmount", price.DiscountAmount, currencyAttribute: null);
            Amount(writer, "AllowanceChargeBaseAmount", price.GrossAmount, currencyAttribute: null);
            writer.WriteEndElement();
        }

        if (line.ProductName.IsSet)
        {
            writer.WriteStartElement("Product");
            Text(writer, "Name", line.ProductName);
            writer.WriteEndElement();
        }

        WriteExtensions(writer, line.Extensions);
        writer.WriteEndElement();
    }

    private static void WriteSummary(XmlWriter writer, FrTransactionSummary summary)
    {
        writer.WriteStartElement("Transactions");
        Date(writer, "Date", summary.Date);
        Code(writer, "TransactionsCurrency", summary.CurrencyCode);
        Code(writer, "TaxDueDateTypeCode", summary.TaxDueDateTypeCode);
        Code(writer, "CategoryCode", summary.CategoryCode);
        Amount(writer, "TaxExclusiveAmount", summary.TaxExclusiveAmount, currencyAttribute: null);
        Amount(writer, "TaxTotal", summary.TaxAmount, currencyAttribute: null);

        if (summary.TransactionCount.IsSet)
        {
            writer.WriteElementString(
                "TransactionsCount",
                summary.TransactionCount.Raw
                    ?? summary.TransactionCount.Value?.ToString(CultureInfo.InvariantCulture)
                    ?? string.Empty);
        }

        foreach (FrTransactionTaxSubtotal subtotal in summary.TaxSubtotals)
        {
            writer.WriteStartElement("TaxSubtotal");
            Number(writer, "TaxPercent", subtotal.Percent);
            Amount(writer, "TaxableAmount", subtotal.TaxableAmount, currencyAttribute: null);
            Amount(writer, "TaxTotal", subtotal.TaxAmount, currencyAttribute: null);
            writer.WriteEndElement();
        }

        WriteExtensions(writer, summary.Extensions);
        writer.WriteEndElement();
    }

    private static void WritePayments(XmlWriter writer, FrPaymentsReport report)
    {
        writer.WriteStartElement("PaymentsReport");
        WritePeriod(writer, "ReportPeriod", report.Period);

        foreach (FrReportedInvoicePayment invoice in report.Invoices)
        {
            writer.WriteStartElement("Invoice");
            Identifier(writer, "InvoiceID", invoice.InvoiceIdentifier);
            Date(writer, "IssueDate", invoice.InvoiceIssueDate);
            WritePayment(writer, invoice.Payment);
            writer.WriteEndElement();
        }

        foreach (FrReportedPayment payment in report.Transactions)
        {
            writer.WriteStartElement("Transactions");
            WritePayment(writer, payment);
            writer.WriteEndElement();
        }

        WriteExtensions(writer, report.Extensions);
        writer.WriteEndElement();
    }

    private static void WritePayment(XmlWriter writer, FrReportedPayment payment)
    {
        writer.WriteStartElement("Payment");
        Date(writer, "Date", payment.Date);

        foreach (FrPaymentSubtotal subtotal in payment.Subtotals)
        {
            writer.WriteStartElement("SubTotals");
            Number(writer, "TaxPercent", subtotal.TaxPercent);
            Code(writer, "CurrencyCode", subtotal.CurrencyCode);
            Amount(writer, "Amount", subtotal.Amount, currencyAttribute: null);
            writer.WriteEndElement();
        }

        writer.WriteEndElement();
    }

    private static void WritePeriod(XmlWriter writer, string element, FrReportPeriod period)
    {
        writer.WriteStartElement(element);
        Date(writer, "StartDate", period.StartDate);
        Date(writer, "EndDate", period.EndDate);
        writer.WriteEndElement();
    }

    private static void WriteExtensions(XmlWriter writer, ExtensionData extensions)
    {
        foreach (ExtensionElement element in extensions)
        {
            writer.WriteRaw(element.Xml);
        }
    }

    private static void Text(XmlWriter writer, string element, TextField field)
    {
        if (field.IsSet)
        {
            writer.WriteElementString(element, field.Raw ?? field.Value ?? string.Empty);
        }
    }

    private static void Code(XmlWriter writer, string element, CodeField field)
    {
        if (field.IsSet)
        {
            writer.WriteElementString(element, field.Raw ?? field.Value ?? string.Empty);
        }
    }

    private static void Identifier(
        XmlWriter writer,
        string element,
        IdentifierField field,
        string? schemeAttribute = null)
    {
        if (!field.IsSet)
        {
            return;
        }

        writer.WriteStartElement(element);

        if (schemeAttribute is not null && !string.IsNullOrEmpty(field.SchemeId))
        {
            writer.WriteAttributeString(schemeAttribute, field.SchemeId);
        }

        writer.WriteString(field.Raw ?? field.Value ?? string.Empty);
        writer.WriteEndElement();
    }

    private static void Amount(XmlWriter writer, string element, AmountField field, string? currencyAttribute)
    {
        if (!field.IsSet)
        {
            return;
        }

        writer.WriteStartElement(element);

        if (currencyAttribute is not null && !string.IsNullOrEmpty(field.CurrencyCode))
        {
            writer.WriteAttributeString(currencyAttribute, field.CurrencyCode);
        }

        writer.WriteString(field.Raw ?? Decimal(field.Value));
        writer.WriteEndElement();
    }

    private static void Number(XmlWriter writer, string element, Field<decimal> field)
    {
        if (field.IsSet)
        {
            writer.WriteElementString(element, field.Raw ?? Decimal(field.Value));
        }
    }

    private static void Date(XmlWriter writer, string element, DateField field)
    {
        if (field.IsSet)
        {
            writer.WriteElementString(
                element,
                field.Raw ?? field.Value?.ToString("yyyyMMdd", CultureInfo.InvariantCulture) ?? string.Empty);
        }
    }

    private static string Moment(DateTimeField field) =>
        field.Raw
        ?? field.Value?.UtcDateTime.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture)
        ?? string.Empty;

    private static string Decimal(decimal? value) =>
        value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
}
