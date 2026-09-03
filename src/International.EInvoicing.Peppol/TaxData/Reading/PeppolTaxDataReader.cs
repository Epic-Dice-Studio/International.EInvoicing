using System.Globalization;
using System.Xml.Linq;
using International.EInvoicing.Configuration;
using International.EInvoicing.Diagnostics;
using International.EInvoicing.Documents;
using International.EInvoicing.Model;
using International.EInvoicing.Peppol.TaxData.Model;
using International.EInvoicing.Profiles;
using International.EInvoicing.Ubl;
using International.EInvoicing.Ubl.Reading;
using International.EInvoicing.Xml;

namespace International.EInvoicing.Peppol.TaxData.Reading;

/// <summary>
/// Reads a tax data document — the receiver's side of tax reporting.
/// </summary>
/// <remarks>
/// <para>
/// The reported document is a <em>projection</em> of an invoice, in the same UBL vocabulary with three
/// elements renamed. So it is read as one: the projection is translated back into the shape
/// <see cref="UblInvoiceReader"/> already understands and handed to it, rather than mapped a second time
/// here. Every business term the UBL reader learns, this learns with it.
/// </para>
/// <para>
/// Reading never throws on the document. A jurisdiction this library does not know is still read — the
/// envelope is the same everywhere — and the downgrade is reported rather than hidden.
/// </para>
/// </remarks>
public sealed class PeppolTaxDataReader
{
    private readonly EInvoicingOptions _options;
    private readonly IProfileResolver _profiles;

    /// <summary>Creates a reader using the supplied options and profile resolver.</summary>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    public PeppolTaxDataReader(EInvoicingOptions options, IProfileResolver profiles)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(profiles);

        _options = options;
        _profiles = profiles;
    }

    /// <summary>The root element every tax data document has, whatever its jurisdiction.</summary>
    public const string RootElement = "TaxData";

    /// <summary>Whether a document is a tax data document, judged by its root element.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="xml"/> is <c>null</c>.</exception>
    public static bool LooksLikeTaxData(string xml)
    {
        ArgumentNullException.ThrowIfNull(xml);

        try
        {
            using var reader = SecureXml.CreateReader(xml);
            XName? root = XDocument.Load(reader).Root?.Name;

            return root?.LocalName == RootElement
                && root.NamespaceName.StartsWith("urn:peppol:schema:", StringComparison.Ordinal);
        }
        catch (System.Xml.XmlException)
        {
            return false;
        }
    }

    /// <summary>Reads a tax data document from a stream. The stream is left open.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <c>null</c>.</exception>
    public ParseResult<PeppolTaxData> Read(Stream stream)
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

            return diagnostics.ToResult<PeppolTaxData>(null);
        }

        return diagnostics.ToResult(ReadTaxData(root, diagnostics));
    }

    /// <summary>Reads a tax data document from XML text.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="xml"/> is <c>null</c>.</exception>
    public ParseResult<PeppolTaxData> Read(string xml)
    {
        ArgumentNullException.ThrowIfNull(xml);

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(xml));
        return Read(stream);
    }

    /// <summary>Reads a tax data document without blocking while it arrives.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <c>null</c>.</exception>
    public async Task<ParseResult<PeppolTaxData>> ReadAsync(
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        byte[] content = await DocumentStreams.ReadAllAsync(stream, cancellationToken).ConfigureAwait(false);

        using var buffered = new MemoryStream(content, writable: false);
        return Read(buffered);
    }

    private PeppolTaxData ReadTaxData(XElement root, DiagnosticCollector diagnostics)
    {
        XNamespace pxs = root.Name.Namespace;

        var document = new PeppolTaxData
        {
            Jurisdiction = JurisdictionOf(root, diagnostics),
            Uuid = Text(root, UblNames.Cbc + "UUID"),
            IssuedAt = Moment(root),
            TaxDataTypeCode = Text(root, pxs + "TaxDataTypeCode"),
            DocumentScope = Text(root, pxs + "DocumentScope"),
            ReporterRole = Text(root, pxs + "ReporterRole"),
        };

        if (root.Element(pxs + "TaxAuthority") is { } authority)
        {
            document.Authority = new PeppolTaxAuthority
            {
                Id = Text(authority, UblNames.Cbc + "ID"),
                Name = authority.Element(UblNames.Cbc + "Name")?.Value,
            };
        }

        document.ReportingParty = Endpoint(root.Element(pxs + "ReportingParty"));
        document.ReceivingParty = Endpoint(root.Element(pxs + "ReceivingParty"));
        document.ReportersRepresentative = Representative(root.Element(pxs + "ReportersRepresentative"));

        XElement? reported = root.Element(pxs + "ReportedTransaction")?.Element(pxs + "ReportedDocument");
        if (reported is not null)
        {
            document.ReportedDocumentUuid = Text(reported, UblNames.Cbc + "UUID");
            document.ReportedDocument = ReadReportedDocument(reported, pxs, diagnostics);
        }

        return document;
    }

    /// <summary>
    /// The reported document, read as the invoice it is a projection of.
    /// </summary>
    /// <remarks>
    /// Three elements are named differently and everything else is UBL as published: the document type code,
    /// the totals and the lines. Renaming those and the root gives the invoice reader a document it already
    /// knows, which is why a term it maps is a term this maps.
    /// </remarks>
    private EInvoice? ReadReportedDocument(XElement reported, XNamespace pxs, DiagnosticCollector diagnostics)
    {
        var invoice = new XElement(UblNames.Invoice + "Invoice", reported.Attributes(), reported.Nodes());

        foreach (XElement element in invoice.Descendants().ToList())
        {
            if (element.Name.Namespace != pxs)
            {
                continue;
            }

            element.Name = element.Name.LocalName switch
            {
                "DocumentTypeCode" => UblNames.Cbc + "InvoiceTypeCode",
                "MonetaryTotal" => UblNames.Cac + "LegalMonetaryTotal",
                "DocumentLine" => UblNames.Cac + "InvoiceLine",
                _ => element.Name,
            };
        }

        ParseResult<EInvoice> result = new UblInvoiceReader(_options, _profiles)
            .Read(invoice.ToString(SaveOptions.DisableFormatting));

        foreach (Diagnostic diagnostic in result.Diagnostics)
        {
            diagnostics.Add(diagnostic);
        }

        return result.Value;
    }

    /// <summary>
    /// Which jurisdiction's document this is, taken from the namespace it declares.
    /// </summary>
    /// <remarks>
    /// A jurisdiction this library does not carry is still read: the envelope is the same everywhere, and
    /// only the code lists differ. What the caller loses is the checking of those lists, and that is said
    /// out loud rather than passed off as a document nobody had to judge.
    /// </remarks>
    private static PeppolTaxDataJurisdiction JurisdictionOf(XElement root, DiagnosticCollector diagnostics)
    {
        string declared = root.Element(UblNames.Cbc + "CustomizationID")?.Value.Trim() ?? string.Empty;

        PeppolTaxDataJurisdiction? known = PeppolTaxDataJurisdiction.All.FirstOrDefault(
            jurisdiction => string.Equals(jurisdiction.CustomizationId, declared, StringComparison.Ordinal));

        if (known is not null)
        {
            return known;
        }

        diagnostics.Add(Diagnostic.Create(DiagnosticCodes.UnknownProfile, declared) with
        {
            Expected = string.Join(", ", PeppolTaxDataJurisdiction.All.Select(j => j.CustomizationId)),
            Found = declared,
            AppliedFallback = "read as a tax data document; the jurisdiction's code lists are not checked",
        });

        return new PeppolTaxDataJurisdiction(
            $"Peppol Tax Data Document ({declared})",
            root.Name.NamespaceName,
            declared,
            [],
            [],
            []);
    }

    private static PeppolTaxDataEndpoint Endpoint(XElement? element)
    {
        if (element?.Element(UblNames.Cbc + "EndpointID") is not { } endpoint)
        {
            return new PeppolTaxDataEndpoint();
        }

        return new PeppolTaxDataEndpoint
        {
            Id = endpoint.Value.Trim(),
            SchemeId = endpoint.Attribute("schemeID")?.Value ?? string.Empty,
        };
    }

    private static PeppolTaxDataEndpoint? Representative(XElement? element)
    {
        if (element?.Element(UblNames.Cac + "PartyIdentification")?.Element(UblNames.Cbc + "ID") is not { } id)
        {
            return null;
        }

        return new PeppolTaxDataEndpoint
        {
            Id = id.Value.Trim(),
            SchemeId = id.Attribute("schemeID")?.Value ?? string.Empty,
        };
    }

    /// <summary>The moment the document was issued, which UBL states as a date and a time of day.</summary>
    private static DateTimeOffset Moment(XElement root)
    {
        string date = Text(root, UblNames.Cbc + "IssueDate");
        string time = Text(root, UblNames.Cbc + "IssueTime");

        string text = time.Length == 0 ? date : $"{date}T{time}";

        return DateTimeOffset.TryParse(
            text,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out DateTimeOffset moment)
            ? moment
            : default;
    }

    private static string Text(XElement parent, XName name) => parent.Element(name)?.Value.Trim() ?? string.Empty;
}
