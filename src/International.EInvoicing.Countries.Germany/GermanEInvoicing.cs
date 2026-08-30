using International.EInvoicing.Building;
using International.EInvoicing.Configuration;
using International.EInvoicing.Countries.Germany.Identifiers;
using International.EInvoicing.FacturX.Pdf;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;
using International.EInvoicing.Validation;
using International.EInvoicing.Validation.XRechnung;

namespace International.EInvoicing.Countries.Germany;

/// <summary>
/// Everything German, from one object.
/// </summary>
/// <remarks>
/// Germany exchanges XRechnung — a CIUS of EN 16931 in both syntaxes — and ZUGFeRD, which is Factur-X under
/// another name. What it adds beyond the norm is the Leitweg-ID that routes a public-sector invoice, and a
/// rule set that is published under a licence, so unlike France and Peppol it ships with this library and
/// validation works out of the box.
/// </remarks>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Performance",
    "CA1822:Mark members as static",
    Justification = "Everything German hangs off one object on purpose; a static member here would send the "
        + "caller back to remembering which type builds what, which is the problem this type exists to solve.")]
public sealed class GermanEInvoicing
{
    private readonly EInvoicing _library;

    private GermanEInvoicing(EInvoicing library) => _library = library;

    /// <summary>The whole library underneath, for anything this shortcut does not cover.</summary>
    public EInvoicing Library => _library;

    /// <summary>A German library instance: both syntaxes, ZUGFeRD, and the German rules.</summary>
    public static GermanEInvoicing Create() => Create(pdf: null);

    /// <summary>The same, able to open ZUGFeRD invoices.</summary>
    /// <param name="pdf">
    /// A PDF reader. Reference <c>International.EInvoicing.FacturX.PdfSharp</c> for one.
    /// </param>
    public static GermanEInvoicing Create(IPdfAttachmentReader? pdf) =>
        Create(germany => germany.AddDefaults().AddGermany().AddXRechnungRules(), pdf);

    /// <summary>The same, with anything else you want registered.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="configure"/> is <c>null</c>.</exception>
    public static GermanEInvoicing Create(Action<EInvoicingBuilder> configure) => Create(configure, null);

    /// <summary>The same, able to open ZUGFeRD invoices.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="configure"/> is <c>null</c>.</exception>
    public static GermanEInvoicing Create(Action<EInvoicingBuilder> configure, IPdfAttachmentReader? pdf)
    {
        ArgumentNullException.ThrowIfNull(configure);

        return new GermanEInvoicing(EInvoicing.Create(configure, pdf));
    }

    /// <summary>The German side of a library instance you already have.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="library"/> is <c>null</c>.</exception>
    public static GermanEInvoicing Over(EInvoicing library)
    {
        ArgumentNullException.ThrowIfNull(library);

        return new GermanEInvoicing(library);
    }

    /// <summary>Reads whatever arrived — an invoice, a credit note, or a ZUGFeRD PDF carrying one.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> is <c>null</c>.</exception>
    public DocumentResult Read(string document) => _library.Read(document);

    /// <summary>Reads whatever the bytes hold, a ZUGFeRD PDF included.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> is <c>null</c>.</exception>
    public DocumentResult Read(byte[] document) => _library.Read(document);

    /// <summary>Reads whatever the stream holds. The stream is left open.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> is <c>null</c>.</exception>
    public DocumentResult Read(Stream document) => _library.Read(document);

    /// <summary>Reads whatever the stream holds, without blocking while it arrives.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> is <c>null</c>.</exception>
    /// <exception cref="OperationCanceledException">The token was cancelled while the document arrived.</exception>
    public Task<DocumentResult> ReadAsync(Stream document, CancellationToken cancellationToken = default) =>
        _library.ReadAsync(document, cancellationToken);

    /// <summary>Reads whatever the file holds.</summary>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty.</exception>
    public DocumentResult ReadFile(string path) => _library.ReadFile(path);

    /// <summary>An invoice declaring XRechnung, in UBL.</summary>
    public EInvoiceBuilder Invoice() => Invoice(DocumentSyntax.Ubl);

    /// <summary>An invoice declaring XRechnung, in the syntax you name.</summary>
    public EInvoiceBuilder Invoice(DocumentSyntax syntax) =>
        EInvoiceBuilder
            .Create(syntax == DocumentSyntax.Cii ? DeProfiles.XRechnungCii : DeProfiles.XRechnungUbl)
            .InCurrency("EUR");

    /// <summary>A credit note declaring XRechnung, in UBL.</summary>
    public EInvoiceBuilder CreditNote() => CreditNote(DocumentSyntax.Ubl);

    /// <summary>A credit note declaring XRechnung, in the syntax you name.</summary>
    public EInvoiceBuilder CreditNote(DocumentSyntax syntax) =>
        Invoice(syntax).OfType(InvoiceTypeCodes.CreditNote);

    /// <summary>
    /// An invoice addressed to a public body by its Leitweg-ID, which is checked before it is written.
    /// </summary>
    /// <remarks>
    /// This is BT-10, and getting it wrong does not fail: the invoice is delivered to another authority.
    /// </remarks>
    /// <param name="leitwegId">The routing identifier, as the authority published it.</param>
    /// <param name="syntax">Which syntax it will be written in.</param>
    /// <exception cref="FormatException">The identifier is not a Leitweg-ID, or its check digits do not match.</exception>
    public EInvoiceBuilder InvoiceToPublicBody(string leitwegId, DocumentSyntax syntax) =>
        Invoice(syntax).WithBuyerReference(DeLeitwegId.Parse(leitwegId).ToString());

    /// <summary>The same, in UBL.</summary>
    /// <exception cref="FormatException">The identifier is not a Leitweg-ID, or its check digits do not match.</exception>
    public EInvoiceBuilder InvoiceToPublicBody(string leitwegId) =>
        InvoiceToPublicBody(leitwegId, DocumentSyntax.Ubl);

    /// <summary>Writes an invoice or credit note, in the syntax its profile is written in.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="invoice"/> is <c>null</c>.</exception>
    public string Write(EInvoice invoice) => _library.Write(invoice);

    /// <summary>Writes an invoice or credit note in the syntax you name.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="invoice"/> is <c>null</c>.</exception>
    public string Write(EInvoice invoice, DocumentFormat format) => _library.Write(invoice, format);

    /// <summary>
    /// Validates a document against the German rules and the EN 16931 ones they restrict.
    /// </summary>
    /// <remarks>Both ship with this library, so this works with nothing to fetch.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> is <c>null</c>.</exception>
    public ValidationReport Validate(string document) => _library.Validate(document);
}
