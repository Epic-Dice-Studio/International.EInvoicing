using System.Xml.Linq;
using International.EInvoicing.Configuration;
using International.EInvoicing.Diagnostics;
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
public sealed class UblInvoiceReader
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

    /// <summary>The syntax this reader understands.</summary>
    public static DocumentSyntax Syntax => DocumentSyntax.Ubl;

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

    private EInvoice ReadInvoice(XElement root, DiagnosticCollector diagnostics)
    {
        // A credit note is the same document under another root, with three elements renamed.
        UblDocumentShape shape = UblDocumentShape.Of(root);
        var mapped = new HashSet<XElement>();
        var owners = new Dictionary<XElement, InvoiceNode>();
        var values = new UblValueReader(diagnostics, mapped);
        var invoice = new EInvoice();

        invoice.SpecificationIdentifier =
            ProfileIdentifier.FromDocument(Take(root, UblNames.Cbc + "CustomizationID", mapped)?.Value);
        invoice.BusinessProcessType = values.ReadIdentifier(Take(root, UblNames.Cbc + "ProfileID", mapped));
        invoice.Number = values.ReadIdentifier(Take(root, UblNames.Cbc + "ID", mapped));
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
            invoice.Notes.Add(new InvoiceNote { Text = values.ReadText(note) });
        }

        invoice.Period = ReadPeriod(Take(root, UblNames.Cac + "InvoicePeriod", mapped), values);
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

        invoice.ContractReference = values.ReadIdentifier(
            Take(root, UblNames.Cac + "ContractDocumentReference", mapped)?.Element(UblNames.Cbc + "ID"));
        invoice.ProjectReference = values.ReadIdentifier(
            Take(root, UblNames.Cac + "ProjectReference", mapped)?.Element(UblNames.Cbc + "ID"));

        foreach (XElement document in TakeAll(root, UblNames.Cac + "AdditionalDocumentReference", mapped))
        {
            AdditionalDocument mappedDocument = ReadAdditionalDocument(document, values);
            owners[document] = mappedDocument;
            invoice.AdditionalDocuments.Add(mappedDocument);
        }

        invoice.Seller = ReadParty(
            Descend(values, Take(root, UblNames.Cac + "AccountingSupplierParty", mapped), UblNames.Cac + "Party"), values);
        invoice.Buyer = ReadParty(
            Descend(values, Take(root, UblNames.Cac + "AccountingCustomerParty", mapped), UblNames.Cac + "Party"), values);
        invoice.Payee = ReadParty(Take(root, UblNames.Cac + "PayeeParty", mapped), values);
        invoice.SellerTaxRepresentative = ReadParty(
            Take(root, UblNames.Cac + "TaxRepresentativeParty", mapped), values);

        invoice.Delivery = ReadDelivery(Take(root, UblNames.Cac + "Delivery", mapped), values);
        invoice.Payment = ReadPayment(root, values, mapped);
        invoice.PaymentTerms = values.ReadText(
            Descend(values, Take(root, UblNames.Cac + "PaymentTerms", mapped), UblNames.Cbc + "Note"));

        foreach (XElement allowance in TakeAll(root, UblNames.Cac + "AllowanceCharge", mapped))
        {
            invoice.AllowancesAndCharges.Add(ReadAllowanceCharge(allowance, values));
        }

        ReadTaxTotal(Take(root, UblNames.Cac + "TaxTotal", mapped), invoice, values);
        ReadTotals(Take(root, UblNames.Cac + "LegalMonetaryTotal", mapped), invoice.Totals, values);

        foreach (XElement line in TakeAll(root, shape.Line, mapped))
        {
            InvoiceLine mappedLine = ReadLine(shape, line, values, owners);
            owners[line] = mappedLine;
            invoice.Lines.Add(mappedLine);
        }

        KeepEverythingElse(root, invoice, mapped, owners, diagnostics);

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

    private static AdditionalDocument ReadAdditionalDocument(XElement element, UblValueReader values)
    {
        XElement? attachment = Descend(values, element, UblNames.Cac + "Attachment");
        XElement? embedded = attachment?.Element(UblNames.Cbc + "EmbeddedDocumentBinaryObject");
        XElement? external = Descend(values, Descend(values, attachment, UblNames.Cac + "ExternalReference"), UblNames.Cbc + "URI");

        var document = new AdditionalDocument
        {
            Identifier = values.ReadIdentifier(element.Element(UblNames.Cbc + "ID")),
            Description = values.ReadText(element.Element(UblNames.Cbc + "DocumentDescription")),
            ExternalLocation = values.ReadText(external),
        };

        if (embedded is not null)
        {
            document.Attachment = ReadBinary(embedded, values);
        }

        return document;
    }

    private static BinaryField ReadBinary(XElement element, UblValueReader values)
    {
        string mimeCode = element.Attribute("mimeCode")?.Value ?? string.Empty;
        string filename = element.Attribute("filename")?.Value ?? string.Empty;
        var source = new FieldSource(element.Value, UblValueReader.LocationOf(element));

        return Convert.TryFromBase64String(element.Value, new byte[element.Value.Length], out _)
            ? new BinaryField(Convert.FromBase64String(element.Value), mimeCode, filename, source)
            : new BinaryField(null, mimeCode, filename, source);
    }

    private static Party? ReadParty(XElement? element, UblValueReader values)
    {
        if (element is null)
        {
            return null;
        }

        // BT-28 is the name a party trades under; BT-27, its legal name, lives in the legal entity below.
        var party = new Party
        {
            TradingName = values.ReadText(
                Descend(values, Descend(values, element, UblNames.Cac + "PartyName"), UblNames.Cbc + "Name")),
            ElectronicAddress = values.ReadIdentifier(element.Element(UblNames.Cbc + "EndpointID")),
        };

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

        foreach (XElement taxScheme in DescendAll(values, element, UblNames.Cac + "PartyTaxScheme"))
        {
            string? scheme = Descend(values, Descend(values, taxScheme, UblNames.Cac + "TaxScheme"), UblNames.Cbc + "ID")?.Value;
            values.Consume(Descend(values, taxScheme, UblNames.Cac + "TaxScheme"));
            IdentifierField companyId = values.ReadIdentifier(taxScheme.Element(UblNames.Cbc + "CompanyID"));

            if (string.Equals(scheme, "VAT", StringComparison.OrdinalIgnoreCase))
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
            : new PostalAddress
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
            };

    private static Contact? ReadContact(XElement? element, UblValueReader values) =>
        element is null
            ? null
            : new Contact
            {
                Name = values.ReadText(element.Element(UblNames.Cbc + "Name")),
                Telephone = values.ReadText(element.Element(UblNames.Cbc + "Telephone")),
                Email = values.ReadText(element.Element(UblNames.Cbc + "ElectronicMail")),
            };

    private static DeliveryInformation? ReadDelivery(XElement? element, UblValueReader values)
    {
        if (element is null)
        {
            return null;
        }

        XElement? location = Descend(values, element, UblNames.Cac + "DeliveryLocation");

        return new DeliveryInformation
        {
            ActualDeliveryDate = values.ReadDate(element.Element(UblNames.Cbc + "ActualDeliveryDate"), "BT-72"),
            LocationIdentifier = values.ReadIdentifier(location?.Element(UblNames.Cbc + "ID")),
            Name = values.ReadText(
                Descend(values, Descend(values, Descend(values, element, UblNames.Cac + "DeliveryParty"), UblNames.Cac + "PartyName"), UblNames.Cbc + "Name")),
            Address = ReadAddress(Descend(values, location, UblNames.Cac + "Address"), values),
        };
    }

    private static PaymentInstructions? ReadPayment(XElement root, UblValueReader values, HashSet<XElement> mapped)
    {
        XElement? means = Take(root, UblNames.Cac + "PaymentMeans", mapped);
        if (means is null)
        {
            return null;
        }

        var payment = new PaymentInstructions
        {
            MeansTypeCode = values.ReadCode(Descend(values, means, UblNames.Cbc + "PaymentMeansCode")),
            RemittanceInformation = values.ReadText(Descend(values, means, UblNames.Cbc + "PaymentID")),
        };

        foreach (XElement account in DescendAll(values, means, UblNames.Cac + "PayeeFinancialAccount"))
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
        values.Consume(Descend(values, category, UblNames.Cac + "TaxScheme"));
        values.Consume(Descend(values, Descend(values, category, UblNames.Cac + "TaxScheme"), UblNames.Cbc + "ID"));

        return new AllowanceCharge
        {
            IsCharge = string.Equals(
                element.Element(UblNames.Cbc + "ChargeIndicator")?.Value.Trim(),
                "true",
                StringComparison.OrdinalIgnoreCase),
            Amount = values.ReadAmount(element.Element(UblNames.Cbc + "Amount")),
            BaseAmount = values.ReadAmount(element.Element(UblNames.Cbc + "BaseAmount")),
            Percentage = values.ReadDecimal(element.Element(UblNames.Cbc + "MultiplierFactorNumeric")),
            Reason = values.ReadText(element.Element(UblNames.Cbc + "AllowanceChargeReason")),
            ReasonCode = values.ReadCode(element.Element(UblNames.Cbc + "AllowanceChargeReasonCode")),
            VatCategoryCode = values.ReadCode(category?.Element(UblNames.Cbc + "ID")),
            VatRate = values.ReadDecimal(category?.Element(UblNames.Cbc + "Percent")),
        };
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

        foreach (XElement allowance in DescendAll(values, element, UblNames.Cac + "AllowanceCharge"))
        {
            line.AllowancesAndCharges.Add(ReadAllowanceCharge(allowance, values));
        }

        XElement? price = Descend(values, element, UblNames.Cac + "Price");
        if (price is not null)
        {
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
            item.ClassificationCodes.Add(
                values.ReadCode(classification.Element(UblNames.Cbc + "ItemClassificationCode")));
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

    /// <summary>
    /// Walks the whole document and gives every element the reader did not map to the node that contained it.
    /// Doing this once at the end, rather than per mapping method, is what makes the guarantee total: an
    /// element nobody thought about is still kept, wherever it sits.
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
                UblValueReader.LocationOf(element)));

            diagnostics.Add(Diagnostic.Create(UblDiagnostics.UnmappedElement, element.Name.LocalName) with
            {
                Location = UblValueReader.LocationOf(element),
                Found = element.Name.LocalName,
                AppliedFallback = "kept verbatim as extension data",
            });
        }
    }
}
