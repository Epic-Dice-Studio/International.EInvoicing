using System.Xml.Linq;
using International.EInvoicing.Configuration;
using International.EInvoicing.Diagnostics;
using International.EInvoicing.Documents;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;
using International.EInvoicing.Values;
using International.EInvoicing.Xml;

namespace International.EInvoicing.Cii.Reading;

/// <summary>
/// Reads a UN/CEFACT Cross Industry Invoice into the canonical model.
/// </summary>
/// <remarks>
/// Reading never throws on the document: a value that cannot be typed keeps its raw text, an element outside
/// EN 16931 is kept verbatim as extension data, and everything the reader had to give up is reported.
/// </remarks>
public sealed class CiiInvoiceReader : IDocumentReader<EInvoice>
{
    private readonly EInvoicingOptions _options;
    private readonly IProfileResolver _profiles;

    /// <summary>Creates a reader using the supplied options and profile resolver.</summary>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    public CiiInvoiceReader(EInvoicingOptions options, IProfileResolver profiles)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(profiles);

        _options = options;
        _profiles = profiles;
    }

    /// <inheritdoc />
    public DocumentSyntax Syntax => DocumentSyntax.Cii;

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
            diagnostics.Add(Diagnostic.Create(CiiDiagnostics.MalformedDocument, exception.Message) with
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
        var mapped = new HashSet<XElement>();
        var owners = new Dictionary<XElement, InvoiceNode>();
        var values = new CiiValueReader(diagnostics, mapped) { Limits = _options.Limits };
        var invoice = new EInvoice();

        ReadContext(root, invoice, values);
        ReadDocument(root, invoice, values);

        XElement? transaction = In(values, root, CiiNames.Rsm + "SupplyChainTradeTransaction");
        foreach (XElement line in AllIn(values, transaction, CiiNames.Ram + "IncludedSupplyChainTradeLineItem"))
        {
            if (Limits.Exceeded(invoice.Lines.Count, values.Limits.MaxDocumentLines))
            {
                diagnostics.Add(Limits.TooMany(values.Limits.MaxDocumentLines, "invoice lines"));
                break;
            }

            InvoiceLine mappedLine = ReadLine(line, values, owners);
            owners[line] = mappedLine;
            invoice.Lines.Add(mappedLine);
        }

        ReadAgreement(In(values, transaction, CiiNames.Ram + "ApplicableHeaderTradeAgreement"), invoice, values, owners);
        ReadDelivery(In(values, transaction, CiiNames.Ram + "ApplicableHeaderTradeDelivery"), invoice, values);
        ReadSettlement(In(values, transaction, CiiNames.Ram + "ApplicableHeaderTradeSettlement"), invoice, values);

        KeepEverythingElse(root, invoice, mapped, owners, diagnostics);

        ProfileResolution resolution = _profiles.Resolve(invoice.SpecificationIdentifier, DocumentSyntax.Cii);
        foreach (Diagnostic diagnostic in resolution.Diagnostics)
        {
            diagnostics.Add(diagnostic);
        }

        invoice.Profile = resolution;
        invoice.Diagnostics = diagnostics.Diagnostics;
        return invoice;
    }

    private static void ReadContext(XElement root, EInvoice invoice, CiiValueReader values)
    {
        XElement? context = In(values, root, CiiNames.Rsm + "ExchangedDocumentContext");

        invoice.BusinessProcessType = values.ReadIdentifier(
            In(values, In(values, context, CiiNames.Ram + "BusinessProcessSpecifiedDocumentContextParameter"), CiiNames.Ram + "ID"));

        invoice.SpecificationIdentifier = ProfileIdentifier.FromDocument(
            In(values, In(values, context, CiiNames.Ram + "GuidelineSpecifiedDocumentContextParameter"), CiiNames.Ram + "ID")?.Value);
    }

    private static void ReadDocument(XElement root, EInvoice invoice, CiiValueReader values)
    {
        XElement? document = In(values, root, CiiNames.Rsm + "ExchangedDocument");

        invoice.Number = values.ReadIdentifier(In(values, document, CiiNames.Ram + "ID"));
        invoice.TypeCode = values.ReadCode(In(values, document, CiiNames.Ram + "TypeCode"));
        invoice.IssueDate = values.ReadDate(In(values, document, CiiNames.Ram + "IssueDateTime"), "BT-2");

        foreach (XElement note in AllIn(values, document, CiiNames.Ram + "IncludedNote"))
        {
            invoice.Notes.Add(new InvoiceNote
            {
                Text = values.ReadText(In(values, note, CiiNames.Ram + "Content")),
                SubjectCode = values.ReadCode(In(values, note, CiiNames.Ram + "SubjectCode")),
            });
        }
    }

    private static void ReadAgreement(
        XElement? agreement,
        EInvoice invoice,
        CiiValueReader values,
        Dictionary<XElement, InvoiceNode> owners)
    {
        if (agreement is null)
        {
            return;
        }

        invoice.BuyerReference = values.ReadText(In(values, agreement, CiiNames.Ram + "BuyerReference"));
        invoice.Seller = ReadParty(In(values, agreement, CiiNames.Ram + "SellerTradeParty"), values);
        invoice.Buyer = ReadParty(In(values, agreement, CiiNames.Ram + "BuyerTradeParty"), values);
        invoice.SellerTaxRepresentative = ReadParty(
            In(values, agreement, CiiNames.Ram + "SellerTaxRepresentativeTradeParty"), values);

        invoice.PurchaseOrderReference = ReadReferencedDocument(agreement, "BuyerOrderReferencedDocument", values);
        invoice.SalesOrderReference = ReadReferencedDocument(agreement, "SellerOrderReferencedDocument", values);
        invoice.ContractReference = ReadReferencedDocument(agreement, "ContractReferencedDocument", values);

        invoice.ProjectReference = values.ReadIdentifier(
            In(values, In(values, agreement, CiiNames.Ram + "SpecifiedProcuringProject"), CiiNames.Ram + "ID"));

        // BT-11 is an identifier here and the project's name travels beside it; the model holds the
        // identifier, and the name has to be marked as read or it comes back where nothing may sit.
        values.Consume(
            In(values, In(values, agreement, CiiNames.Ram + "SpecifiedProcuringProject"), CiiNames.Ram + "Name"));

        foreach (XElement document in AllIn(values, agreement, CiiNames.Ram + "AdditionalReferencedDocument"))
        {
            if (Limits.Exceeded(invoice.AdditionalDocuments.Count, values.Limits.MaxAttachmentCount))
            {
                values.Diagnostics.Add(Limits.TooMany(values.Limits.MaxAttachmentCount, "attached documents"));
                break;
            }

            // A referenced document typed 130 is BT-18, the object this invoice is about, not a supporting
            // document. Reading every one of them as an attachment left the type code behind and the object
            // identifier unread.
            XElement? typeCode = In(values, document, CiiNames.Ram + "TypeCode");

            // The first one is BT-18; a document that carries several keeps the rest as supporting documents
            // rather than losing them to a field that holds one.
            if (typeCode?.Value.Trim() == InvoicedObjectTypeCode && !invoice.InvoicedObjectIdentifier.IsSet)
            {
                IdentifierField scheme = values.ReadIdentifier(
                    In(values, document, CiiNames.Ram + "ReferenceTypeCode"));

                invoice.InvoicedObjectIdentifier =
                    values.ReadIdentifier(In(values, document, CiiNames.Ram + "IssuerAssignedID"))
                    with
                    { SchemeId = scheme.Value };

                continue;
            }

            AdditionalDocument mappedDocument = ReadAdditionalDocument(document, values);
            owners[document] = mappedDocument;
            invoice.AdditionalDocuments.Add(mappedDocument);
        }
    }

    private static void ReadDelivery(XElement? delivery, EInvoice invoice, CiiValueReader values)
    {
        if (delivery is null)
        {
            return;
        }

        XElement? shipTo = In(values, delivery, CiiNames.Ram + "ShipToTradeParty");
        XElement? occurrence = In(values, delivery, CiiNames.Ram + "ActualDeliverySupplyChainEvent");

        var information = new DeliveryInformation
        {
            Name = values.ReadText(In(values, shipTo, CiiNames.Ram + "Name")),
            // BT-71 is written either as a plain ID or as a GlobalID carrying its scheme, and only the first
            // was read — so a delivery location identified the usual way, by GLN, was lost.
            LocationIdentifier = values.ReadIdentifier(
                In(values, shipTo, CiiNames.Ram + "ID") ?? In(values, shipTo, CiiNames.Ram + "GlobalID")),
            ActualDeliveryDate = values.ReadDate(
                In(values, occurrence, CiiNames.Ram + "OccurrenceDateTime"), "BT-72"),
            Address = ReadAddress(In(values, shipTo, CiiNames.Ram + "PostalTradeAddress"), values),
        };

        invoice.Delivery = information;
        invoice.DespatchAdviceReference = ReadReferencedDocument(delivery, "DespatchAdviceReferencedDocument", values);
        invoice.ReceivingAdviceReference = ReadReferencedDocument(delivery, "ReceivingAdviceReferencedDocument", values);
    }

    private static void ReadSettlement(XElement? settlement, EInvoice invoice, CiiValueReader values)
    {
        if (settlement is null)
        {
            return;
        }

        invoice.CurrencyCode = values.ReadCode(In(values, settlement, CiiNames.Ram + "InvoiceCurrencyCode"));
        invoice.TaxAccountingCurrencyCode = values.ReadCode(In(values, settlement, CiiNames.Ram + "TaxCurrencyCode"));
        invoice.BuyerAccountingReference = values.ReadText(In(values, settlement, CiiNames.Ram + "ReceivableSpecifiedTradeAccountingAccount") is { } account
            ? In(values, account, CiiNames.Ram + "ID")
            : null);
        invoice.Payee = ReadParty(In(values, settlement, CiiNames.Ram + "PayeeTradeParty"), values);
        invoice.Period = ReadPeriod(In(values, settlement, CiiNames.Ram + "BillingSpecifiedPeriod"), values);

        ReadPaymentMeans(settlement, invoice, values);

        foreach (XElement tax in AllIn(values, settlement, CiiNames.Ram + "ApplicableTradeTax"))
        {
            invoice.VatBreakdown.Add(new VatBreakdownEntry
            {
                TaxAmount = values.ReadAmount(In(values, tax, CiiNames.Ram + "CalculatedAmount"), "BT-117"),
                TaxableAmount = values.ReadAmount(In(values, tax, CiiNames.Ram + "BasisAmount"), "BT-116"),
                CategoryCode = values.ReadCode(In(values, tax, CiiNames.Ram + "CategoryCode")),
                Rate = values.ReadDecimal(In(values, tax, CiiNames.Ram + "RateApplicablePercent"), "BT-119"),
                ExemptionReason = values.ReadText(In(values, tax, CiiNames.Ram + "ExemptionReason")),
                ExemptionReasonCode = values.ReadCode(In(values, tax, CiiNames.Ram + "ExemptionReasonCode")),
            });

            values.Consume(In(values, tax, CiiNames.Ram + "TypeCode"));

            // BT-7. CII files the tax point date inside the breakdown rather than beside the issue date, so
            // reading it only at document level left it behind — and wrote it back out of place.
            if (!invoice.TaxPointDate.IsSet)
            {
                invoice.TaxPointDate = values.ReadDate(In(values, tax, CiiNames.Ram + "TaxPointDate"), "BT-7");
            }
            else
            {
                values.Consume(In(values, tax, CiiNames.Ram + "TaxPointDate"));
            }
        }

        foreach (XElement allowance in AllIn(values, settlement, CiiNames.Ram + "SpecifiedTradeAllowanceCharge"))
        {
            invoice.AllowancesAndCharges.Add(ReadAllowanceCharge(allowance, values));
        }

        XElement? terms = In(values, settlement, CiiNames.Ram + "SpecifiedTradePaymentTerms");
        invoice.PaymentTerms = values.ReadText(In(values, terms, CiiNames.Ram + "Description"));
        invoice.DueDate = values.ReadDate(In(values, terms, CiiNames.Ram + "DueDateDateTime"), "BT-9");

        ReadTotals(In(values, settlement, CiiNames.Ram + "SpecifiedTradeSettlementHeaderMonetarySummation"), invoice.Totals, values);

        foreach (XElement preceding in AllIn(values, settlement, CiiNames.Ram + "InvoiceReferencedDocument"))
        {
            invoice.PrecedingInvoices.Add(new DocumentReference
            {
                Identifier = values.ReadIdentifier(In(values, preceding, CiiNames.Ram + "IssuerAssignedID")),
                IssueDate = values.ReadDate(In(values, preceding, CiiNames.Ram + "FormattedIssueDateTime"), "BT-26"),
            });
        }
    }

    private static void ReadPaymentMeans(XElement settlement, EInvoice invoice, CiiValueReader values)
    {
        // Every block, not the first: one account per payment means is what the schema allows, so an invoice
        // offering two accounts repeats the block. Reading only the first loses an account the sender meant
        // you to be able to pay into.
        List<XElement> blocks = AllIn(values, settlement, CiiNames.Ram + "SpecifiedTradeSettlementPaymentMeans");
        XElement? means = blocks.FirstOrDefault();
        IdentifierField creditor = values.ReadIdentifier(In(values, settlement, CiiNames.Ram + "CreditorReferenceID"));
        TextField reference = values.ReadText(In(values, settlement, CiiNames.Ram + "PaymentReference"));

        if (means is null && !creditor.IsSet && !reference.IsSet)
        {
            return;
        }

        var payment = new PaymentInstructions { RemittanceInformation = reference };

        // BT-89 sits with the payment terms in CII, BT-91 with the payment means, and BT-90 here. Three
        // places for one instruction, which is how two of the three came to be read by nothing.
        IdentifierField mandate = values.ReadIdentifier(
            In(values, In(values, settlement, CiiNames.Ram + "SpecifiedTradePaymentTerms"), CiiNames.Ram + "DirectDebitMandateID"));

        IdentifierField debited = values.ReadIdentifier(
            In(
                values,
                In(values, means, CiiNames.Ram + "PayerPartyDebtorFinancialAccount"),
                CiiNames.Ram + "IBANID"));

        if (creditor.IsSet || mandate.IsSet || debited.IsSet)
        {
            payment.DirectDebit = new DirectDebit
            {
                CreditorIdentifier = creditor,
                MandateReference = mandate,
                DebitedAccountIdentifier = debited,
            };
        }

        if (means is not null)
        {
            payment.MeansTypeCode = values.ReadCode(In(values, means, CiiNames.Ram + "TypeCode"));
            payment.MeansText = values.ReadText(In(values, means, CiiNames.Ram + "Information"));
        }

        foreach (XElement block in blocks)
        {
            if (!ReferenceEquals(block, means))
            {
                values.Consume(In(values, block, CiiNames.Ram + "TypeCode"));
                values.Consume(In(values, block, CiiNames.Ram + "Information"));
            }

            XElement? account = In(values, block, CiiNames.Ram + "PayeePartyCreditorFinancialAccount");
            if (account is not null)
            {
                XElement? iban = In(values, account, CiiNames.Ram + "IBANID");
                IdentifierField accountIdentifier = iban is not null
                    ? values.ReadIdentifier(iban)
                    : values.ReadIdentifier(In(values, account, CiiNames.Ram + "ProprietaryID")) with
                    {
                        SchemeId = CreditTransferSchemes.Proprietary,
                    };

                payment.CreditTransfers.Add(new CreditTransfer
                {
                    AccountIdentifier = accountIdentifier,
                    AccountName = values.ReadText(In(values, account, CiiNames.Ram + "AccountName")),
                    ServiceProviderIdentifier = values.ReadIdentifier(
                        In(values, In(values, block, CiiNames.Ram + "PayeeSpecifiedCreditorFinancialInstitution"), CiiNames.Ram + "BICID")),
                });
            }
        }

        invoice.Payment = payment;
    }

    private static void ReadTotals(XElement? summation, DocumentTotals totals, CiiValueReader values)
    {
        if (summation is null)
        {
            return;
        }

        totals.LineTotalAmount = values.ReadAmount(In(values, summation, CiiNames.Ram + "LineTotalAmount"), "BT-106");
        totals.AllowanceTotalAmount = values.ReadAmount(In(values, summation, CiiNames.Ram + "AllowanceTotalAmount"), "BT-107");
        totals.ChargeTotalAmount = values.ReadAmount(In(values, summation, CiiNames.Ram + "ChargeTotalAmount"), "BT-108");
        totals.TaxExclusiveAmount = values.ReadAmount(In(values, summation, CiiNames.Ram + "TaxBasisTotalAmount"), "BT-109");
        List<XElement> taxTotals = AllIn(values, summation, CiiNames.Ram + "TaxTotalAmount");
        totals.TaxAmount = values.ReadAmount(taxTotals.FirstOrDefault(), "BT-110");

        // BT-111: the same tax, in the currency the seller accounts in, written as a second amount with its
        // own currencyID. Reading only the first left it unmapped.
        foreach (XElement extra in taxTotals.Skip(1))
        {
            totals.TaxAmountInAccountingCurrency = values.ReadAmount(extra, "BT-111");
        }
        totals.TaxInclusiveAmount = values.ReadAmount(In(values, summation, CiiNames.Ram + "GrandTotalAmount"), "BT-112");
        totals.PrepaidAmount = values.ReadAmount(In(values, summation, CiiNames.Ram + "TotalPrepaidAmount"), "BT-113");
        totals.RoundingAmount = values.ReadAmount(In(values, summation, CiiNames.Ram + "RoundingAmount"), "BT-114");
        totals.DuePayableAmount = values.ReadAmount(In(values, summation, CiiNames.Ram + "DuePayableAmount"), "BT-115");
    }

    private static InvoiceLine ReadLine(
        XElement element,
        CiiValueReader values,
        Dictionary<XElement, InvoiceNode> owners)
    {
        XElement? document = In(values, element, CiiNames.Ram + "AssociatedDocumentLineDocument");
        XElement? agreement = In(values, element, CiiNames.Ram + "SpecifiedLineTradeAgreement");
        XElement? delivery = In(values, element, CiiNames.Ram + "SpecifiedLineTradeDelivery");
        XElement? settlement = In(values, element, CiiNames.Ram + "SpecifiedLineTradeSettlement");

        var line = new InvoiceLine
        {
            Identifier = values.ReadIdentifier(In(values, document, CiiNames.Ram + "LineID")),
            Note = values.ReadText(In(values, In(values, document, CiiNames.Ram + "IncludedNote"), CiiNames.Ram + "Content")),
            ParentLineIdentifier = values.ReadIdentifier(In(values, document, CiiNames.Ram + "ParentLineID")),
            LineStatusCode = values.ReadCode(In(values, document, CiiNames.Ram + "LineStatusCode")),
            LineStatusReasonCode = values.ReadCode(In(values, document, CiiNames.Ram + "LineStatusReasonCode")),
            Quantity = values.ReadQuantity(In(values, delivery, CiiNames.Ram + "BilledQuantity"), "BT-129"),
            Item = ReadItem(In(values, element, CiiNames.Ram + "SpecifiedTradeProduct"), values, owners),
            Price = ReadPrice(agreement, values),
            Period = ReadPeriod(In(values, settlement, CiiNames.Ram + "BillingSpecifiedPeriod"), values),
        };

        if (agreement is not null)
        {
            line.OrderLineReference = values.ReadIdentifier(
                In(values, In(values, agreement, CiiNames.Ram + "BuyerOrderReferencedDocument"), CiiNames.Ram + "LineID"));

        }

        if (settlement is null)
        {
            return line;
        }

        // BT-128, which CII files with the line's settlement — the object the line is about, and the term a
        // utility invoice uses to say which meter it is billing.
        XElement? referenced = In(values, settlement, CiiNames.Ram + "AdditionalReferencedDocument");
        if (referenced is not null)
        {
            values.Consume(In(values, referenced, CiiNames.Ram + "TypeCode"));

            // BT-128-1. CII states the identifier's scheme in a sibling element rather than an attribute,
            // which is where UBL puts it — same term, two shapes, one field.
            IdentifierField scheme = values.ReadIdentifier(
                In(values, referenced, CiiNames.Ram + "ReferenceTypeCode"));

            line.ObjectIdentifier = values.ReadIdentifier(In(values, referenced, CiiNames.Ram + "IssuerAssignedID"))
                with
            { SchemeId = scheme.Value };
        }

        XElement? tax = In(values, settlement, CiiNames.Ram + "ApplicableTradeTax");
        line.VatCategoryCode = values.ReadCode(In(values, tax, CiiNames.Ram + "CategoryCode"));
        line.VatRate = values.ReadDecimal(In(values, tax, CiiNames.Ram + "RateApplicablePercent"), "BT-152");
        values.Consume(In(values, tax, CiiNames.Ram + "TypeCode"));

        line.NetAmount = values.ReadAmount(
            In(values, In(values, settlement, CiiNames.Ram + "SpecifiedTradeSettlementLineMonetarySummation"), CiiNames.Ram + "LineTotalAmount"),
            "BT-131");

        line.BuyerAccountingReference = values.ReadText(
            In(values, In(values, settlement, CiiNames.Ram + "ReceivableSpecifiedTradeAccountingAccount"), CiiNames.Ram + "ID"));

        foreach (XElement allowance in AllIn(values, settlement, CiiNames.Ram + "SpecifiedTradeAllowanceCharge"))
        {
            line.AllowancesAndCharges.Add(ReadAllowanceCharge(allowance, values));
        }

        return line;
    }

    private static LinePrice? ReadPrice(XElement? agreement, CiiValueReader values)
    {
        XElement? net = In(values, agreement, CiiNames.Ram + "NetPriceProductTradePrice");
        XElement? gross = In(values, agreement, CiiNames.Ram + "GrossPriceProductTradePrice");

        if (net is null && gross is null)
        {
            return null;
        }

        var price = new LinePrice
        {
            NetPrice = values.ReadAmount(In(values, net, CiiNames.Ram + "ChargeAmount"), "BT-146"),
            // BT-149 may be stated on both prices, and the norm says they agree. Reading one and ignoring the
            // other kept the loser as extension data, inside an element that allows no such child.
            BaseQuantity = values.ReadQuantity(
                In(values, net, CiiNames.Ram + "BasisQuantity") ?? In(values, gross, CiiNames.Ram + "BasisQuantity"),
                "BT-149"),
            GrossPrice = values.ReadAmount(In(values, gross, CiiNames.Ram + "ChargeAmount"), "BT-148"),
        };

        values.Consume(In(values, gross, CiiNames.Ram + "BasisQuantity"));

        XElement? discount = In(values, gross, CiiNames.Ram + "AppliedTradeAllowanceCharge");
        price.Discount = values.ReadAmount(In(values, discount, CiiNames.Ram + "ActualAmount"), "BT-147");
        values.Consume(In(values, discount, CiiNames.Ram + "ChargeIndicator"));
        values.Consume(In(values, In(values, discount, CiiNames.Ram + "ChargeIndicator"), CiiNames.Udt + "Indicator"));
        return price;
    }

    private static Item? ReadItem(
        XElement? element,
        CiiValueReader values,
        Dictionary<XElement, InvoiceNode> owners)
    {
        if (element is null)
        {
            return null;
        }

        var item = new Item
        {
            Name = values.ReadText(In(values, element, CiiNames.Ram + "Name")),
            Description = values.ReadText(In(values, element, CiiNames.Ram + "Description")),
            SellerIdentifier = values.ReadIdentifier(In(values, element, CiiNames.Ram + "SellerAssignedID")),
            BuyerIdentifier = values.ReadIdentifier(In(values, element, CiiNames.Ram + "BuyerAssignedID")),
            StandardIdentifier = values.ReadIdentifier(In(values, element, CiiNames.Ram + "GlobalID")),
            OriginCountryCode = values.ReadCode(
                In(values, In(values, element, CiiNames.Ram + "OriginTradeCountry"), CiiNames.Ram + "ID")),
        };

        foreach (XElement classification in AllIn(values, element, CiiNames.Ram + "DesignatedProductClassification"))
        {
            item.Classifications.Add(new ItemClassification
            {
                Code = values.ReadCode(In(values, classification, CiiNames.Ram + "ClassCode")),
                Name = values.ReadText(In(values, classification, CiiNames.Ram + "ClassName")),
            });
        }

        foreach (XElement characteristic in AllIn(values, element, CiiNames.Ram + "ApplicableProductCharacteristic"))
        {
            item.Characteristics.Add(new ItemCharacteristic
            {
                Name = values.ReadText(In(values, characteristic, CiiNames.Ram + "Description")),
                Value = values.ReadText(In(values, characteristic, CiiNames.Ram + "Value")),
            });
        }

        owners[element] = item;
        return item;
    }

    private static Party? ReadParty(XElement? element, CiiValueReader values)
    {
        if (element is null)
        {
            return null;
        }

        var party = new Party
        {
            Name = values.ReadText(In(values, element, CiiNames.Ram + "Name")),
            AdditionalLegalInformation = values.ReadText(In(values, element, CiiNames.Ram + "Description")),
            ElectronicAddress = values.ReadIdentifier(
                In(values, In(values, element, CiiNames.Ram + "URIUniversalCommunication"), CiiNames.Ram + "URIID")),
            Address = ReadAddress(In(values, element, CiiNames.Ram + "PostalTradeAddress"), values),
            Contact = ReadContact(In(values, element, CiiNames.Ram + "DefinedTradeContact"), values),
        };

        foreach (XElement identifier in AllIn(values, element, CiiNames.Ram + "ID"))
        {
            party.Identifiers.Add(values.ReadIdentifier(identifier));
        }

        foreach (XElement identifier in AllIn(values, element, CiiNames.Ram + "GlobalID"))
        {
            party.Identifiers.Add(values.ReadIdentifier(identifier));
        }

        XElement? legal = In(values, element, CiiNames.Ram + "SpecifiedLegalOrganization");
        if (legal is not null)
        {
            party.LegalRegistrationIdentifier = values.ReadIdentifier(In(values, legal, CiiNames.Ram + "ID"));
            party.TradingName = values.ReadText(In(values, legal, CiiNames.Ram + "TradingBusinessName"));
        }

        foreach (XElement registration in AllIn(values, element, CiiNames.Ram + "SpecifiedTaxRegistration"))
        {
            XElement? id = In(values, registration, CiiNames.Ram + "ID");
            IdentifierField field = values.ReadIdentifier(id);

            if (string.Equals(field.SchemeId, "VA", StringComparison.OrdinalIgnoreCase))
            {
                party.VatIdentifier = field;
            }
            else
            {
                party.TaxRegistrationIdentifier = field;
            }
        }

        return party;
    }

    private static PostalAddress? ReadAddress(XElement? element, CiiValueReader values) =>
        element is null
            ? null
            : new PostalAddress
            {
                Line1 = values.ReadText(In(values, element, CiiNames.Ram + "LineOne")),
                Line2 = values.ReadText(In(values, element, CiiNames.Ram + "LineTwo")),
                Line3 = values.ReadText(In(values, element, CiiNames.Ram + "LineThree")),
                City = values.ReadText(In(values, element, CiiNames.Ram + "CityName")),
                PostCode = values.ReadText(In(values, element, CiiNames.Ram + "PostcodeCode")),
                CountrySubdivision = values.ReadText(In(values, element, CiiNames.Ram + "CountrySubDivisionName")),
                CountryCode = values.ReadCode(In(values, element, CiiNames.Ram + "CountryID")),
            };

    private static Contact? ReadContact(XElement? element, CiiValueReader values) =>
        element is null
            ? null
            : new Contact
            {
                Name = values.ReadText(In(values, element, CiiNames.Ram + "PersonName")),
                Telephone = values.ReadText(
                    In(values, In(values, element, CiiNames.Ram + "TelephoneUniversalCommunication"), CiiNames.Ram + "CompleteNumber")),
                Email = values.ReadText(
                    In(values, In(values, element, CiiNames.Ram + "EmailURIUniversalCommunication"), CiiNames.Ram + "URIID")),
            };

    private static InvoicingPeriod? ReadPeriod(XElement? element, CiiValueReader values) =>
        element is null
            ? null
            : new InvoicingPeriod
            {
                StartDate = values.ReadDate(In(values, element, CiiNames.Ram + "StartDateTime"), "BT-73"),
                EndDate = values.ReadDate(In(values, element, CiiNames.Ram + "EndDateTime"), "BT-74"),
            };

    private static AllowanceCharge ReadAllowanceCharge(XElement element, CiiValueReader values)
    {
        XElement? indicator = In(values, element, CiiNames.Ram + "ChargeIndicator");

        var allowanceCharge = new AllowanceCharge
        {
            IsCharge = values.ReadIndicator(indicator).Value ?? false,
            Amount = values.ReadAmount(In(values, element, CiiNames.Ram + "ActualAmount")),
            BaseAmount = values.ReadAmount(In(values, element, CiiNames.Ram + "BasisAmount")),
            Percentage = values.ReadDecimal(In(values, element, CiiNames.Ram + "CalculationPercent")),
            Reason = values.ReadText(In(values, element, CiiNames.Ram + "Reason")),
            ReasonCode = values.ReadCode(In(values, element, CiiNames.Ram + "ReasonCode")),
            VatCategoryCode = values.ReadCode(
                In(values, In(values, element, CiiNames.Ram + "CategoryTradeTax"), CiiNames.Ram + "CategoryCode")),
            VatRate = values.ReadDecimal(
                In(values, In(values, element, CiiNames.Ram + "CategoryTradeTax"), CiiNames.Ram + "RateApplicablePercent")),
        };

        // The scheme is always VAT and is written back from nothing, so it has to be marked as read or it
        // returns as extension data — inside an element that allows no such child.
        values.Consume(In(values, In(values, element, CiiNames.Ram + "CategoryTradeTax"), CiiNames.Ram + "TypeCode"));

        return allowanceCharge;
    }

    /// <summary>The code CII gives a referenced document that identifies what the invoice is about (BT-18).</summary>
    private const string InvoicedObjectTypeCode = "130";

    /// <summary>The code it gives a supporting document (BT-122 to BT-125), which the writer states itself.</summary>
    private const string SupportingDocumentTypeCode = "916";

    private static AdditionalDocument ReadAdditionalDocument(XElement element, CiiValueReader values)
    {
        XElement? binary = In(values, element, CiiNames.Ram + "AttachmentBinaryObject");

        var document = new AdditionalDocument
        {
            Identifier = values.ReadIdentifier(In(values, element, CiiNames.Ram + "IssuerAssignedID")),
            Description = values.ReadText(In(values, element, CiiNames.Ram + "Name")),
            ExternalLocation = values.ReadText(In(values, element, CiiNames.Ram + "URIID")),
        };

        if (binary is not null)
        {
            document.Attachment = ReadBinary(binary, values);
            values.Consume(binary);
        }

        // 916 says "a supporting document", which is what this is, and the writer states it from nothing.
        // Anything else is kept as extension data, where the writer puts it back in the right place.
        XElement? typeCode = In(values, element, CiiNames.Ram + "TypeCode");
        if (typeCode?.Value.Trim() != SupportingDocumentTypeCode)
        {
            values.Release(typeCode);
        }

        return document;
    }

    private static BinaryField ReadBinary(XElement element, CiiValueReader values)
    {
        string mimeCode = element.Attribute("mimeCode")?.Value ?? string.Empty;
        string filename = element.Attribute("filename")?.Value ?? string.Empty;
        var source = new FieldSource(element.Value, CiiValueReader.LocationOf(element));

        return Limits.Decode(element.Value, values.Limits, values.Diagnostics) is { } decoded
            ? new BinaryField(decoded, mimeCode, filename, source)
            : new BinaryField(null, mimeCode, filename, source);
    }

    private static IdentifierField ReadReferencedDocument(XElement parent, string name, CiiValueReader values) =>
        values.ReadIdentifier(In(values, In(values, parent, CiiNames.Ram + name), CiiNames.Ram + "IssuerAssignedID"));

    /// <summary>Finds a child element and marks it mapped, so it is not later kept as extension data.</summary>
    private static XElement? In(CiiValueReader values, XElement? parent, XName name)
    {
        XElement? child = parent?.Element(name);
        values.Consume(child);
        return child;
    }

    private static List<XElement> AllIn(CiiValueReader values, XElement? parent, XName name)
    {
        List<XElement> children = [.. parent?.Elements(name) ?? []];
        foreach (XElement child in children)
        {
            values.Consume(child);
        }

        return children;
    }

    /// <summary>
    /// Walks the whole document and gives every element the reader did not map to the invoice. Doing this once
    /// at the end is what makes the guarantee total: an element nobody thought about is still kept.
    /// </summary>
    private static void KeepEverythingElse(
        XElement source,
        InvoiceNode node,
        HashSet<XElement> mapped,
        IReadOnlyDictionary<XElement, InvoiceNode> owners,
        DiagnosticCollector diagnostics)
    {
        foreach (XElement element in source.Elements())
        {
            if (mapped.Contains(element))
            {
                // Descend with the node that owns this element, when one exists, so what it contains is kept
                // where it belongs and can be written back inside it.
                KeepEverythingElse(
                    element,
                    owners.TryGetValue(element, out InvoiceNode? owner) ? owner : node,
                    mapped,
                    owners,
                    diagnostics);
                continue;
            }

            node.Extensions.Add(new ExtensionElement(
                element.Name.NamespaceName,
                element.Name.LocalName,
                element.ToString(SaveOptions.DisableFormatting),
                CiiValueReader.LocationOf(element)));

            diagnostics.Add(Diagnostic.Create(CiiDiagnostics.UnmappedElement, element.Name.LocalName) with
            {
                Location = CiiValueReader.LocationOf(element),
                Found = element.Name.LocalName,
                AppliedFallback = "kept verbatim as extension data",
            });
        }
    }
}
