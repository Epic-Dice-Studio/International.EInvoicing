using System.Xml.Linq;
using International.EInvoicing.Configuration;
using International.EInvoicing.Diagnostics;
using International.EInvoicing.Documents;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;
using International.EInvoicing.Xml;

namespace International.EInvoicing.Ubl.Reading;

/// <summary>Reads a UBL <c>OrderCancellation</c> — the buyer withdrawing an order — into the canonical model.</summary>
public sealed class UblOrderCancellationReader : IDocumentReader<OrderCancellation>
{
    private readonly EInvoicingOptions _options;
    private readonly IProfileResolver _profiles;

    /// <summary>Creates a reader using the supplied options and profile resolver.</summary>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    public UblOrderCancellationReader(EInvoicingOptions options, IProfileResolver profiles)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(profiles);

        _options = options;
        _profiles = profiles;
    }

    /// <inheritdoc />
    public DocumentSyntax Syntax => DocumentSyntax.Ubl;

    /// <summary>Reads a cancellation from a stream. The stream is left open.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <c>null</c>.</exception>
    public ParseResult<OrderCancellation> Read(Stream stream)
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

            return diagnostics.ToResult<OrderCancellation>(null);
        }

        return diagnostics.ToResult(ReadCancellation(root, diagnostics));
    }

    /// <summary>Reads a cancellation from XML text.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="xml"/> is <c>null</c>.</exception>
    public ParseResult<OrderCancellation> Read(string xml)
    {
        ArgumentNullException.ThrowIfNull(xml);

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(xml));
        return Read(stream);
    }

    /// <inheritdoc />
    public async Task<ParseResult<OrderCancellation>> ReadAsync(
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        byte[] content = await DocumentStreams.ReadAllAsync(stream, cancellationToken).ConfigureAwait(false);

        using var buffered = new MemoryStream(content, writable: false);
        return Read(buffered);
    }

    private OrderCancellation ReadCancellation(XElement root, DiagnosticCollector diagnostics)
    {
        var mapped = new HashSet<XElement>();
        var owners = new Dictionary<XElement, InvoiceNode>();
        var values = new UblValueReader(diagnostics, mapped);
        var cancellation = new OrderCancellation();

        cancellation.SpecificationIdentifier = ProfileIdentifier.FromDocument(
            UblOrderReader.Take(root, UblNames.Cbc + "CustomizationID", mapped)?.Value);
        cancellation.BusinessProcessType = values.ReadIdentifier(
            UblOrderReader.Take(root, UblNames.Cbc + "ProfileID", mapped));
        cancellation.Number = values.ReadIdentifier(UblOrderReader.Take(root, UblNames.Cbc + "ID", mapped));
        cancellation.IssuedAt = UblMoment.Read(
            UblOrderReader.Take(root, UblNames.Cbc + "IssueDate", mapped),
            UblOrderReader.Take(root, UblNames.Cbc + "IssueTime", mapped));
        cancellation.Note = values.ReadText(UblOrderReader.Take(root, UblNames.Cbc + "Note", mapped));
        cancellation.Reason = values.ReadText(
            UblOrderReader.Take(root, UblNames.Cbc + "CancellationNote", mapped));

        if (UblOrderReader.Take(root, UblNames.Cac + "OrderReference", mapped) is { } order)
        {
            owners[order] = cancellation;
            cancellation.OrderReference = values.ReadIdentifier(
                UblOrderReader.Take(order, UblNames.Cbc + "ID", mapped));
        }

        if (UblOrderReader.Take(root, UblNames.Cac + "OriginatorDocumentReference", mapped) is { } originator)
        {
            owners[originator] = cancellation;
            cancellation.OriginatorReference = values.ReadIdentifier(
                UblOrderReader.Take(originator, UblNames.Cbc + "ID", mapped));
        }

        foreach (XElement attached in UblOrderReader.TakeAll(root, UblNames.Cac + "AdditionalDocumentReference", mapped))
        {
            cancellation.AdditionalDocuments.Add(
                UblAttachments.Read(attached, values, mapped, owners, _options.Limits));
        }

        if (UblOrderReader.Take(root, UblNames.Cac + "Contract", mapped) is { } contract)
        {
            owners[contract] = cancellation;
            cancellation.ContractReference = values.ReadIdentifier(
                UblOrderReader.Take(contract, UblNames.Cbc + "ID", mapped));
        }

        cancellation.Buyer = UblOrderReader.WrappedParty(root, "BuyerCustomerParty", values, mapped, owners);
        cancellation.Seller = UblOrderReader.WrappedParty(root, "SellerSupplierParty", values, mapped, owners);
        cancellation.Originator = UblOrderReader.WrappedParty(
            root, "OriginatorCustomerParty", values, mapped, owners);

        UblExtensions.KeepEverythingElse(root, cancellation, mapped, owners, diagnostics);

        ProfileResolution resolution = _profiles.Resolve(cancellation.SpecificationIdentifier, DocumentSyntax.Ubl);
        foreach (Diagnostic diagnostic in resolution.Diagnostics)
        {
            diagnostics.Add(diagnostic);
        }

        cancellation.Profile = resolution;
        cancellation.Diagnostics = diagnostics.Diagnostics;
        return cancellation;
    }
}
