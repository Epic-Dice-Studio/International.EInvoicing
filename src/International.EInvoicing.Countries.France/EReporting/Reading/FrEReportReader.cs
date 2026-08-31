using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using International.EInvoicing.Configuration;
using International.EInvoicing.Countries.France.EReporting.Model;
using International.EInvoicing.Diagnostics;
using International.EInvoicing.Model;
using International.EInvoicing.Values;
using International.EInvoicing.Xml;

namespace International.EInvoicing.Countries.France.EReporting.Reading;

/// <summary>
/// Reads a French e-reporting transmission into the model.
/// </summary>
/// <remarks>
/// Like every reader here, it reports rather than throws: a value it cannot interpret keeps its raw text and
/// says why, and an element it does not know is kept as extension data on the node that carried it.
/// </remarks>
public sealed class FrEReportReader
{
    private readonly EInvoicingOptions _options;

    /// <summary>Creates a reader using the supplied options.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <c>null</c>.</exception>
    public FrEReportReader(EInvoicingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options;
    }

    /// <summary>Creates a reader using the default options.</summary>
    public FrEReportReader()
        : this(new EInvoicingOptions())
    {
    }

    /// <summary>Reads a transmission from a stream. The stream is left open.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <c>null</c>.</exception>
    public ParseResult<FrEReport> Read(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var diagnostics = new DiagnosticCollector(_options.DiagnosticPolicy);

        XElement root;
        try
        {
            using var reader = SecureXml.CreateReader(stream, _options.Limits);
            root = XElement.Load(reader, LoadOptions.SetLineInfo);
            SecureXml.EnsureDepthWithin(root, _options.Limits);
        }
        catch (XmlException exception)
        {
            diagnostics.Add(Diagnostic.Create(FrEReportDiagnostics.MalformedDocument, exception.Message) with
            {
                Location = new SourceLocation(null, exception.LineNumber, exception.LinePosition),
            });

            return diagnostics.ToResult<FrEReport>(null);
        }

        return diagnostics.ToResult(ReadReport(root, diagnostics));
    }

    /// <summary>Reads a transmission from XML text.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="xml"/> is <c>null</c>.</exception>
    public ParseResult<FrEReport> Read(string xml)
    {
        ArgumentNullException.ThrowIfNull(xml);

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(xml));
        return Read(stream);
    }

    private static FrEReport ReadReport(XElement root, DiagnosticCollector diagnostics)
    {
        var values = new FrValueReader(diagnostics);
        var report = new FrEReport();

        if (values.In(root, "ReportDocument") is { } document)
        {
            report.Document = ReadDocument(document, values);
        }

        if (values.In(root, "TransactionsReport") is { } transactions)
        {
            report.Transactions = ReadTransactions(transactions, values);
        }

        if (values.In(root, "PaymentsReport") is { } payments)
        {
            report.Payments = ReadPayments(payments, values);
        }

        values.KeepEverythingElse(root, report);
        report.Diagnostics = diagnostics.Diagnostics;
        return report;
    }

    private static FrEReportDocument ReadDocument(XElement element, FrValueReader values)
    {
        var document = new FrEReportDocument
        {
            Identifier = FrValueReader.Identifier(values.In(element, "Id")),
            Name = FrValueReader.Text(values.In(element, "Name")),
            IssuedAt = values.Moment(values.In(values.In(element, "IssueDateTime"), "DateTimeString")),
            TypeCode = FrValueReader.Code(values.In(element, "TypeCode")),
            Sender = ReadHeaderParty(values.In(element, "Sender"), values),
            Issuer = ReadHeaderParty(values.In(element, "Issuer"), values),
        };

        values.Own(element, document);
        return document;
    }

    private static FrEReportParty? ReadHeaderParty(XElement? element, FrValueReader values)
    {
        if (element is null)
        {
            return null;
        }

        var party = new FrEReportParty
        {
            Identifier = FrValueReader.Identifier(values.In(element, "Id"), "schemeId"),
            Name = FrValueReader.Text(values.In(element, "Name")),
            RoleCode = FrValueReader.Code(values.In(element, "RoleCode")),
            ElectronicAddress = FrValueReader.Identifier(
                values.In(values.In(element, "URIUniversalCommunication"), "URIID")),
        };

        values.Own(element, party);
        return party;
    }

    private static FrTransactionsReport ReadTransactions(XElement element, FrValueReader values)
    {
        var report = new FrTransactionsReport { Period = ReadPeriod(values.In(element, "ReportPeriod"), values) };

        foreach (XElement invoice in values.AllIn(element, "Invoice"))
        {
            report.Invoices.Add(ReadInvoice(invoice, values));
        }

        foreach (XElement summary in values.AllIn(element, "Transactions"))
        {
            report.Summaries.Add(ReadSummary(summary, values));
        }

        values.Own(element, report);
        return report;
    }

    private static FrReportPeriod ReadPeriod(XElement? element, FrValueReader values)
    {
        var period = new FrReportPeriod
        {
            StartDate = values.Date(values.In(element, "StartDate")),
            EndDate = values.Date(values.In(element, "EndDate")),
        };

        if (element is not null)
        {
            values.Own(element, period);
        }

        return period;
    }

    private static FrReportedInvoice ReadInvoice(XElement element, FrValueReader values)
    {
        var invoice = new FrReportedInvoice
        {
            Identifier = FrValueReader.Identifier(values.In(element, "ID")),
            IssueDate = values.Date(values.In(element, "IssueDate")),
            TypeCode = FrValueReader.Code(values.In(element, "TypeCode")),
            CurrencyCode = FrValueReader.Code(values.In(element, "CurrencyCode")),
            DueDate = values.Date(values.In(element, "DueDate")),
            TaxDueDateTypeCode = FrValueReader.Code(values.In(element, "TaxDueDateTypeCode")),
            Seller = ReadTradeParty(values.In(element, "Seller"), values) ?? new FrReportedParty(),
            Buyer = ReadTradeParty(values.In(element, "Buyer"), values),
        };

        foreach (XElement note in values.AllIn(element, "IncludedNote"))
        {
            invoice.Notes.Add(ReadNote(note, values, "Subject", "Content"));
        }

        if (values.In(element, "BusinessProcess") is { } process)
        {
            invoice.BusinessProcess = new FrReportedBusinessProcess
            {
                Identifier = FrValueReader.Identifier(values.In(process, "ID")),
                ProfileIdentifier = FrValueReader.Identifier(values.In(process, "TypeID")),
            };

            values.Own(process, invoice.BusinessProcess);
        }

        foreach (XElement reference in values.AllIn(element, "ReferencedDocument"))
        {
            invoice.ReferencedDocuments.Add(ReadReference(reference, values));
        }

        if (values.In(element, "SellerTaxRepresentative") is { } representative)
        {
            invoice.SellerTaxRepresentative = new FrReportedTaxRegistration
            {
                Identifier = FrValueReader.Identifier(values.In(representative, "TaxRegistrationId"), "schemeId"),
            };

            values.Own(representative, invoice.SellerTaxRepresentative);
        }

        foreach (XElement delivery in values.AllIn(element, "Delivery"))
        {
            invoice.Deliveries.Add(ReadDelivery(delivery, values));
        }

        if (values.In(element, "InvoicePeriod") is { } period)
        {
            invoice.InvoicePeriod = ReadPeriod(period, values);
        }

        foreach (XElement allowance in values.AllIn(element, "AllowanceCharge"))
        {
            invoice.AllowancesAndCharges.Add(ReadAllowance(allowance, values));
        }

        if (values.In(element, "MonetaryTotal") is { } totals)
        {
            invoice.Totals = new FrReportedTotals
            {
                TaxExclusiveAmount = values.Amount(values.In(totals, "TaxExclusiveAmount")),
                TaxAmount = values.Amount(values.In(totals, "TaxAmount"), "CurrencyCode"),
            };

            values.Own(totals, invoice.Totals);
        }

        foreach (XElement subtotal in values.AllIn(element, "TaxSubTotal"))
        {
            invoice.TaxSubtotals.Add(ReadTaxSubtotal(subtotal, values));
        }

        foreach (XElement line in values.AllIn(element, "Line"))
        {
            invoice.Lines.Add(ReadLine(line, values));
        }

        values.Own(element, invoice);
        return invoice;
    }

    private static FrReportedNote ReadNote(XElement element, FrValueReader values, string code, string content)
    {
        var note = new FrReportedNote
        {
            SubjectCode = FrValueReader.Code(values.In(element, code)),
            Content = FrValueReader.Text(values.In(element, content)),
        };

        values.Own(element, note);
        return note;
    }

    private static FrReportedDocumentReference ReadReference(XElement element, FrValueReader values)
    {
        var reference = new FrReportedDocumentReference
        {
            Identifier = FrValueReader.Identifier(values.In(element, "ID")),
            IssueDate = values.Date(values.In(element, "IssueDate")),
        };

        values.Own(element, reference);
        return reference;
    }

    private static FrReportedParty? ReadTradeParty(XElement? element, FrValueReader values)
    {
        if (element is null)
        {
            return null;
        }

        var party = new FrReportedParty
        {
            CompanyIdentifier = FrValueReader.Identifier(values.In(element, "CompanyId"), "schemeId"),
            CountryCode = FrValueReader.Code(values.In(values.In(element, "PostalAddress"), "CountryId")),
        };

        if (values.In(element, "TaxRegistrationId") is { } registration)
        {
            party.TaxRegistration = new FrReportedTaxRegistration
            {
                Identifier = FrValueReader.Identifier(registration, "qualifyingId"),
            };
        }

        values.Own(element, party);
        return party;
    }

    private static FrReportedDelivery ReadDelivery(XElement element, FrValueReader values)
    {
        var delivery = new FrReportedDelivery
        {
            Date = values.Date(values.In(element, "Date")),
            Name = FrValueReader.Text(values.In(element, "Name")),
            Location = ReadLocation(values.In(element, "Location"), values),
        };

        values.Own(element, delivery);
        return delivery;
    }

    private static FrPostalLocation? ReadLocation(XElement? element, FrValueReader values)
    {
        if (element is null)
        {
            return null;
        }

        var location = new FrPostalLocation
        {
            LineOne = FrValueReader.Text(values.In(element, "LineOne")),
            LineTwo = FrValueReader.Text(values.In(element, "LineTwo")),
            LineThree = FrValueReader.Text(values.In(element, "LineThree")),
            CityName = FrValueReader.Text(values.In(element, "CityName")),
            PostalZone = FrValueReader.Text(values.In(element, "PostalZone")),
            CountrySubentity = FrValueReader.Text(values.In(element, "CountrySubentity")),
            CountryCode = FrValueReader.Code(values.In(element, "CountryId")),
        };

        values.Own(element, location);
        return location;
    }

    private static FrReportedAllowanceCharge ReadAllowance(XElement element, FrValueReader values)
    {
        var allowance = new FrReportedAllowanceCharge
        {
            IsCharge = FrValueReader.Indicator(element.Attribute("ChargeIndicator")?.Value),
            Amount = values.Amount(values.In(element, "Amount")),
            TaxCategoryCode = FrValueReader.Code(values.In(element, "TaxCategoryCode")),
            TaxPercent = values.Decimal(values.In(element, "TaxPercent")),
        };

        values.Own(element, allowance);
        return allowance;
    }

    private static FrReportedTaxSubtotal ReadTaxSubtotal(XElement element, FrValueReader values)
    {
        var subtotal = new FrReportedTaxSubtotal
        {
            TaxableAmount = values.Amount(values.In(element, "TaxableAmount")),
            TaxAmount = values.Amount(values.In(element, "TaxAmount")),
        };

        if (values.In(element, "TaxCategory") is { } category)
        {
            subtotal.CategoryCode = FrValueReader.Code(values.In(category, "Code"));
            subtotal.Percent = values.Decimal(values.In(category, "Percent"));
            subtotal.ExemptionReason = FrValueReader.Text(values.In(category, "TaxExemptionReason"));
            subtotal.ExemptionReasonCode = FrValueReader.Code(values.In(category, "TaxExemptionReasonCode"));
            values.Own(category, subtotal);
        }

        values.Own(element, subtotal);
        return subtotal;
    }

    private static FrReportedInvoiceLine ReadLine(XElement element, FrValueReader values)
    {
        var line = new FrReportedInvoiceLine
        {
            BilledQuantity = values.Quantity(values.In(element, "BilledQuantity")),
            ReferencedDocument = values.In(element, "ReferencedDocument") is { } reference
                ? ReadReference(reference, values)
                : null,
            Delivery = values.In(element, "Delivery") is { } delivery ? ReadDelivery(delivery, values) : null,
            InvoicePeriod = values.In(element, "InvoicePeriod") is { } period ? ReadPeriod(period, values) : null,
            ProductName = FrValueReader.Text(values.In(values.In(element, "Product"), "Name")),
        };

        foreach (XElement note in values.AllIn(element, "Note"))
        {
            line.Notes.Add(ReadNote(note, values, "Code", "Comment"));
        }

        foreach (XElement allowance in values.AllIn(element, "AllowanceCharge"))
        {
            line.AllowancesAndCharges.Add(ReadAllowance(allowance, values));
        }

        if (values.In(element, "Price") is { } price)
        {
            line.Price = new FrReportedPrice
            {
                NetAmount = values.Amount(values.In(price, "PriceAmount")),
                DiscountAmount = values.Amount(values.In(price, "AllowanceChargeAmount")),
                GrossAmount = values.Amount(values.In(price, "AllowanceChargeBaseAmount")),
            };

            values.Own(price, line.Price);
        }

        values.Own(element, line);
        return line;
    }

    private static FrTransactionSummary ReadSummary(XElement element, FrValueReader values)
    {
        var summary = new FrTransactionSummary
        {
            Date = values.Date(values.In(element, "Date")),
            CurrencyCode = FrValueReader.Code(values.In(element, "TransactionsCurrency")),
            TaxDueDateTypeCode = FrValueReader.Code(values.In(element, "TaxDueDateTypeCode")),
            CategoryCode = FrValueReader.Code(values.In(element, "CategoryCode")),
            TaxExclusiveAmount = values.Amount(values.In(element, "TaxExclusiveAmount")),
            TaxAmount = values.Amount(values.In(element, "TaxTotal")),
            TransactionCount = values.Integer(values.In(element, "TransactionsCount")),
        };

        foreach (XElement subtotal in values.AllIn(element, "TaxSubtotal"))
        {
            var split = new FrTransactionTaxSubtotal
            {
                Percent = values.Decimal(values.In(subtotal, "TaxPercent")),
                TaxableAmount = values.Amount(values.In(subtotal, "TaxableAmount")),
                TaxAmount = values.Amount(values.In(subtotal, "TaxTotal")),
            };

            values.Own(subtotal, split);
            summary.TaxSubtotals.Add(split);
        }

        values.Own(element, summary);
        return summary;
    }

    private static FrPaymentsReport ReadPayments(XElement element, FrValueReader values)
    {
        var report = new FrPaymentsReport { Period = ReadPeriod(values.In(element, "ReportPeriod"), values) };

        foreach (XElement invoice in values.AllIn(element, "Invoice"))
        {
            var paid = new FrReportedInvoicePayment
            {
                InvoiceIdentifier = FrValueReader.Identifier(values.In(invoice, "InvoiceID")),
                InvoiceIssueDate = values.Date(values.In(invoice, "IssueDate")),
                Payment = ReadPayment(values.In(invoice, "Payment"), values),
            };

            values.Own(invoice, paid);
            report.Invoices.Add(paid);
        }

        foreach (XElement transaction in values.AllIn(element, "Transactions"))
        {
            report.Transactions.Add(ReadPayment(values.In(transaction, "Payment"), values));
            values.Consume(transaction);
        }

        values.Own(element, report);
        return report;
    }

    private static FrReportedPayment ReadPayment(XElement? element, FrValueReader values)
    {
        var payment = new FrReportedPayment();

        if (element is null)
        {
            return payment;
        }

        payment.Date = values.Date(values.In(element, "Date"));

        foreach (XElement subtotal in values.AllIn(element, "SubTotals"))
        {
            var split = new FrPaymentSubtotal
            {
                TaxPercent = values.Decimal(values.In(subtotal, "TaxPercent")),
                CurrencyCode = FrValueReader.Code(values.In(subtotal, "CurrencyCode")),
                Amount = values.Amount(values.In(subtotal, "Amount")),
            };

            values.Own(subtotal, split);
            payment.Subtotals.Add(split);
        }

        values.Own(element, payment);
        return payment;
    }
}

/// <summary>Turns e-reporting elements into fields, remembering which ones were understood.</summary>
internal sealed class FrValueReader(DiagnosticCollector diagnostics)
{
    private readonly HashSet<XElement> _mapped = [];
    private readonly Dictionary<XElement, InvoiceNode> _owners = [];

    public XElement? In(XElement? parent, string name)
    {
        XElement? child = parent?.Element(name);
        Consume(child);
        return child;
    }

    public List<XElement> AllIn(XElement? parent, string name)
    {
        List<XElement> children = [.. parent?.Elements(name) ?? []];

        foreach (XElement child in children)
        {
            Consume(child);
        }

        return children;
    }

    public void Consume([NotNullWhen(true)] XElement? element)
    {
        if (element is not null)
        {
            _mapped.Add(element);
        }
    }

    /// <summary>Notes which node an element belongs to, so anything unmapped inside it lands there.</summary>
    public void Own(XElement element, InvoiceNode node) => _owners[element] = node;

    public static TextField Text(XElement? element) =>
        element is null ? TextField.Unset : new TextField(element.Value, null, Source(element));

    public static CodeField Code(XElement? element) =>
        element is null ? CodeField.Unset : new CodeField(element.Value, null, null, null, Source(element));

    public static IdentifierField Identifier(XElement? element, string? schemeAttribute = null) =>
        element is null
            ? IdentifierField.Unset
            : new IdentifierField(
                element.Value,
                schemeAttribute is null ? null : element.Attribute(schemeAttribute)?.Value,
                null,
                null,
                Source(element));

    public static IndicatorField Indicator(string? value) => value?.Trim().ToUpperInvariant() switch
    {
        null => IndicatorField.Unset,
        "TRUE" or "1" => new IndicatorField(true),
        "FALSE" or "0" => new IndicatorField(false),
        _ => new IndicatorField(null),
    };

    public AmountField Amount(XElement? element, string? currencyAttribute = null)
    {
        if (element is null)
        {
            return AmountField.Unset;
        }

        string? currency = currencyAttribute is null ? null : element.Attribute(currencyAttribute)?.Value;

        return decimal.TryParse(element.Value.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out decimal amount)
            ? new AmountField(amount, currency, Source(element))
            : new AmountField(null, currency, Source(element, Report(element, "an amount")));
    }

    public QuantityField Quantity(XElement? element)
    {
        if (element is null)
        {
            return QuantityField.Unset;
        }

        string? unit = element.Attribute("UnitCode")?.Value;

        return decimal.TryParse(element.Value.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out decimal quantity)
            ? new QuantityField(quantity, unit, null, Source(element))
            : new QuantityField(null, unit, null, Source(element, Report(element, "a quantity")));
    }

    public Field<decimal> Decimal(XElement? element)
    {
        if (element is null)
        {
            return Field<decimal>.Unset;
        }

        return decimal.TryParse(element.Value.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out decimal value)
            ? new Field<decimal>(value, Source(element))
            : new Field<decimal>(null, Source(element, Report(element, "a decimal number")));
    }

    public Field<int> Integer(XElement? element)
    {
        if (element is null)
        {
            return Field<int>.Unset;
        }

        return int.TryParse(element.Value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
            ? new Field<int>(value, Source(element))
            : new Field<int>(null, Source(element, Report(element, "a whole number")));
    }

    /// <summary>A date, which e-reporting writes as <c>CCYYMMDD</c>.</summary>
    public DateField Date(XElement? element)
    {
        if (element is null)
        {
            return DateField.Unset;
        }

        return DateOnly.TryParseExact(element.Value.Trim(), "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly date)
            ? new DateField(date, null, Source(element))
            : new DateField(null, null, Source(element, Report(element, "a date as CCYYMMDD")));
    }

    /// <summary>A timestamp, which e-reporting writes as <c>CCYYMMDDHHMMSS</c>.</summary>
    public DateTimeField Moment(XElement? element)
    {
        if (element is null)
        {
            return DateTimeField.Unset;
        }

        return DateTime.TryParseExact(
            element.Value.Trim(),
            "yyyyMMddHHmmss",
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out DateTime moment)
            ? new DateTimeField(new DateTimeOffset(moment, TimeSpan.Zero), null, Source(element))
            : new DateTimeField(null, null, Source(element, Report(element, "a timestamp as CCYYMMDDHHMMSS")));
    }

    /// <summary>Keeps what the model does not describe, on the node that carried it.</summary>
    public void KeepEverythingElse(XElement source, InvoiceNode node)
    {
        foreach (XElement element in source.Elements())
        {
            if (_mapped.Contains(element))
            {
                KeepEverythingElse(element, _owners.TryGetValue(element, out InvoiceNode? owner) ? owner : node);
                continue;
            }

            node.Extensions.Add(new ExtensionElement(
                element.Name.NamespaceName,
                element.Name.LocalName,
                element.ToString(SaveOptions.DisableFormatting),
                LocationOf(element)));

            diagnostics.Add(Diagnostic.Create(FrEReportDiagnostics.UnmappedElement, element.Name.LocalName) with
            {
                Location = LocationOf(element),
                Found = element.Name.LocalName,
                AppliedFallback = "kept verbatim as extension data",
            });
        }
    }

    private static SourceLocation LocationOf(XElement element)
    {
        var lineInfo = (IXmlLineInfo)element;
        var segments = new Stack<string>();

        for (XElement? current = element; current is not null; current = current.Parent)
        {
            segments.Push(current.Name.LocalName);
        }

        return new SourceLocation(
            "/" + string.Join('/', segments),
            lineInfo.HasLineInfo() ? lineInfo.LineNumber : 0,
            lineInfo.HasLineInfo() ? lineInfo.LinePosition : 0);
    }

    private static FieldSource Source(XElement element, Diagnostic? diagnostic = null) =>
        new(element.Value, LocationOf(element), diagnostic);

    private Diagnostic? Report(XElement element, string expected)
    {
        Diagnostic diagnostic = Diagnostic.Create(DiagnosticCodes.InvalidValue, element.Value.Trim(), expected) with
        {
            Location = LocationOf(element),
            Expected = expected,
            Found = element.Value.Trim(),
            AppliedFallback = "raw text preserved; typed value is null",
        };

        return diagnostics.Add(diagnostic);
    }
}
