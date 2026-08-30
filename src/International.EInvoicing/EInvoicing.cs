using System.Xml.Linq;
using International.EInvoicing.Cdar;
using International.EInvoicing.Cdar.Model;
using International.EInvoicing.Cdar.Reading;
using International.EInvoicing.Cdar.Writing;
using International.EInvoicing.Cii;
using International.EInvoicing.Cii.Reading;
using International.EInvoicing.Cii.Writing;
using International.EInvoicing.Configuration;
using International.EInvoicing.Diagnostics;
using International.EInvoicing.FacturX;
using International.EInvoicing.FacturX.Pdf;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;
using International.EInvoicing.Ubl;
using International.EInvoicing.Ubl.Reading;
using International.EInvoicing.Ubl.Writing;
using International.EInvoicing.Validation;
using International.EInvoicing.Validation.En16931;
using International.EInvoicing.Validation.Schematron;
using International.EInvoicing.Xml;

namespace International.EInvoicing;

/// <summary>The syntax to write a document in.</summary>
public enum DocumentFormat
{
    /// <summary>OASIS UBL 2.1 — the syntax of Peppol, Belgium and the Nordics.</summary>
    Ubl,

    /// <summary>UN/CEFACT CII — the payload of Factur-X, ZUGFeRD and XRechnung CII.</summary>
    Cii,
}

/// <summary>
/// The short way in: hand it a document, get back what it is.
/// </summary>
/// <remarks>
/// <para>
/// This is a convenience over the individual readers and writers, not a replacement for them. Everything
/// underneath stays reachable — <see cref="Ubl"/>, <see cref="Cii"/>, <see cref="Lifecycle"/>,
/// <see cref="Profiles"/> — for when a caller needs to be specific.
/// </para>
/// <para>
/// Reading never throws on a document you received. Unknown profiles, unreadable values and unmapped elements
/// come back as diagnostics with documented fallbacks.
/// </para>
/// </remarks>
public sealed class EInvoicing
{
    private readonly EInvoicingOptions _options;
    private readonly IPdfAttachmentReader? _pdf;

    private EInvoicing(EInvoicingOptions options, IProfileResolver profiles, IPdfAttachmentReader? pdf)
    {
        _options = options;
        _pdf = pdf;
        Profiles = profiles;

        Ubl = new UblInvoiceReader(options, profiles);
        Cii = new CiiInvoiceReader(options, profiles);
        Lifecycle = new CdarReader(options, profiles);
        UblWriter = new UblInvoiceWriter();
        CiiWriter = new CiiInvoiceWriter();
        LifecycleWriter = new CdarWriter();
    }

    /// <summary>The UBL reader, for a caller that already knows what it holds.</summary>
    public UblInvoiceReader Ubl { get; }

    /// <summary>The CII reader.</summary>
    public CiiInvoiceReader Cii { get; }

    /// <summary>The lifecycle message reader.</summary>
    public CdarReader Lifecycle { get; }

    /// <summary>The UBL writer.</summary>
    public UblInvoiceWriter UblWriter { get; }

    /// <summary>The CII writer.</summary>
    public CiiInvoiceWriter CiiWriter { get; }

    /// <summary>The lifecycle message writer.</summary>
    public CdarWriter LifecycleWriter { get; }

    /// <summary>How declared profiles are resolved, and what this instance implements.</summary>
    public IProfileResolver Profiles { get; }

    /// <summary>
    /// Everything this library ships: UBL, CII, Factur-X and lifecycle profiles, with balanced diagnostics.
    /// </summary>
    /// <param name="pdf">
    /// A PDF reader, if hybrid invoices should be opened. Reference
    /// <c>International.EInvoicing.FacturX.PdfSharp</c> for one; without it a PDF is reported rather than read.
    /// </param>
    public static EInvoicing CreateDefault(IPdfAttachmentReader? pdf = null)
    {
        var registry = new ProfileRegistry(KnownProfiles.All);

        foreach (Profile profile in FacturXProfiles.All.Concat(CdarProfiles.All))
        {
            registry.Register(profile);
        }

        return new EInvoicing(new EInvoicingOptions(), new ProfileResolver(registry), pdf);
    }

    /// <summary>The same, with your own options — resource limits, diagnostic policy.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <c>null</c>.</exception>
    public static EInvoicing Create(
        EInvoicingOptions options,
        IProfileResolver? profiles = null,
        IPdfAttachmentReader? pdf = null)
    {
        ArgumentNullException.ThrowIfNull(options);

        IProfileResolver resolver = profiles ?? CreateDefault().Profiles;
        return new EInvoicing(options, resolver, pdf);
    }

    /// <summary>Reads whatever the stream holds. The stream is left open.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> is <c>null</c>.</exception>
    public DocumentResult Read(Stream document)
    {
        ArgumentNullException.ThrowIfNull(document);

        byte[] content;
        using (var buffer = new MemoryStream())
        {
            document.CopyTo(buffer);
            content = buffer.ToArray();
        }

        return Read(content);
    }

    /// <summary>Reads whatever the text holds.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> is <c>null</c>.</exception>
    public DocumentResult Read(string document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return Read(System.Text.Encoding.UTF8.GetBytes(document));
    }

    /// <summary>Reads whatever the bytes hold, PDF included.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> is <c>null</c>.</exception>
    public DocumentResult Read(byte[] document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (FacturXReader.LooksLikePdf(document))
        {
            return ReadHybrid(document);
        }

        string text = System.Text.Encoding.UTF8.GetString(document);

        return Detect(text) switch
        {
            DocumentKind.Ubl => FromInvoice(DocumentKind.Ubl, Ubl.Read(text)),
            DocumentKind.Cii => FromInvoice(DocumentKind.Cii, Cii.Read(text)),
            DocumentKind.Cdar => FromStatus(Lifecycle.Read(text)),
            _ => new DocumentResult
            {
                Kind = DocumentKind.Unknown,
                Diagnostics = [Unrecognised()],
            },
        };
    }

    /// <summary>What a document is, judged by its root element rather than by its file name.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> is <c>null</c>.</exception>
    public static DocumentKind Detect(string document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (document.TrimStart().StartsWith("%PDF-", StringComparison.Ordinal))
        {
            return DocumentKind.Pdf;
        }

        XName root;
        try
        {
            using var reader = SecureXml.CreateReader(document);
            root = XDocument.Load(reader).Root?.Name ?? XName.Get("none");
        }
        catch (System.Xml.XmlException)
        {
            return DocumentKind.Unknown;
        }

        if (root.Namespace == CdarNames.Rsm)
        {
            return DocumentKind.Cdar;
        }

        if (root.Namespace == CiiNames.Rsm)
        {
            return DocumentKind.Cii;
        }

        return root.Namespace == UblNames.Invoice || root.Namespace == UblNames.CreditNote
            ? DocumentKind.Ubl
            : DocumentKind.Unknown;
    }

    /// <summary>Writes an invoice in the syntax you name.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="invoice"/> is <c>null</c>.</exception>
    public string Write(EInvoice invoice, DocumentFormat format)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        return format == DocumentFormat.Cii
            ? CiiWriter.WriteToString(invoice)
            : UblWriter.WriteToString(invoice);
    }

    /// <summary>Writes a lifecycle status message.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="status"/> is <c>null</c>.</exception>
    public string Write(LifecycleStatusMessage status)
    {
        ArgumentNullException.ThrowIfNull(status);
        return LifecycleWriter.WriteToString(status);
    }

    /// <summary>
    /// Validates a document against EN 16931, and says what it could not check.
    /// </summary>
    /// <remarks>
    /// A profile this library implements no rules for is reported as not checked rather than passed over, so
    /// <see cref="ValidationReport.IsComplete"/> tells the truth about how much was verified.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> is <c>null</c>.</exception>
    public ValidationReport Validate(string document)
    {
        ArgumentNullException.ThrowIfNull(document);

        DocumentKind kind = Detect(document);
        if (kind is not (DocumentKind.Ubl or DocumentKind.Cii))
        {
            return new ValidationReport(
                [],
                [new RuleSetOutcome("EN 16931", En16931Rules.ArtefactVersion, Ran: false, $"{kind} is not an EN 16931 syntax")]);
        }

        DocumentSyntax syntax = kind == DocumentKind.Ubl ? DocumentSyntax.Ubl : DocumentSyntax.Cii;
        ValidationReport report = new SchematronValidator().Validate(document, En16931Rules.For(syntax));

        DocumentResult read = Read(document);
        if (read.Profile is not { IsExact: false } resolution)
        {
            return report;
        }

        return report.And(new ValidationReport(
            [],
            [
                new RuleSetOutcome(
                    resolution.Declared.ToString(),
                    "—",
                    Ran: false,
                    "this library implements no rule set for that profile, so only EN 16931 was checked"),
            ]));
    }

    private DocumentResult ReadHybrid(byte[] pdf)
    {
        using var stream = new MemoryStream(pdf);
        var reader = new FacturXReader(_options, Cii, _pdf);
        ParseResult<EInvoice> result = reader.Read(stream);

        return new DocumentResult
        {
            Kind = DocumentKind.Pdf,
            Invoice = result.Value,
            Diagnostics = result.Diagnostics,
            Profile = result.Value?.Profile,
        };
    }

    private static DocumentResult FromInvoice(DocumentKind kind, ParseResult<EInvoice> result) => new()
    {
        Kind = kind,
        Invoice = result.Value,
        Diagnostics = result.Diagnostics,
        Profile = result.Value?.Profile,
    };

    private static DocumentResult FromStatus(ParseResult<LifecycleStatusMessage> result) => new()
    {
        Kind = DocumentKind.Cdar,
        LifecycleStatus = result.Value,
        Diagnostics = result.Diagnostics,
        Profile = result.Value?.Profile,
    };

    private static Diagnostic Unrecognised() =>
        Diagnostic.Create(EInvoicingDiagnostics.UnrecognisedDocument) with
        {
            Expected = "a UBL invoice, a CII invoice, a lifecycle message, or a PDF carrying one",
            Found = "an unrecognised root element",
        };
}
