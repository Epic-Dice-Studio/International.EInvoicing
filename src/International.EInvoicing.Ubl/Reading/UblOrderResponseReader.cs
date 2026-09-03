using System.Xml.Linq;
using International.EInvoicing.Configuration;
using International.EInvoicing.Diagnostics;
using International.EInvoicing.Documents;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;
using International.EInvoicing.Xml;

namespace International.EInvoicing.Ubl.Reading;

/// <summary>
/// Reads a UBL <c>OrderResponse</c> — the seller's answer to an order — into the canonical model.
/// </summary>
/// <remarks>
/// It shares the order's readers for the parts that are the order's: an item, a price, a delivery window and
/// a party mean the same thing whichever of the two documents they sit in, and reading them twice would be
/// two chances to read them differently.
/// </remarks>
public sealed class UblOrderResponseReader : IDocumentReader<OrderResponse>
{
    private readonly EInvoicingOptions _options;
    private readonly IProfileResolver _profiles;

    /// <summary>Creates a reader using the supplied options and profile resolver.</summary>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    public UblOrderResponseReader(EInvoicingOptions options, IProfileResolver profiles)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(profiles);

        _options = options;
        _profiles = profiles;
    }

    /// <inheritdoc />
    public DocumentSyntax Syntax => DocumentSyntax.Ubl;

    /// <summary>Reads an order response from a stream. The stream is left open.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <c>null</c>.</exception>
    public ParseResult<OrderResponse> Read(Stream stream)
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

            return diagnostics.ToResult<OrderResponse>(null);
        }

        return diagnostics.ToResult(ReadResponse(root, diagnostics));
    }

    /// <summary>Reads an order response from XML text.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="xml"/> is <c>null</c>.</exception>
    public ParseResult<OrderResponse> Read(string xml)
    {
        ArgumentNullException.ThrowIfNull(xml);

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(xml));
        return Read(stream);
    }

    /// <inheritdoc />
    public async Task<ParseResult<OrderResponse>> ReadAsync(
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        byte[] content = await DocumentStreams.ReadAllAsync(stream, cancellationToken).ConfigureAwait(false);

        using var buffered = new MemoryStream(content, writable: false);
        return Read(buffered);
    }

    private OrderResponse ReadResponse(XElement root, DiagnosticCollector diagnostics)
    {
        var mapped = new HashSet<XElement>();
        var owners = new Dictionary<XElement, InvoiceNode>();
        var values = new UblValueReader(diagnostics, mapped);
        var response = new OrderResponse();

        response.SpecificationIdentifier = ProfileIdentifier.FromDocument(
            UblOrderReader.Take(root, UblNames.Cbc + "CustomizationID", mapped)?.Value);
        response.BusinessProcessType = values.ReadIdentifier(
            UblOrderReader.Take(root, UblNames.Cbc + "ProfileID", mapped));
        response.Number = values.ReadIdentifier(UblOrderReader.Take(root, UblNames.Cbc + "ID", mapped));
        response.SalesOrderNumber = values.ReadIdentifier(
            UblOrderReader.Take(root, UblNames.Cbc + "SalesOrderID", mapped));
        response.IssuedAt = UblMoment.Read(
            UblOrderReader.Take(root, UblNames.Cbc + "IssueDate", mapped),
            UblOrderReader.Take(root, UblNames.Cbc + "IssueTime", mapped));
        response.ResponseCode = values.ReadCode(
            UblOrderReader.Take(root, UblNames.Cbc + "OrderResponseCode", mapped));
        foreach (XElement note in UblOrderReader.TakeAll(root, UblNames.Cbc + "Note", mapped))
        {
            response.Notes.Add(values.ReadNote(note));
        }
        response.CurrencyCode = values.ReadCode(
            UblOrderReader.Take(root, UblNames.Cbc + "DocumentCurrencyCode", mapped));
        response.BuyerReference = values.ReadText(
            UblOrderReader.Take(root, UblNames.Cbc + "CustomerReference", mapped));

        if (UblOrderReader.Take(root, UblNames.Cac + "OrderReference", mapped) is { } order)
        {
            owners[order] = response;
            response.OrderReference = values.ReadIdentifier(
                UblOrderReader.Take(order, UblNames.Cbc + "ID", mapped));
        }

        if (UblOrderReader.Take(root, UblNames.Cac + "OrderChangeDocumentReference", mapped) is { } change)
        {
            owners[change] = response;
            response.OrderChangeReference = values.ReadIdentifier(
                UblOrderReader.Take(change, UblNames.Cbc + "ID", mapped));
        }

        response.OriginatorReference = UblOrderReader.Reference(
            root, "OriginatorDocumentReference", values, mapped, owners, response);
        response.ContractReference = UblOrderReader.Reference(
            root, "Contract", values, mapped, owners, response);

        foreach (XElement attached in UblOrderReader.TakeAll(
            root, UblNames.Cac + "AdditionalDocumentReference", mapped))
        {
            response.AdditionalDocuments.Add(
                UblAttachments.Read(attached, values, mapped, owners, _options.Limits));
        }

        response.Seller = UblOrderReader.WrappedParty(root, "SellerSupplierParty", values, mapped, owners);
        response.Buyer = UblOrderReader.WrappedParty(root, "BuyerCustomerParty", values, mapped, owners);
        response.Originator = UblOrderReader.WrappedParty(
            root, "OriginatorCustomerParty", values, mapped, owners);
        response.Invoicee = UblOrderReader.WrappedParty(
            root, "AccountingCustomerParty", values, mapped, owners);

        // The buyer's role element may carry the contact the driver calls, beside the party rather than
        // inside it — the same shape the despatch advice uses.
        if (UblOrderReader.Take(root, UblNames.Cac + "BuyerCustomerParty", mapped) is { } buyerRole
            && UblOrderReader.Take(buyerRole, UblNames.Cac + "DeliveryContact", mapped) is { } contact
            && response.Buyer is not null)
        {
            response.Buyer.Contact ??= UblParties.ReadContact(contact, values, mapped, owners);
        }

        foreach (XElement allowance in UblOrderReader.TakeAll(root, UblNames.Cac + "AllowanceCharge", mapped))
        {
            response.AllowancesAndCharges.Add(
                UblOrderReader.ReadAllowanceCharge(allowance, values, mapped, owners));
        }

        ReadTaxTotal(UblOrderReader.Take(root, UblNames.Cac + "TaxTotal", mapped), response, values, mapped, owners);
        UblOrderReader.ReadTotals(
            UblOrderReader.Take(root, UblNames.Cac + "LegalMonetaryTotal", mapped),
            response.Totals,
            values,
            mapped,
            owners);
        response.Delivery = UblOrderReader.ReadDelivery(
            UblOrderReader.Take(root, UblNames.Cac + "Delivery", mapped), values, mapped, owners);

        foreach (XElement line in UblOrderReader.TakeAll(root, UblNames.Cac + "OrderLine", mapped))
        {
            if (Limits.Exceeded(response.Lines.Count, _options.Limits.MaxDocumentLines))
            {
                diagnostics.Add(Limits.TooMany(_options.Limits.MaxDocumentLines, "order response lines"));
                break;
            }

            OrderResponseLine mappedLine = ReadLine(line, values, mapped, owners, _options.Limits);
            owners[line] = mappedLine;
            response.Lines.Add(mappedLine);
        }

        UblExtensions.KeepEverythingElse(root, response, mapped, owners, diagnostics);

        ProfileResolution resolution = _profiles.Resolve(response.SpecificationIdentifier, DocumentSyntax.Ubl);
        foreach (Diagnostic diagnostic in resolution.Diagnostics)
        {
            diagnostics.Add(diagnostic);
        }

        response.Profile = resolution;
        response.Diagnostics = diagnostics.Diagnostics;
        return response;
    }

    /// <summary>The tax the parties agreed, and its breakdown by category and rate.</summary>
    private static void ReadTaxTotal(
        XElement? element,
        OrderResponse response,
        UblValueReader values,
        HashSet<XElement> mapped,
        Dictionary<XElement, InvoiceNode> owners)
    {
        if (element is null)
        {
            return;
        }

        owners[element] = response;
        response.TaxAmount = values.ReadAmount(UblOrderReader.Take(element, UblNames.Cbc + "TaxAmount", mapped));

        foreach (XElement subtotal in UblOrderReader.TakeAll(element, UblNames.Cac + "TaxSubtotal", mapped))
        {
            var entry = new VatBreakdownEntry
            {
                TaxableAmount = values.ReadAmount(
                    UblOrderReader.Take(subtotal, UblNames.Cbc + "TaxableAmount", mapped)),
                TaxAmount = values.ReadAmount(
                    UblOrderReader.Take(subtotal, UblNames.Cbc + "TaxAmount", mapped)),
            };

            owners[subtotal] = entry;

            if (UblOrderReader.Take(subtotal, UblNames.Cac + "TaxCategory", mapped) is { } category)
            {
                owners[category] = entry;
                entry.CategoryCode = values.ReadCode(
                    UblOrderReader.Take(category, UblNames.Cbc + "ID", mapped));
                entry.Rate = values.ReadDecimal(
                    UblOrderReader.Take(category, UblNames.Cbc + "Percent", mapped));

                if (UblOrderReader.Take(category, UblNames.Cac + "TaxScheme", mapped) is { } scheme)
                {
                    owners[scheme] = entry;
                    UblOrderReader.Take(scheme, UblNames.Cbc + "ID", mapped);
                }
            }

            response.VatBreakdown.Add(entry);
        }
    }

    private static OrderResponseLine ReadLine(
        XElement element,
        UblValueReader values,
        HashSet<XElement> mapped,
        Dictionary<XElement, InvoiceNode> owners,
        DocumentLimits limits)
    {
        var line = new OrderResponseLine();

        if (UblOrderReader.Take(element, UblNames.Cac + "OrderLineReference", mapped) is { } reference)
        {
            owners[reference] = line;
            line.OrderLineReference = values.ReadIdentifier(
                UblOrderReader.Take(reference, UblNames.Cbc + "LineID", mapped));
        }

        if (UblOrderReader.Take(element, UblNames.Cac + "LineItem", mapped) is { } item)
        {
            owners[item] = line;
            line.Identifier = values.ReadIdentifier(UblOrderReader.Take(item, UblNames.Cbc + "ID", mapped));
            foreach (XElement note in UblOrderReader.TakeAll(item, UblNames.Cbc + "Note", mapped))
            {
                line.Notes.Add(values.ReadNote(note));
            }
            line.StatusCode = values.ReadCode(UblOrderReader.Take(item, UblNames.Cbc + "LineStatusCode", mapped));
            line.Quantity = values.ReadQuantity(UblOrderReader.Take(item, UblNames.Cbc + "Quantity", mapped));
            line.NetAmount = values.ReadAmount(
                UblOrderReader.Take(item, UblNames.Cbc + "LineExtensionAmount", mapped));
            line.MaximumBackorderQuantity = values.ReadQuantity(
                UblOrderReader.Take(item, UblNames.Cbc + "MaximumBackorderQuantity", mapped));
            line.Delivery = UblOrderReader.ReadDelivery(
                UblOrderReader.Take(item, UblNames.Cac + "Delivery", mapped), values, mapped, owners);
            line.Price = UblOrderReader.ReadPrice(
                UblOrderReader.Take(item, UblNames.Cac + "Price", mapped), values, mapped, owners);
            line.Item = UblOrderReader.ReadItem(
                UblOrderReader.Take(item, UblNames.Cac + "Item", mapped), values, mapped, owners, limits);
        }

        // What the seller offers instead of what was ordered.
        if (UblOrderReader.Take(element, UblNames.Cac + "SellerSubstitutedLineItem", mapped) is { } substitute)
        {
            owners[substitute] = line;
            line.SubstitutedIdentifier = values.ReadIdentifier(
                UblOrderReader.Take(substitute, UblNames.Cbc + "ID", mapped));
            line.SubstitutedItem = UblOrderReader.ReadItem(
                UblOrderReader.Take(substitute, UblNames.Cac + "Item", mapped), values, mapped, owners, limits);
        }

        return line;
    }
}
