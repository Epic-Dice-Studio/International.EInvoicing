using International.EInvoicing.Building;
using International.EInvoicing.Configuration;
using International.EInvoicing.FacturX.Pdf;
using International.EInvoicing.Model;
using International.EInvoicing.Peppol;
using International.EInvoicing.Profiles;
using International.EInvoicing.Validation;

namespace International.EInvoicing.Countries.Japan;

/// <summary>
/// Everything Japan, from one object.
/// </summary>
/// <remarks>
/// <para>
/// Japan exchanges <b>Peppol PINT</b>, not BIS Billing — it is outside the European family, and the two
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
    Justification = "Everything Japan hangs off one object on purpose; a static member here would send "
        + "the caller back to remembering which type builds what, which is the problem this type solves.")]
public sealed class JapanEInvoicing
{
    private readonly EInvoicing _library;

    private JapanEInvoicing(EInvoicing library) => _library = library;

    /// <summary>The whole library underneath, for anything this shortcut does not cover.</summary>
    public EInvoicing Library => _library;

    /// <summary>A Japan library instance: the Peppol profiles Japan exchanges.</summary>
    public static JapanEInvoicing Create() => Create(pdf: null);

    /// <summary>The same, able to open hybrid PDFs.</summary>
    public static JapanEInvoicing Create(IPdfAttachmentReader? pdf) =>
        Create(library => library.AddDefaults().AddJapan(), pdf);

    /// <summary>The same, with anything else you want registered — the Peppol rules above all.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="configure"/> is <c>null</c>.</exception>
    public static JapanEInvoicing Create(Action<EInvoicingBuilder> configure) => Create(configure, null);

    /// <summary>The same, able to open hybrid PDFs.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="configure"/> is <c>null</c>.</exception>
    public static JapanEInvoicing Create(Action<EInvoicingBuilder> configure, IPdfAttachmentReader? pdf)
    {
        ArgumentNullException.ThrowIfNull(configure);

        return new JapanEInvoicing(EInvoicing.Create(configure, pdf));
    }

    /// <summary>The Norwegian side of a library instance you already have.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="library"/> is <c>null</c>.</exception>
    public static JapanEInvoicing Over(EInvoicing library)
    {
        ArgumentNullException.ThrowIfNull(library);

        return new JapanEInvoicing(library);
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
            .Create(syntax == DocumentSyntax.Cii ? JpProfiles.PintBilling : JpProfiles.PintBilling)
            .InCurrency("JPY")
            .ForPeppolPint()
            .Extend(invoice => invoice.TaxSchemeIdentifier = "VAT");

    /// <summary>A credit note declaring Peppol BIS Billing, in UBL.</summary>
    public EInvoiceBuilder CreditNote() => CreditNote(DocumentSyntax.Ubl);

    /// <summary>The same, in the syntax you name.</summary>
    public EInvoiceBuilder CreditNote(DocumentSyntax syntax) =>
        Invoice(syntax).OfType(InvoiceTypeCodes.CreditNote);

    /// <summary>
    /// A Japanese party, identified by its registration number.
    /// </summary>
    /// <remarks>
    /// Japan's qualified invoice system turns on the seller's registration number, which travels as the VAT
    /// identifier (BT-31) — the term EN 16931 gives it. The Japanese rules constrain how many times it may
    /// appear rather than its shape, so no check digit is verified here: this library does not invent
    /// validation its sources do not define.
    /// </remarks>
    /// <param name="party">The party being described.</param>
    /// <param name="registrationNumber">The registration number.</param>
    /// <param name="name">The party's legal name.</param>
    /// <exception cref="ArgumentNullException"><paramref name="party"/> is <c>null</c>.</exception>
    public PartyBuilder Describe(PartyBuilder party, string registrationNumber, string name)
    {
        ArgumentNullException.ThrowIfNull(party);

        return party.Named(name).WithVatIdentifier(registrationNumber);
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
