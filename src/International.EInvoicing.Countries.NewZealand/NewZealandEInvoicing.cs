using International.EInvoicing.Building;
using International.EInvoicing.Configuration;
using International.EInvoicing.Countries.NewZealand.Identifiers;
using International.EInvoicing.FacturX.Pdf;
using International.EInvoicing.Model;
using International.EInvoicing.Peppol;
using International.EInvoicing.Profiles;
using International.EInvoicing.Validation;

namespace International.EInvoicing.Countries.NewZealand;

/// <summary>
/// Everything New Zealand, from one object.
/// </summary>
/// <remarks>
/// <para>
/// New Zealand exchanges <b>Peppol PINT</b>, not BIS Billing — it is outside the European family, and the two
/// disagree about the business process as well as the profile. This holds the A-NZ specialisation, the PINT
/// process, and the ABN in the scheme Peppol reserves for it, checked before it is written.
/// </para>
/// <para>
/// The A-NZ jurisdiction rules do not run here: OpenPEPPOL publishes them as pre-compiled XSLT and this
/// library's engine executes Schematron. A document is read and mapped, and reported as unchecked rather
/// than passed. See <c>docs/standards/peppol-pint.md</c>.
/// </para>
/// </remarks>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Performance",
    "CA1822:Mark members as static",
    Justification = "Everything New Zealand hangs off one object on purpose; a static member here would send "
        + "the caller back to remembering which type builds what, which is the problem this type solves.")]
public sealed class NewZealandEInvoicing
{
    private readonly EInvoicing _library;

    private NewZealandEInvoicing(EInvoicing library) => _library = library;

    /// <summary>The whole library underneath, for anything this shortcut does not cover.</summary>
    public EInvoicing Library => _library;

    /// <summary>A New Zealand library instance: the Peppol profiles NewZealand exchanges.</summary>
    public static NewZealandEInvoicing Create() => Create(pdf: null);

    /// <summary>The same, able to open hybrid PDFs.</summary>
    public static NewZealandEInvoicing Create(IPdfAttachmentReader? pdf) =>
        Create(library => library.AddDefaults().AddNewZealand(), pdf);

    /// <summary>The same, with anything else you want registered — the Peppol rules above all.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="configure"/> is <c>null</c>.</exception>
    public static NewZealandEInvoicing Create(Action<EInvoicingBuilder> configure) => Create(configure, null);

    /// <summary>The same, able to open hybrid PDFs.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="configure"/> is <c>null</c>.</exception>
    public static NewZealandEInvoicing Create(Action<EInvoicingBuilder> configure, IPdfAttachmentReader? pdf)
    {
        ArgumentNullException.ThrowIfNull(configure);

        return new NewZealandEInvoicing(EInvoicing.Create(configure, pdf));
    }

    /// <summary>The Norwegian side of a library instance you already have.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="library"/> is <c>null</c>.</exception>
    public static NewZealandEInvoicing Over(EInvoicing library)
    {
        ArgumentNullException.ThrowIfNull(library);

        return new NewZealandEInvoicing(library);
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

    /// <summary>Reads whatever the stream holds, without blocking while it arrives.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> is <c>null</c>.</exception>
    /// <exception cref="OperationCanceledException">The token was cancelled while the document arrived.</exception>
    public Task<DocumentResult> ReadAsync(Stream document, CancellationToken cancellationToken = default) =>
        _library.ReadAsync(document, cancellationToken);

    /// <summary>Reads whatever the file holds.</summary>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty.</exception>
    public DocumentResult ReadFile(string path) => _library.ReadFile(path);

    /// <summary>An invoice declaring Peppol BIS Billing, in UBL, with the business process the network requires.</summary>
    public EInvoiceBuilder Invoice() => Invoice(DocumentSyntax.Ubl);

    /// <summary>The same, in the syntax you name.</summary>
    public EInvoiceBuilder Invoice(DocumentSyntax syntax) =>
        EInvoiceBuilder
            .Create(syntax == DocumentSyntax.Cii ? NzProfiles.PintBilling : NzProfiles.PintBilling)
            .InCurrency("NZD")
            .ForPeppolPint();

    /// <summary>A credit note declaring Peppol BIS Billing, in UBL.</summary>
    public EInvoiceBuilder CreditNote() => CreditNote(DocumentSyntax.Ubl);

    /// <summary>The same, in the syntax you name.</summary>
    public EInvoiceBuilder CreditNote(DocumentSyntax syntax) =>
        Invoice(syntax).OfType(InvoiceTypeCodes.CreditNote);

    /// <summary>
    /// A New Zealand party, identified by its NZBN where Peppol looks for it.
    /// </summary>
    /// <remarks>IS-R-002 and IS-R-004 are fatal: both parties need a legal entity identifier in scheme 0196.</remarks>
    /// <param name="party">The party being described.</param>
    /// <param name="identifier">The NZBN, however it is spaced.</param>
    /// <param name="name">The party's legal name.</param>
    /// <exception cref="ArgumentNullException"><paramref name="party"/> is <c>null</c>.</exception>
    /// <exception cref="FormatException">The value is not an NZBN.</exception>
    public PartyBuilder Describe(PartyBuilder party, string identifier, string name)
    {
        ArgumentNullException.ThrowIfNull(party);

        NzNzbn nzbn = NzNzbn.Parse(identifier);

        return party
            .Named(name)
            .WithLegalRegistration(nzbn.Value)
            .WithElectronicAddress(nzbn.Value, NzNzbn.Scheme);
    }

    /// <summary>Writes an invoice or credit note, in the syntax its profile is written in.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="invoice"/> is <c>null</c>.</exception>
    public string Write(EInvoice invoice) => _library.Write(invoice);

    /// <summary>Writes an invoice or credit note in the syntax you name.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="invoice"/> is <c>null</c>.</exception>
    public string Write(EInvoice invoice, DocumentFormat format) => _library.Write(invoice, format);

    /// <summary>Validates a document against every rule set registered for it.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> is <c>null</c>.</exception>
    public ValidationReport Validate(string document) => _library.Validate(document);
}
