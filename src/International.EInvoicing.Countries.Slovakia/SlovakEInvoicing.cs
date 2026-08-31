using International.EInvoicing.Building;
using International.EInvoicing.Configuration;
using International.EInvoicing.FacturX.Pdf;
using International.EInvoicing.Model;
using International.EInvoicing.Peppol;
using International.EInvoicing.Peppol.TaxData;
using International.EInvoicing.Peppol.TaxData.Model;
using International.EInvoicing.Peppol.TaxData.Writing;
using International.EInvoicing.Profiles;
using International.EInvoicing.Validation;

namespace International.EInvoicing.Countries.Slovakia;

/// <summary>
/// Everything Slovak, from one object.
/// </summary>
/// <remarks>
/// <para>
/// Slovakia's B2B mandate starts on <b>1 January 2027</b>, and it has two halves. The invoice travels
/// between the parties as Peppol BIS Billing 3.0. Within fifteen minutes, a <b>tax data document</b> about it
/// goes to the financial administration — a different document, with its own identifier and its own 88
/// published assertions.
/// </para>
/// <para>
/// Sending it is transport, which this library does not do. Building it correctly is not, and that is what
/// <see cref="TaxDataFor(EInvoice, string, string)"/> and <see cref="Write(PeppolTaxData)"/> are for.
/// </para>
/// </remarks>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Performance",
    "CA1822:Mark members as static",
    Justification = "Everything Slovak hangs off one object on purpose; a static member here would send the "
        + "caller back to remembering which type builds what, which is the problem this type solves.")]
public sealed class SlovakEInvoicing
{
    private readonly EInvoicing _library;

    private SlovakEInvoicing(EInvoicing library) => _library = library;

    /// <summary>The whole library underneath, for anything this shortcut does not cover.</summary>
    public EInvoicing Library => _library;

    /// <summary>A Slovak library instance: the Peppol profiles the mandate exchanges.</summary>
    public static SlovakEInvoicing Create() => Create(pdf: null);

    /// <summary>The same, able to open hybrid PDFs.</summary>
    public static SlovakEInvoicing Create(IPdfAttachmentReader? pdf) =>
        Create(library => library.AddDefaults().AddSlovakia(), pdf);

    /// <summary>The same, with anything else you want registered — the Peppol rules above all.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="configure"/> is <c>null</c>.</exception>
    public static SlovakEInvoicing Create(Action<EInvoicingBuilder> configure) => Create(configure, null);

    /// <summary>The same, able to open hybrid PDFs.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="configure"/> is <c>null</c>.</exception>
    public static SlovakEInvoicing Create(Action<EInvoicingBuilder> configure, IPdfAttachmentReader? pdf)
    {
        ArgumentNullException.ThrowIfNull(configure);

        return new SlovakEInvoicing(EInvoicing.Create(configure, pdf));
    }

    /// <summary>The Slovak side of a library instance you already have.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="library"/> is <c>null</c>.</exception>
    public static SlovakEInvoicing Over(EInvoicing library)
    {
        ArgumentNullException.ThrowIfNull(library);

        return new SlovakEInvoicing(library);
    }

    /// <summary>An invoice declaring Peppol BIS Billing, in UBL, with the business process the network requires.</summary>
    public EInvoiceBuilder Invoice() => Invoice(DocumentSyntax.Ubl);

    /// <summary>The same, in the syntax you name.</summary>
    public EInvoiceBuilder Invoice(DocumentSyntax syntax) =>
        EInvoiceBuilder
            .Create(syntax == DocumentSyntax.Cii ? SkProfiles.PeppolBillingCii : SkProfiles.PeppolBillingUbl)
            .InCurrency("EUR")
            .ForPeppol();

    /// <summary>A credit note declaring Peppol BIS Billing, in UBL.</summary>
    public EInvoiceBuilder CreditNote() => CreditNote(DocumentSyntax.Ubl);

    /// <summary>The same, in the syntax you name.</summary>
    public EInvoiceBuilder CreditNote(DocumentSyntax syntax) =>
        Invoice(syntax).OfType(InvoiceTypeCodes.CreditNote);

    /// <summary>
    /// The tax data document reporting an invoice, with the parts only the caller knows left to fill in.
    /// </summary>
    /// <remarks>
    /// What is filled in here is what follows from the invoice and from the rules: the identifiers, the time
    /// of issue with its offset, and a domestic sale reported by the sender's own service provider. What is
    /// not is the tax authority and the two endpoints, which are the network's business rather than the
    /// document's — set them before writing.
    /// </remarks>
    /// <param name="invoice">The invoice being reported.</param>
    /// <param name="uuid">This report's own identifier (TDT-003).</param>
    /// <param name="reportedDocumentUuid">The reported document's identifier (TDT-017).</param>
    /// <exception cref="ArgumentNullException"><paramref name="invoice"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">An identifier is empty.</exception>
    public PeppolTaxData TaxDataFor(EInvoice invoice, string uuid, string reportedDocumentUuid) =>
        TaxDataFor(invoice, uuid, reportedDocumentUuid, TimeProvider.System);

    /// <summary>The same, taking the time of issue from a clock of your own.</summary>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">An identifier is empty.</exception>
    public PeppolTaxData TaxDataFor(EInvoice invoice, string uuid, string reportedDocumentUuid, TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(invoice);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentException.ThrowIfNullOrWhiteSpace(uuid);
        ArgumentException.ThrowIfNullOrWhiteSpace(reportedDocumentUuid);

        return new PeppolTaxData
        {
            Jurisdiction = PeppolTaxDataJurisdiction.Slovakia,
            Uuid = uuid,
            ReportedDocumentUuid = reportedDocumentUuid,
            IssuedAt = clock.GetLocalNow(),
            TaxDataTypeCode = "S",
            DocumentScope = "D",
            ReporterRole = "C2",
            ReportedDocument = invoice,
        };
    }

    /// <summary>Reads whatever arrived — an invoice, a credit note, or a PDF carrying one.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> is <c>null</c>.</exception>
    public DocumentResult Read(string document) => _library.Read(document);

    /// <summary>Reads whatever the bytes hold.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> is <c>null</c>.</exception>
    public DocumentResult Read(byte[] document) => _library.Read(document);

    /// <summary>Reads whatever the stream holds. The stream is left open.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> is <c>null</c>.</exception>
    public DocumentResult Read(Stream document) => _library.Read(document);

    /// <summary>Reads whatever the file holds.</summary>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty.</exception>
    public DocumentResult ReadFile(string path) => _library.ReadFile(path);

    /// <summary>Writes an invoice or credit note, in the syntax its profile is written in.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="invoice"/> is <c>null</c>.</exception>
    public string Write(EInvoice invoice) => _library.Write(invoice);

    /// <summary>Writes an invoice or credit note in the syntax you name.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="invoice"/> is <c>null</c>.</exception>
    public string Write(EInvoice invoice, DocumentFormat format) => _library.Write(invoice, format);

    /// <summary>Writes a tax data document.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="taxData"/> is <c>null</c>.</exception>
    public string Write(PeppolTaxData taxData) => new PeppolTaxDataWriter().WriteToString(taxData);

    /// <summary>What the registered rules say about a document.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> is <c>null</c>.</exception>
    public ValidationReport Validate(string document) => _library.Validate(document);
}
