using System.Xml.Linq;
using International.EInvoicing.Configuration;
using International.EInvoicing.Diagnostics;
using International.EInvoicing.Documents;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;
using International.EInvoicing.Values;
using International.EInvoicing.Xml;

namespace International.EInvoicing.Ubl.Reading;

/// <summary>
/// Reads a UBL 2.1 invoice into the canonical model.
/// </summary>
/// <remarks>
/// Reading never throws on the document: a value that cannot be typed keeps its raw text, an element outside
/// EN 16931 is kept verbatim as extension data, and everything the reader had to give up is reported.
/// </remarks>
public sealed class UblInvoiceReader : IDocumentReader<EInvoice>
{
    private readonly EInvoicingOptions _options;
    private readonly IProfileResolver _profiles;

    /// <summary>Creates a reader using the supplied options and profile resolver.</summary>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    public UblInvoiceReader(EInvoicingOptions options, IProfileResolver profiles)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(profiles);

        _options = options;
        _profiles = profiles;
    }

    /// <inheritdoc />
    public DocumentSyntax Syntax => DocumentSyntax.Ubl;

    /// <summary>Reads an invoice from a stream. The stream is left open.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <c>null</c>.</exception>
    public ParseResult<EInvoice> Read(Stream stream)
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
        catch (System.Xml.XmlException exception)
        {
            diagnostics.Add(Diagnostic.Create(UblDiagnostics.MalformedDocument, exception.Message) with
            {
                Location = new SourceLocation(null, exception.LineNumber, exception.LinePosition),
            });

            return diagnostics.ToResult<EInvoice>(null);
        }

        return diagnostics.ToResult(ReadInvoice(root, diagnostics));
    }

    /// <summary>Reads an invoice from XML text.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="xml"/> is <c>null</c>.</exception>
    public ParseResult<EInvoice> Read(string xml)
    {
        ArgumentNullException.ThrowIfNull(xml);

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(xml));
        return Read(stream);
    }

    /// <inheritdoc />
    public async Task<ParseResult<EInvoice>> ReadAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        byte[] content = await DocumentStreams.ReadAllAsync(stream, cancellationToken).ConfigureAwait(false);

        using var buffered = new MemoryStream(content, writable: false);
        return Read(buffered);
    }

    private EInvoice ReadInvoice(XElement root, DiagnosticCollector diagnostics)
    {
        // A credit note is the same document under another root, with three elements renamed.
        UblDocumentShape shape = UblDocumentShape.Of(root);
        var mapped = new HashSet<XElement>();
        var values = new UblValueReader(diagnostics, mapped);
        Dictionary<XElement, InvoiceNode> owners = values.Owners;
        var invoice = new EInvoice();

        invoice.SpecificationIdentifier =
            ProfileIdentifier.FromDocument(Take(root, UblNames.Cbc + "CustomizationID", mapped)?.Value);
        invoice.BusinessProcessType = values.ReadIdentifier(Take(root, UblNames.Cbc + "ProfileID", mapped));
        invoice.Number = values.ReadIdentifier(Take(root, UblNames.Cbc + "ID", mapped));
        invoice.DocumentUuid = values.ReadIdentifier(Take(root, UblNames.Cbc + "UUID", mapped));
        invoice.IssueDate = values.ReadDate(Take(root, UblNames.Cbc + "IssueDate", mapped), "BT-2");
        invoice.DueDate = values.ReadDate(Take(root, UblNames.Cbc + "DueDate", mapped), "BT-9");
        invoice.TypeCode = values.ReadCode(Take(root, shape.TypeCode, mapped));
        invoice.TaxPointDate = values.ReadDate(Take(root, UblNames.Cbc + "TaxPointDate", mapped), "BT-7");
        invoice.CurrencyCode = values.ReadCode(Take(root, UblNames.Cbc + "DocumentCurrencyCode", mapped));
        invoice.TaxAccountingCurrencyCode = values.ReadCode(Take(root, UblNames.Cbc + "TaxCurrencyCode", mapped));
        invoice.BuyerAccountingReference = values.ReadText(Take(root, UblNames.Cbc + "AccountingCost", mapped));
        invoice.BuyerReference = values.ReadText(Take(root, UblNames.Cbc + "BuyerReference", mapped));

        foreach (XElement note in TakeAll(root, UblNames.Cbc + "Note", mapped))
        {
            // "#AAB#…" is BT-21 and BT-22 in one element, which is how UBL carries a coded note.
            invoice.Notes.Add(values.ReadNote(note));
        }

        XElement? period = Take(root, UblNames.Cac + "InvoicePeriod", mapped);
        invoice.Period = ReadPeriod(period, values);
        invoice.TaxPointDateCode = values.ReadCode(
            Descend(values, period, UblNames.Cbc + "DescriptionCode"));
        invoice.PurchaseOrderReference = ReadOrderReference(root, values, mapped, "ID");
        invoice.SalesOrderReference = ReadOrderReference(root, values, mapped, "SalesOrderID");

        foreach (XElement billing in TakeAll(root, UblNames.Cac + "BillingReference", mapped))
        {
            XElement? reference = Descend(values, billing, UblNames.Cac + "InvoiceDocumentReference");
            invoice.PrecedingInvoices.Add(new DocumentReference
            {
                Identifier = values.ReadIdentifier(reference?.Element(UblNames.Cbc + "ID")),
                IssueDate = values.ReadDate(reference?.Element(UblNames.Cbc + "IssueDate"), "BT-26"),
            });
        }

        // BT-16, BT-15 and BT-17. Each is one identifier in its own container, and each was read by the CII
        // side and by nothing here — so a document carrying them arrived with the fields empty and left with
        // the elements re-emitted as extension data, after the elements UBL requires them to precede.
        invoice.DespatchAdviceReference = values.ReadIdentifier(
            Take(root, UblNames.Cac + "DespatchDocumentReference", mapped)?.Element(UblNames.Cbc + "ID"));
        invoice.ReceivingAdviceReference = values.ReadIdentifier(
            Take(root, UblNames.Cac + "ReceiptDocumentReference", mapped)?.Element(UblNames.Cbc + "ID"));
        invoice.TenderOrLotReference = values.ReadIdentifier(
            Take(root, UblNames.Cac + "OriginatorDocumentReference", mapped)?.Element(UblNames.Cbc + "ID"));
        invoice.ContractReference = values.ReadIdentifier(
            Take(root, UblNames.Cac + "ContractDocumentReference", mapped)?.Element(UblNames.Cbc + "ID"));
        invoice.ProjectReference = values.ReadIdentifier(
            Take(root, UblNames.Cac + "ProjectReference", mapped)?.Element(UblNames.Cbc + "ID"));

        foreach (XElement document in TakeAll(root, UblNames.Cac + "AdditionalDocumentReference", mapped))
        {
            if (Limits.Exceeded(invoice.AdditionalDocuments.Count, _options.Limits.MaxAttachmentCount))
            {
                diagnostics.Add(Limits.TooMany(_options.Limits.MaxAttachmentCount, "attached documents"));
                break;
            }

            AdditionalDocument mappedDocument = ReadAdditionalDocument(document, values, _options.Limits);
            owners[document] = mappedDocument;
            invoice.AdditionalDocuments.Add(mappedDocument);
        }

        // Read before the parties, because it decides which of their registrations is BT-31.
        string taxScheme = TaxSchemeOf(root);
        invoice.TaxSchemeIdentifier = new Values.CodeField(taxScheme);

        invoice.Seller = ReadParty(
            Descend(values, Take(root, UblNames.Cac + "AccountingSupplierParty", mapped), UblNames.Cac + "Party"),
            values,
            taxScheme);
        invoice.Buyer = ReadParty(
            Descend(values, Take(root, UblNames.Cac + "AccountingCustomerParty", mapped), UblNames.Cac + "Party"),
            values,
            taxScheme);
        invoice.Payee = ReadParty(Take(root, UblNames.Cac + "PayeeParty", mapped), values, taxScheme);
        invoice.SellerTaxRepresentative = ReadParty(
            Take(root, UblNames.Cac + "TaxRepresentativeParty", mapped), values, taxScheme);

        invoice.Delivery = ReadDelivery(Take(root, UblNames.Cac + "Delivery", mapped), values);
        invoice.Payment = ReadPayment(root, values, mapped);
        invoice.PaymentTerms = values.ReadText(
            Descend(values, Take(root, UblNames.Cac + "PaymentTerms", mapped), UblNames.Cbc + "Note"));

        foreach (XElement allowance in TakeAll(root, UblNames.Cac + "AllowanceCharge", mapped))
        {
            invoice.AllowancesAndCharges.Add(ReadAllowanceCharge(allowance, values));
        }

        // Two TaxTotal elements are allowed, and the second is BT-111: the same tax in the currency the
        // seller accounts in. Reading only the first left it unmapped, so it was written back out of place.
        List<XElement> taxTotals = TakeAll(root, UblNames.Cac + "TaxTotal", mapped);
        ReadTaxTotal(taxTotals.FirstOrDefault(), invoice, values);

        foreach (XElement extra in taxTotals.Skip(1))
        {
            invoice.Totals.TaxAmountInAccountingCurrency = values.ReadAmount(
                Descend(values, extra, UblNames.Cbc + "TaxAmount"),
                "BT-111");
        }
        ReadTotals(Take(root, UblNames.Cac + "LegalMonetaryTotal", mapped), invoice.Totals, values);

        foreach (XElement line in TakeAll(root, shape.Line, mapped))
        {
            if (Limits.Exceeded(invoice.Lines.Count, _options.Limits.MaxDocumentLines))
            {
                diagnostics.Add(Limits.TooMany(_options.Limits.MaxDocumentLines, "invoice lines"));
                break;
            }

            InvoiceLine mappedLine = ReadLine(shape, line, values, owners);
            owners[line] = mappedLine;
            invoice.Lines.Add(mappedLine);
        }

        UblExtensions.KeepEverythingElse(root, invoice, mapped, owners, diagnostics);

        ProfileResolution resolution = _profiles.Resolve(invoice.SpecificationIdentifier, DocumentSyntax.Ubl);
        foreach (Diagnostic diagnostic in resolution.Diagnostics)
        {
            diagnostics.Add(diagnostic);
        }

        invoice.Profile = resolution;
        invoice.Diagnostics = diagnostics.Diagnostics;
        return invoice;
    }

    private static IdentifierField ReadOrderReference(
        XElement root,
        UblValueReader values,
        HashSet<XElement> mapped,
        string childName)
    {
        XElement? order = root.Element(UblNames.Cac + "OrderReference");
        if (order is null)
        {
            return IdentifierField.Unset;
        }

        mapped.Add(order);
        return values.ReadIdentifier(order.Element(UblNames.Cbc + childName));
    }

    private static InvoicingPeriod? ReadPeriod(XElement? element, UblValueReader values) =>
        element is null
            ? null
            : new InvoicingPeriod
            {
                StartDate = values.ReadDate(element.Element(UblNames.Cbc + "StartDate"), "BT-73"),
                EndDate = values.ReadDate(element.Element(UblNames.Cbc + "EndDate"), "BT-74"),
            };

    private static AdditionalDocument ReadAdditionalDocument(
        XElement element,
        UblValueReader values,
        DocumentLimits limits)
    {
        XElement? attachment = Descend(values, element, UblNames.Cac + "Attachment");
        // Descend, not Element: reading it is not enough, it has to be marked as read, or the raw element is
        // kept as extension data as well and the attachment is written twice — megabytes, duplicated.
        XElement? embedded = Descend(values, attachment, UblNames.Cbc + "EmbeddedDocumentBinaryObject");
        XElement? external = Descend(values, Descend(values, attachment, UblNames.Cac + "ExternalReference"), UblNames.Cbc + "URI");

        var document = new AdditionalDocument
        {
            Identifier = values.ReadIdentifier(element.Element(UblNames.Cbc + "ID")),
            Description = values.ReadText(element.Element(UblNames.Cbc + "DocumentDescription")),
            ExternalLocation = values.ReadText(external),
        };

        if (embedded is not null)
        {
            document.Attachment = ReadBinary(embedded, values, limits);
        }

        return document;
    }

    private static BinaryField ReadBinary(XElement element, UblValueReader values, DocumentLimits limits)
    {
        string mimeCode = element.Attribute("mimeCode")?.Value ?? string.Empty;
        string filename = element.Attribute("filename")?.Value ?? string.Empty;
        var source = new FieldSource(element.Value, UblValueReader.LocationOf(element));

        return Limits.Decode(element.Value, limits, values.Diagnostics) is { } decoded
            ? new BinaryField(decoded, mimeCode, filename, source)
            : new BinaryField(null, mimeCode, filename, source);
    }

    /// <summary>
    /// The tax scheme this document's categories belong to, taken from the breakdown where the standard puts
    /// it. <c>VAT</c> when the document says nothing, which is what EN 16931's own bindings assume.
    /// </summary>
    private static string TaxSchemeOf(XElement root) => root
        .Elements(UblNames.Cac + "TaxTotal")
        .Elements(UblNames.Cac + "TaxSubtotal")
        .Elements(UblNames.Cac + "TaxCategory")
        .Elements(UblNames.Cac + "TaxScheme")
        .Elements(UblNames.Cbc + "ID")
        .Select(id => id.Value.Trim())
        .FirstOrDefault(value => value.Length > 0) ?? "VAT";

    private static Party? ReadParty(XElement? element, UblValueReader values, string taxScheme)
    {
        if (element is null)
        {
            return null;
        }

        // BT-28 is the name a party trades under; BT-27, its legal name, lives in the legal entity below.
        var party = values.Own(element, new Party
        {
            TradingName = values.ReadText(
                Descend(values, Descend(values, element, UblNames.Cac + "PartyName"), UblNames.Cbc + "Name")),
            ElectronicAddress = values.ReadIdentifier(element.Element(UblNames.Cbc + "EndpointID")),
        });

        foreach (XElement identification in DescendAll(values, element, UblNames.Cac + "PartyIdentification"))
        {
            party.Identifiers.Add(values.ReadIdentifier(identification.Element(UblNames.Cbc + "ID")));
        }

        XElement? legalEntity = Descend(values, element, UblNames.Cac + "PartyLegalEntity");
        if (legalEntity is not null)
        {
            party.LegalRegistrationIdentifier = values.ReadIdentifier(legalEntity.Element(UblNames.Cbc + "CompanyID"));
            party.AdditionalLegalInformation = values.ReadText(legalEntity.Element(UblNames.Cbc + "CompanyLegalForm"));
            party.Name = values.ReadText(legalEntity.Element(UblNames.Cbc + "RegistrationName"));
        }

        // A document that gives only a trading name still has a party with a name. The trading name stays
        // where it was, so writing the document back does not drop the element it came from.
        if (!party.Name.IsSet)
        {
            party.Name = party.TradingName;
        }

        foreach (XElement registration in DescendAll(values, element, UblNames.Cac + "PartyTaxScheme"))
        {
            string? scheme = Descend(values, Descend(values, registration, UblNames.Cac + "TaxScheme"), UblNames.Cbc + "ID")?.Value;
            values.Consume(Descend(values, registration, UblNames.Cac + "TaxScheme"));
            IdentifierField companyId = values.ReadIdentifier(registration.Element(UblNames.Cbc + "CompanyID"));

            // BT-31 is "the seller's VAT identifier" in a standard written for Europe. Where the local tax
            // is called something else — GST in Australia and New Zealand — it is still BT-31, so the
            // document's own scheme decides, not the word VAT.
            if (string.Equals(scheme, taxScheme, StringComparison.OrdinalIgnoreCase)
                || string.Equals(scheme, "VAT", StringComparison.OrdinalIgnoreCase))
            {
                party.VatIdentifier = companyId;
            }
            else
            {
                party.TaxRegistrationIdentifier = companyId;
            }
        }

        party.Address = ReadAddress(Descend(values, element, UblNames.Cac + "PostalAddress"), values);
        party.Contact = ReadContact(Descend(values, element, UblNames.Cac + "Contact"), values);
        return party;
    }

    private static PostalAddress? ReadAddress(XElement? element, UblValueReader values) =>
        element is null
            ? null
            : values.Own(element, new PostalAddress
            {
                Line1 = values.ReadText(element.Element(UblNames.Cbc + "StreetName")),
                Line2 = values.ReadText(element.Element(UblNames.Cbc + "AdditionalStreetName")),
                Line3 = values.ReadText(
                    Descend(values, Descend(values, element, UblNames.Cac + "AddressLine"), UblNames.Cbc + "Line")),
                City = values.ReadText(element.Element(UblNames.Cbc + "CityName")),
                PostCode = values.ReadText(element.Element(UblNames.Cbc + "PostalZone")),
                CountrySubdivision = values.ReadText(element.Element(UblNames.Cbc + "CountrySubentity")),
                CountryCode = values.ReadCode(
                    Descend(values, Descend(values, element, UblNames.Cac + "Country"), UblNames.Cbc + "IdentificationCode")),
            });

    private static Contact? ReadContact(XElement? element, UblValueReader values) =>
        element is null
            ? null
            : values.Own(element, new Contact
            {
                Name = values.ReadText(element.Element(UblNames.Cbc + "Name")),
                Telephone = values.ReadText(element.Element(UblNames.Cbc + "Telephone")),
                Email = values.ReadText(element.Element(UblNames.Cbc + "ElectronicMail")),
            });

    private static DeliveryInformation? ReadDelivery(XElement? element, UblValueReader values)
    {
        if (element is null)
        {
            return null;
        }

        XElement? location = Descend(values, element, UblNames.Cac + "DeliveryLocation");

        return values.Own(element, new DeliveryInformation
        {
            ActualDeliveryDate = values.ReadDate(element.Element(UblNames.Cbc + "ActualDeliveryDate"), "BT-72"),
            LocationIdentifier = values.ReadIdentifier(location?.Element(UblNames.Cbc + "ID")),
            Name = values.ReadText(
                Descend(values, Descend(values, Descend(values, element, UblNames.Cac + "DeliveryParty"), UblNames.Cac + "PartyName"), UblNames.Cbc + "Name")),
            Address = ReadAddress(Descend(values, location, UblNames.Cac + "Address"), values),
        });
    }

    private static PaymentInstructions? ReadPayment(XElement root, UblValueReader values, HashSet<XElement> mapped)
    {
        // Every block, not the first: UBL allows one account per cac:PaymentMeans, so an invoice offering two
        // accounts repeats the whole block — which EN 16931's own examples do. Reading only the first loses
        // an account the sender meant you to be able to pay into.
        List<XElement> blocks = [.. TakeAll(root, UblNames.Cac + "PaymentMeans", mapped)];

        if (blocks.Count == 0)
        {
            return null;
        }

        XElement means = blocks[0];

        var payment = new PaymentInstructions
        {
            MeansTypeCode = values.ReadCode(Descend(values, means, UblNames.Cbc + "PaymentMeansCode")),
            RemittanceInformation = values.ReadText(Descend(values, means, UblNames.Cbc + "PaymentID")),
        };

        foreach (XElement block in blocks.Skip(1))
        {
            values.Consume(Descend(values, block, UblNames.Cbc + "PaymentMeansCode"));
            values.Consume(Descend(values, block, UblNames.Cbc + "PaymentID"));
        }

        foreach (XElement mandate in blocks.SelectMany(
            block => DescendAll(values, block, UblNames.Cac + "PaymentMandate")))
        {
            // BT-89 and BT-91. A direct debit says which mandate authorises it and which account it takes
            // from; neither was read here, though the model has held both since the CII side needed them.
            payment.DirectDebit ??= new DirectDebit();
            payment.DirectDebit.MandateReference = values.ReadIdentifier(
                Descend(values, mandate, UblNames.Cbc + "ID"));
            payment.DirectDebit.DebitedAccountIdentifier = values.ReadIdentifier(
                Descend(
                    values,
                    Descend(values, mandate, UblNames.Cac + "PayerFinancialAccount"),
                    UblNames.Cbc + "ID"));
        }

        foreach (XElement account in blocks.SelectMany(
            block => DescendAll(values, block, UblNames.Cac + "PayeeFinancialAccount")))
        {
            payment.CreditTransfers.Add(new CreditTransfer
            {
                AccountIdentifier = values.ReadIdentifier(Descend(values, account, UblNames.Cbc + "ID")),
                AccountName = values.ReadText(Descend(values, account, UblNames.Cbc + "Name")),
                ServiceProviderIdentifier = values.ReadIdentifier(
                    Descend(
                        values,
                        Descend(values, account, UblNames.Cac + "FinancialInstitutionBranch"),
                        UblNames.Cbc + "ID")),
            });
        }

        return payment;
    }

    private static AllowanceCharge ReadAllowanceCharge(XElement element, UblValueReader values)
    {
        XElement? category = Descend(values, element, UblNames.Cac + "TaxCategory");
        XElement? indicator = element.Element(UblNames.Cbc + "ChargeIndicator");

        // Read as a flag rather than through a field, so it needs saying explicitly that it was mapped —
        // without this it lands in extension data and is written back a second time.
        values.Consume(indicator);
        values.Consume(Descend(values, category, UblNames.Cac + "TaxScheme"));
        values.Consume(Descend(values, Descend(values, category, UblNames.Cac + "TaxScheme"), UblNames.Cbc + "ID"));

        return values.Own(element, new AllowanceCharge
        {
            IsCharge = string.Equals(indicator?.Value.Trim(), "true", StringComparison.OrdinalIgnoreCase),
            Amount = values.ReadAmount(element.Element(UblNames.Cbc + "Amount")),
            BaseAmount = values.ReadAmount(element.Element(UblNames.Cbc + "BaseAmount")),
            Percentage = values.ReadDecimal(element.Element(UblNames.Cbc + "MultiplierFactorNumeric")),
            Reason = values.ReadText(element.Element(UblNames.Cbc + "AllowanceChargeReason")),
            ReasonCode = values.ReadCode(element.Element(UblNames.Cbc + "AllowanceChargeReasonCode")),
            VatCategoryCode = values.ReadCode(category?.Element(UblNames.Cbc + "ID")),
            VatRate = values.ReadDecimal(category?.Element(UblNames.Cbc + "Percent")),
        });
    }

    private static void ReadTaxTotal(XElement? element, EInvoice invoice, UblValueReader values)
    {
        if (element is null)
        {
            return;
        }

        invoice.Totals.TaxAmount = values.ReadAmount(element.Element(UblNames.Cbc + "TaxAmount"), "BT-110");

        foreach (XElement subtotal in DescendAll(values, element, UblNames.Cac + "TaxSubtotal"))
        {
            XElement? category = Descend(values, subtotal, UblNames.Cac + "TaxCategory");
            values.Consume(Descend(values, category, UblNames.Cac + "TaxScheme"));
            values.Consume(Descend(values, Descend(values, category, UblNames.Cac + "TaxScheme"), UblNames.Cbc + "ID"));

            invoice.VatBreakdown.Add(new VatBreakdownEntry
            {
                TaxableAmount = values.ReadAmount(subtotal.Element(UblNames.Cbc + "TaxableAmount"), "BT-116"),
                TaxAmount = values.ReadAmount(subtotal.Element(UblNames.Cbc + "TaxAmount"), "BT-117"),
                CategoryCode = values.ReadCode(category?.Element(UblNames.Cbc + "ID")),
                Rate = values.ReadDecimal(category?.Element(UblNames.Cbc + "Percent"), "BT-119"),
                ExemptionReason = values.ReadText(category?.Element(UblNames.Cbc + "TaxExemptionReason")),
                ExemptionReasonCode = values.ReadCode(category?.Element(UblNames.Cbc + "TaxExemptionReasonCode")),
            });
        }
    }

    private static void ReadTotals(XElement? element, DocumentTotals totals, UblValueReader values)
    {
        if (element is null)
        {
            return;
        }

        totals.LineTotalAmount = values.ReadAmount(element.Element(UblNames.Cbc + "LineExtensionAmount"), "BT-106");
        totals.AllowanceTotalAmount = values.ReadAmount(element.Element(UblNames.Cbc + "AllowanceTotalAmount"), "BT-107");
        totals.ChargeTotalAmount = values.ReadAmount(element.Element(UblNames.Cbc + "ChargeTotalAmount"), "BT-108");
        totals.TaxExclusiveAmount = values.ReadAmount(element.Element(UblNames.Cbc + "TaxExclusiveAmount"), "BT-109");
        totals.TaxInclusiveAmount = values.ReadAmount(element.Element(UblNames.Cbc + "TaxInclusiveAmount"), "BT-112");
        totals.PrepaidAmount = values.ReadAmount(element.Element(UblNames.Cbc + "PrepaidAmount"), "BT-113");
        totals.RoundingAmount = values.ReadAmount(element.Element(UblNames.Cbc + "PayableRoundingAmount"), "BT-114");
        totals.DuePayableAmount = values.ReadAmount(element.Element(UblNames.Cbc + "PayableAmount"), "BT-115");
    }

    private static InvoiceLine ReadLine(
        UblDocumentShape shape,
        XElement element,
        UblValueReader values,
        Dictionary<XElement, InvoiceNode> owners)
    {
        var line = new InvoiceLine
        {
            Identifier = values.ReadIdentifier(element.Element(UblNames.Cbc + "ID")),
            Note = values.ReadText(element.Element(UblNames.Cbc + "Note")),
            Quantity = values.ReadQuantity(element.Element(shape.Quantity), "BT-129"),
            NetAmount = values.ReadAmount(element.Element(UblNames.Cbc + "LineExtensionAmount"), "BT-131"),
            BuyerAccountingReference = values.ReadText(element.Element(UblNames.Cbc + "AccountingCost")),
            OrderLineReference = values.ReadIdentifier(
                Descend(values, Descend(values, element, UblNames.Cac + "OrderLineReference"), UblNames.Cbc + "LineID")),
            Period = ReadPeriod(Descend(values, element, UblNames.Cac + "InvoicePeriod"), values),
        };

        // BT-128, which UBL files as a document reference on the line.
        XElement? lineDocument = Descend(values, element, UblNames.Cac + "DocumentReference");
        if (lineDocument is not null)
        {
            values.Consume(Descend(values, lineDocument, UblNames.Cbc + "DocumentTypeCode"));
            line.ObjectIdentifier = values.ReadIdentifier(Descend(values, lineDocument, UblNames.Cbc + "ID"));
        }

        foreach (XElement allowance in DescendAll(values, element, UblNames.Cac + "AllowanceCharge"))
        {
            line.AllowancesAndCharges.Add(ReadAllowanceCharge(allowance, values));
        }

        XElement? price = Descend(values, element, UblNames.Cac + "Price");
        if (price is not null)
        {
            // The discount on a price is written as an allowance, whose indicator says which it is. Reading
            // the amounts and leaving the indicator behind kept it as extension data on the line, where UBL
            // does not allow a cbc:ChargeIndicator at all.
            values.Consume(
                Descend(values, Descend(values, price, UblNames.Cac + "AllowanceCharge"), UblNames.Cbc + "ChargeIndicator"));

            line.Price = new LinePrice
            {
                NetPrice = values.ReadAmount(price.Element(UblNames.Cbc + "PriceAmount"), "BT-146"),
                BaseQuantity = values.ReadQuantity(price.Element(UblNames.Cbc + "BaseQuantity"), "BT-149"),
                Discount = values.ReadAmount(
                    Descend(values, Descend(values, price, UblNames.Cac + "AllowanceCharge"), UblNames.Cbc + "Amount"),
                    "BT-147"),
                GrossPrice = values.ReadAmount(
                    Descend(
                        values,
                        Descend(values, price, UblNames.Cac + "AllowanceCharge"),
                        UblNames.Cbc + "BaseAmount"),
                    "BT-148"),
            };
        }

        XElement? item = Descend(values, element, UblNames.Cac + "Item");
        if (item is not null)
        {
            line.Item = ReadItem(item, values);
            owners[item] = line.Item;

            XElement? category = Descend(values, item, UblNames.Cac + "ClassifiedTaxCategory");
            values.Consume(Descend(values, category, UblNames.Cac + "TaxScheme"));
            values.Consume(Descend(values, Descend(values, category, UblNames.Cac + "TaxScheme"), UblNames.Cbc + "ID"));
            line.VatCategoryCode = values.ReadCode(category?.Element(UblNames.Cbc + "ID"));
            line.VatRate = values.ReadDecimal(category?.Element(UblNames.Cbc + "Percent"), "BT-152");
        }

        return line;
    }

    private static Item ReadItem(XElement element, UblValueReader values)
    {
        var item = new Item
        {
            Name = values.ReadText(element.Element(UblNames.Cbc + "Name")),
            Description = values.ReadText(element.Element(UblNames.Cbc + "Description")),
            SellerIdentifier = values.ReadIdentifier(
                Descend(values, Descend(values, element, UblNames.Cac + "SellersItemIdentification"), UblNames.Cbc + "ID")),
            BuyerIdentifier = values.ReadIdentifier(
                Descend(values, Descend(values, element, UblNames.Cac + "BuyersItemIdentification"), UblNames.Cbc + "ID")),
            StandardIdentifier = values.ReadIdentifier(
                Descend(values, Descend(values, element, UblNames.Cac + "StandardItemIdentification"), UblNames.Cbc + "ID")),
            OriginCountryCode = values.ReadCode(
                Descend(values, Descend(values, element, UblNames.Cac + "OriginCountry"), UblNames.Cbc + "IdentificationCode")),
        };

        foreach (XElement classification in DescendAll(values, element, UblNames.Cac + "CommodityClassification"))
        {
            item.Classifications.Add(
                UblClassification.Read(classification.Element(UblNames.Cbc + "ItemClassificationCode"), values));
        }

        foreach (XElement property in DescendAll(values, element, UblNames.Cac + "AdditionalItemProperty"))
        {
            item.Characteristics.Add(new ItemCharacteristic
            {
                Name = values.ReadText(property.Element(UblNames.Cbc + "Name")),
                Value = values.ReadText(property.Element(UblNames.Cbc + "Value")),
            });
        }

        return item;
    }

    /// <summary>Finds a composite child and marks it mapped, so it is not later kept as extension data.</summary>
    private static XElement? Descend(UblValueReader values, XElement? parent, XName name)
    {
        XElement? child = parent?.Element(name);
        values.Consume(child);
        return child;
    }

    private static List<XElement> DescendAll(UblValueReader values, XElement? parent, XName name)
    {
        List<XElement> children = [.. parent?.Elements(name) ?? []];
        foreach (XElement child in children)
        {
            values.Consume(child);
        }

        return children;
    }

    private static XElement? Take(XElement parent, XName name, HashSet<XElement> mapped)
    {
        XElement? element = parent.Element(name);
        if (element is not null)
        {
            mapped.Add(element);
        }

        return element;
    }

    private static List<XElement> TakeAll(XElement parent, XName name, HashSet<XElement> mapped)
    {
        List<XElement> elements = [.. parent.Elements(name)];
        foreach (XElement element in elements)
        {
            mapped.Add(element);
        }

        return elements;
    }
}
