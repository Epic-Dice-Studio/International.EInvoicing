using System.Xml.Linq;
using International.EInvoicing.Cii;
using International.EInvoicing.Cii.Reading;
using International.EInvoicing.Configuration;
using International.EInvoicing.Diagnostics;
using International.EInvoicing.Documents;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;
using International.EInvoicing.Values;
using International.EInvoicing.Xml;

namespace International.EInvoicing.Zugferd1.Reading;

/// <summary>
/// Reads a ZUGFeRD 1.0 invoice into the canonical model.
/// </summary>
/// <remarks>
/// <para>
/// Reading only. FeRD replaced this format in 2019 and nothing should be written in it again; what an
/// archive of them needs is a way forward, and once a document is in the model
/// <c>EInvoicing.Convert</c> writes it as ZUGFeRD 2, Factur-X, CII or UBL.
/// </para>
/// <para>
/// Reading never throws on the document: a value that cannot be typed keeps its raw text, an element nobody
/// mapped is kept verbatim where it sat, and everything the reader had to give up is reported.
/// </para>
/// </remarks>
public sealed class Zugferd1InvoiceReader : IDocumentReader<EInvoice>
{
    private readonly EInvoicingOptions _options;
    private readonly IProfileResolver _profiles;

    /// <summary>Creates a reader using the supplied options and profile resolver.</summary>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    public Zugferd1InvoiceReader(EInvoicingOptions options, IProfileResolver profiles)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(profiles);

        _options = options;
        _profiles = profiles;
    }

    /// <inheritdoc />
    public DocumentSyntax Syntax => DocumentSyntax.Zugferd1;

    private static XNamespace Ram => Zugferd1Names.Ram;

    private static XNamespace Rsm => Zugferd1Names.Rsm;

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
            diagnostics.Add(Diagnostic.Create(Zugferd1Diagnostics.MalformedDocument, exception.Message) with
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
        ReadHeader(root, invoice, values);

        XElement? transaction = In(values, root, Rsm + "SpecifiedSupplyChainTradeTransaction");

        foreach (XElement line in AllIn(values, transaction, Ram + "IncludedSupplyChainTradeLineItem"))
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

        ReadAgreement(In(values, transaction, Ram + "ApplicableSupplyChainTradeAgreement"), invoice, values, owners);
        ReadDelivery(In(values, transaction, Ram + "ApplicableSupplyChainTradeDelivery"), invoice, values, owners);
        ReadSettlement(In(values, transaction, Ram + "ApplicableSupplyChainTradeSettlement"), invoice, values, owners);

        Zugferd1Extensions.KeepEverythingElse(root, invoice, mapped, owners, diagnostics);

        ProfileResolution resolution = _profiles.Resolve(invoice.SpecificationIdentifier, DocumentSyntax.Zugferd1);
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
        XElement? context = In(values, root, Rsm + "SpecifiedExchangedDocumentContext");

        invoice.BusinessProcessType = values.ReadIdentifier(
            In(values, In(values, context, Ram + "BusinessProcessSpecifiedDocumentContextParameter"), Ram + "ID"));
        invoice.SpecificationIdentifier = ProfileIdentifier.FromDocument(
            In(values, In(values, context, Ram + "GuidelineSpecifiedDocumentContextParameter"), Ram + "ID")?.Value);

        // ZUGFeRD 1.0 states whether the document is a test at the top, and EN 16931 has no term for it. It
        // is read so that it is not reported as unmapped, and kept as extension data so it is not lost.
        values.Release(In(values, context, Ram + "TestIndicator"));
    }

    private static void ReadHeader(XElement root, EInvoice invoice, CiiValueReader values)
    {
        XElement? document = In(values, root, Rsm + "HeaderExchangedDocument");

        invoice.Number = values.ReadIdentifier(In(values, document, Ram + "ID"));
        invoice.TypeCode = values.ReadCode(In(values, document, Ram + "TypeCode"));
        invoice.IssueDate = values.ReadDate(In(values, document, Ram + "IssueDateTime"), "BT-2");

        foreach (XElement note in AllIn(values, document, Ram + "IncludedNote"))
        {
            invoice.Notes.Add(new InvoiceNote
            {
                Text = values.ReadText(In(values, note, Ram + "Content")),
                SubjectCode = values.ReadCode(In(values, note, Ram + "SubjectCode")),
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

        invoice.BuyerReference = values.ReadText(In(values, agreement, Ram + "BuyerReference"));
        invoice.Seller = Zugferd1Parties.ReadParty(In(values, agreement, Ram + "SellerTradeParty"), values, owners);
        invoice.Buyer = Zugferd1Parties.ReadParty(In(values, agreement, Ram + "BuyerTradeParty"), values, owners);
        invoice.PurchaseOrderReference = Reference(agreement, "BuyerOrderReferencedDocument", values);
        invoice.SalesOrderReference = Reference(agreement, "SellerOrderReferencedDocument", values);
        invoice.ContractReference = Reference(agreement, "ContractReferencedDocument", values);

        foreach (XElement document in AllIn(values, agreement, Ram + "AdditionalReferencedDocument"))
        {
            if (Limits.Exceeded(invoice.AdditionalDocuments.Count, values.Limits.MaxAttachmentCount))
            {
                values.Diagnostics.Add(Limits.TooMany(values.Limits.MaxAttachmentCount, "attachments"));
                break;
            }

            invoice.AdditionalDocuments.Add(Zugferd1Parties.ReadDocument(document, values, owners));
        }
    }

    private static void ReadDelivery(
        XElement? delivery,
        EInvoice invoice,
        CiiValueReader values,
        Dictionary<XElement, InvoiceNode> owners)
    {
        if (delivery is null)
        {
            return;
        }

        Party? recipient = Zugferd1Parties.ReadParty(In(values, delivery, Ram + "ShipToTradeParty"), values, owners);
        DateField delivered = values.ReadDate(
            In(values, In(values, delivery, Ram + "ActualDeliverySupplyChainEvent"), Ram + "OccurrenceDateTime"),
            "BT-72");
        IdentifierField despatchAdvice = Reference(delivery, "DespatchAdviceReferencedDocument", values);
        IdentifierField receivingAdvice = Reference(delivery, "ReceivingAdviceReferencedDocument", values);

        // BT-16 in ZUGFeRD 1.0 is the delivery note, which is the despatch advice under an older name.
        IdentifierField deliveryNote = Reference(delivery, "DeliveryNoteReferencedDocument", values);

        invoice.DespatchAdviceReference = despatchAdvice.IsSet ? despatchAdvice : deliveryNote;
        invoice.ReceivingAdviceReference = receivingAdvice;

        if (recipient is null && !delivered.IsSet)
        {
            return;
        }

        invoice.Delivery = new DeliveryInformation
        {
            ActualDeliveryDate = delivered,
            Name = recipient?.Name ?? TextField.Unset,
            Address = recipient?.Address,
        };

        owners[delivery] = invoice.Delivery;
    }

    private static void ReadSettlement(
        XElement? settlement,
        EInvoice invoice,
        CiiValueReader values,
        Dictionary<XElement, InvoiceNode> owners)
    {
        if (settlement is null)
        {
            return;
        }

        invoice.CurrencyCode = values.ReadCode(In(values, settlement, Ram + "InvoiceCurrencyCode"));
        invoice.TaxAccountingCurrencyCode = values.ReadCode(In(values, settlement, Ram + "TaxCurrencyCode"));
        invoice.Payee = Zugferd1Parties.ReadParty(In(values, settlement, Ram + "PayeeTradeParty"), values, owners);
        invoice.Period = Zugferd1Parties.ReadPeriod(In(values, settlement, Ram + "BillingSpecifiedPeriod"), values);
        invoice.BuyerAccountingReference = values.ReadText(
            In(values, In(values, settlement, Ram + "ReceivableSpecifiedTradeAccountingAccount"), Ram + "ID"));

        ReadPaymentMeans(settlement, invoice, values, owners);

        foreach (XElement tax in AllIn(values, settlement, Ram + "ApplicableTradeTax"))
        {
            invoice.VatBreakdown.Add(new VatBreakdownEntry
            {
                TaxAmount = values.ReadAmount(In(values, tax, Ram + "CalculatedAmount")),
                TaxableAmount = values.ReadAmount(In(values, tax, Ram + "BasisAmount")),
                CategoryCode = values.ReadCode(In(values, tax, Ram + "CategoryCode")),
                Rate = values.ReadDecimal(In(values, tax, Ram + "ApplicablePercent")),
                ExemptionReason = values.ReadText(In(values, tax, Ram + "ExemptionReason")),
            });

            values.Consume(tax.Element(Ram + "TypeCode"));
        }

        foreach (XElement allowanceCharge in AllIn(values, settlement, Ram + "SpecifiedTradeAllowanceCharge"))
        {
            invoice.AllowancesAndCharges.Add(
                Zugferd1Parties.ReadAllowanceCharge(allowanceCharge, values, owners));
        }

        // A freight charge, which ZUGFeRD 1.0 states apart and EN 16931 folds into the charges. It is one
        // either way, so it is read as one rather than kept as something a receiver has to know about.
        foreach (XElement service in AllIn(values, settlement, Ram + "SpecifiedLogisticsServiceCharge"))
        {
            XElement? tax = In(values, service, Ram + "AppliedTradeTax");
            var charge = new AllowanceCharge
            {
                IsCharge = true,
                Reason = values.ReadText(In(values, service, Ram + "Description")),
                Amount = values.ReadAmount(In(values, service, Ram + "AppliedAmount")),
                VatCategoryCode = values.ReadCode(In(values, tax, Ram + "CategoryCode")),
                VatRate = values.ReadDecimal(In(values, tax, Ram + "ApplicablePercent")),
            };

            values.Consume(tax?.Element(Ram + "TypeCode"));
            owners[service] = charge;
            invoice.AllowancesAndCharges.Add(charge);
        }

        ReadPaymentTerms(settlement, invoice, values, owners);
        ReadTotals(In(values, settlement, Ram + "SpecifiedTradeSettlementMonetarySummation"), invoice, values, owners);
    }

    private static void ReadPaymentTerms(
        XElement settlement,
        EInvoice invoice,
        CiiValueReader values,
        Dictionary<XElement, InvoiceNode> owners)
    {
        List<XElement> terms = AllIn(values, settlement, Ram + "SpecifiedTradePaymentTerms");
        if (terms.Count == 0)
        {
            return;
        }

        invoice.PaymentTerms = values.ReadText(In(values, terms[0], Ram + "Description"));
        invoice.DueDate = values.ReadDate(In(values, terms[0], Ram + "DueDateDateTime"), "BT-9");

        foreach (XElement term in terms)
        {
            owners[term] = invoice;
        }
    }

    private static void ReadPaymentMeans(
        XElement settlement,
        EInvoice invoice,
        CiiValueReader values,
        Dictionary<XElement, InvoiceNode> owners)
    {
        invoice.Payment = new PaymentInstructions
        {
            RemittanceInformation = values.ReadText(In(values, settlement, Ram + "PaymentReference")),
        };

        foreach (XElement means in AllIn(values, settlement, Ram + "SpecifiedTradeSettlementPaymentMeans"))
        {
            if (!invoice.Payment.MeansTypeCode.IsSet)
            {
                invoice.Payment.MeansTypeCode = values.ReadCode(In(values, means, Ram + "TypeCode"));
                invoice.Payment.MeansText = values.ReadText(In(values, means, Ram + "Information"));
            }

            XElement? account = In(values, means, Ram + "PayeePartyCreditorFinancialAccount");
            XElement? institution = In(values, means, Ram + "PayeeSpecifiedCreditorFinancialInstitution");

            if (account is null && institution is null)
            {
                owners[means] = invoice.Payment;
                continue;
            }

            IdentifierField iban = values.ReadIdentifier(In(values, account, Ram + "IBANID"));
            IdentifierField proprietary = values.ReadIdentifier(In(values, account, Ram + "ProprietaryID"));

            var transfer = new CreditTransfer
            {
                AccountIdentifier = iban.IsSet
                    ? iban
                    : proprietary.IsSet ? proprietary with { SchemeId = CreditTransferSchemes.Proprietary } : default,
                AccountName = values.ReadText(In(values, account, Ram + "AccountName")),
                ServiceProviderIdentifier = values.ReadIdentifier(In(values, institution, Ram + "BICID")),
            };

            owners[means] = transfer;
            invoice.Payment.CreditTransfers.Add(transfer);
        }
    }

    private static void ReadTotals(
        XElement? element,
        EInvoice invoice,
        CiiValueReader values,
        Dictionary<XElement, InvoiceNode> owners)
    {
        if (element is null)
        {
            return;
        }

        invoice.Totals.LineTotalAmount = values.ReadAmount(In(values, element, Ram + "LineTotalAmount"));
        invoice.Totals.ChargeTotalAmount = values.ReadAmount(In(values, element, Ram + "ChargeTotalAmount"));
        invoice.Totals.AllowanceTotalAmount = values.ReadAmount(In(values, element, Ram + "AllowanceTotalAmount"));
        invoice.Totals.TaxExclusiveAmount = values.ReadAmount(In(values, element, Ram + "TaxBasisTotalAmount"));
        invoice.Totals.TaxAmount = values.ReadAmount(In(values, element, Ram + "TaxTotalAmount"));
        invoice.Totals.TaxInclusiveAmount = values.ReadAmount(In(values, element, Ram + "GrandTotalAmount"));
        invoice.Totals.PrepaidAmount = values.ReadAmount(In(values, element, Ram + "TotalPrepaidAmount"));
        invoice.Totals.RoundingAmount = values.ReadAmount(In(values, element, Ram + "RoundingAmount"));
        invoice.Totals.DuePayableAmount = values.ReadAmount(In(values, element, Ram + "DuePayableAmount"));

        owners[element] = invoice.Totals;
    }

    private static InvoiceLine ReadLine(
        XElement element,
        CiiValueReader values,
        Dictionary<XElement, InvoiceNode> owners)
    {
        var line = new InvoiceLine();

        if (In(values, element, Ram + "AssociatedDocumentLineDocument") is { } document)
        {
            line.Identifier = values.ReadIdentifier(In(values, document, Ram + "LineID"));

            // EN 16931 has one note per line (BT-127); a 2013 document may carry several, and the rest are
            // kept where they sat rather than folded into the first.
            List<XElement> notes = AllIn(values, document, Ram + "IncludedNote");
            if (notes.Count > 0)
            {
                line.Note = values.ReadText(In(values, notes[0], Ram + "Content"));
                values.Consume(notes[0].Element(Ram + "SubjectCode"));
            }

            for (var index = 1; index < notes.Count; index++)
            {
                values.Release(notes[index]);
            }
        }

        line.Item = ReadItem(In(values, element, Ram + "SpecifiedTradeProduct"), values, owners);
        ReadLineAgreement(In(values, element, Ram + "SpecifiedSupplyChainTradeAgreement"), line, values, owners);

        if (In(values, element, Ram + "SpecifiedSupplyChainTradeDelivery") is { } delivery)
        {
            line.Quantity = values.ReadQuantity(In(values, delivery, Ram + "BilledQuantity"), "BT-129");
            owners[delivery] = line;
        }

        ReadLineSettlement(In(values, element, Ram + "SpecifiedSupplyChainTradeSettlement"), line, values, owners);

        return line;
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
            Name = values.ReadText(In(values, element, Ram + "Name")),
            Description = values.ReadText(In(values, element, Ram + "Description")),
            StandardIdentifier = values.ReadIdentifier(In(values, element, Ram + "GlobalID")),
            SellerIdentifier = values.ReadIdentifier(In(values, element, Ram + "SellerAssignedID")),
            BuyerIdentifier = values.ReadIdentifier(In(values, element, Ram + "BuyerAssignedID")),
            OriginCountryCode = values.ReadCode(
                In(values, In(values, element, Ram + "OriginTradeCountry"), Ram + "ID")),
        };

        foreach (XElement characteristic in AllIn(values, element, Ram + "ApplicableProductCharacteristic"))
        {
            item.Characteristics.Add(new ItemCharacteristic
            {
                Name = values.ReadText(In(values, characteristic, Ram + "Description")),
                Value = values.ReadText(In(values, characteristic, Ram + "Value")),
            });
        }

        owners[element] = item;
        return item;
    }

    private static void ReadLineAgreement(
        XElement? agreement,
        InvoiceLine line,
        CiiValueReader values,
        Dictionary<XElement, InvoiceNode> owners)
    {
        if (agreement is null)
        {
            return;
        }

        line.OrderLineReference = values.ReadIdentifier(
            In(values, In(values, agreement, Ram + "BuyerOrderReferencedDocument"), Ram + "LineID"));

        XElement? gross = In(values, agreement, Ram + "GrossPriceProductTradePrice");
        XElement? net = In(values, agreement, Ram + "NetPriceProductTradePrice");

        if (gross is null && net is null)
        {
            return;
        }

        var price = new LinePrice
        {
            GrossPrice = values.ReadAmount(In(values, gross, Ram + "ChargeAmount")),
            NetPrice = values.ReadAmount(In(values, net, Ram + "ChargeAmount")),
            BaseQuantity = values.ReadQuantity(In(values, net ?? gross, Ram + "BasisQuantity")),
        };

        foreach (XElement applied in AllIn(values, gross, Ram + "AppliedTradeAllowanceCharge"))
        {
            price.Adjustments.Add(Zugferd1Parties.ReadAllowanceCharge(applied, values, owners));
        }

        decimal reduction = price.Adjustments
            .Where(adjustment => !adjustment.IsCharge)
            .Sum(adjustment => adjustment.Amount.Value ?? 0m);

        if (reduction != 0m)
        {
            price.Discount = new AmountField(reduction);
        }

        // Both prices state the same basis quantity, and the model keeps one.
        if (net is not null && gross is not null)
        {
            values.Consume(gross.Element(Ram + "BasisQuantity"));
        }

        line.Price = price;

        if (net is not null)
        {
            owners[net] = price;
        }
    }

    private static void ReadLineSettlement(
        XElement? settlement,
        InvoiceLine line,
        CiiValueReader values,
        Dictionary<XElement, InvoiceNode> owners)
    {
        if (settlement is null)
        {
            return;
        }

        if (In(values, settlement, Ram + "ApplicableTradeTax") is { } tax)
        {
            line.VatCategoryCode = values.ReadCode(In(values, tax, Ram + "CategoryCode"));
            line.VatRate = values.ReadDecimal(In(values, tax, Ram + "ApplicablePercent"));
            values.Consume(tax.Element(Ram + "TypeCode"));
        }

        foreach (XElement allowanceCharge in AllIn(values, settlement, Ram + "SpecifiedTradeAllowanceCharge"))
        {
            line.AllowancesAndCharges.Add(Zugferd1Parties.ReadAllowanceCharge(allowanceCharge, values, owners));
        }

        line.Period = Zugferd1Parties.ReadPeriod(In(values, settlement, Ram + "BillingSpecifiedPeriod"), values);
        line.NetAmount = values.ReadAmount(
            In(
                values,
                In(values, settlement, Ram + "SpecifiedTradeSettlementMonetarySummation"),
                Ram + "LineTotalAmount"));
        line.BuyerAccountingReference = values.ReadText(
            In(values, In(values, settlement, Ram + "ReceivableSpecifiedTradeAccountingAccount"), Ram + "ID"));
    }

    private static IdentifierField Reference(XElement? parent, string localName, CiiValueReader values)
    {
        XElement? document = In(values, parent, Ram + localName);

        // ZUGFeRD 1.0 puts the identifier in ram:ID; the later CII renamed it IssuerAssignedID.
        IdentifierField identifier = values.ReadIdentifier(In(values, document, Ram + "ID"));

        values.Consume(document?.Element(Ram + "IssueDateTime"));
        return identifier;
    }

    internal static XElement? In(CiiValueReader values, XElement? parent, XName name)
    {
        XElement? child = parent?.Element(name);
        values.Consume(child);
        return child;
    }

    internal static List<XElement> AllIn(CiiValueReader values, XElement? parent, XName name)
    {
        List<XElement> children = [.. parent?.Elements(name) ?? []];
        foreach (XElement child in children)
        {
            values.Consume(child);
        }

        return children;
    }
}
