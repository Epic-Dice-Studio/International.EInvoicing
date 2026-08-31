using International.EInvoicing.Building;
using International.EInvoicing.Configuration;
using International.EInvoicing.FacturX.Pdf;
using International.EInvoicing.Model;
using International.EInvoicing.Peppol;
using International.EInvoicing.Profiles;
using International.EInvoicing.Validation;

namespace International.EInvoicing.Countries.Singapore;

/// <summary>
/// Everything Singapore, from one object.
/// </summary>
/// <remarks>
/// <para>
/// Singapore exchanges <b>Peppol PINT</b>, not BIS Billing — it is outside the European family, and the two
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
    Justification = "Everything Singapore hangs off one object on purpose; a static member here would send "
        + "the caller back to remembering which type builds what, which is the problem this type solves.")]
public sealed class SingaporeEInvoicing
{
    private readonly EInvoicing _library;

    private SingaporeEInvoicing(EInvoicing library) => _library = library;

    /// <summary>The whole library underneath, for anything this shortcut does not cover.</summary>
    public EInvoicing Library => _library;

    /// <summary>A Singapore library instance: the Peppol profiles Singapore exchanges.</summary>
    public static SingaporeEInvoicing Create() => Create(pdf: null);

    /// <summary>The same, able to open hybrid PDFs.</summary>
    public static SingaporeEInvoicing Create(IPdfAttachmentReader? pdf) =>
        Create(library => library.AddDefaults().AddSingapore(), pdf);

    /// <summary>The same, with anything else you want registered — the Peppol rules above all.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="configure"/> is <c>null</c>.</exception>
    public static SingaporeEInvoicing Create(Action<EInvoicingBuilder> configure) => Create(configure, null);

    /// <summary>The same, able to open hybrid PDFs.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="configure"/> is <c>null</c>.</exception>
    public static SingaporeEInvoicing Create(Action<EInvoicingBuilder> configure, IPdfAttachmentReader? pdf)
    {
        ArgumentNullException.ThrowIfNull(configure);

        return new SingaporeEInvoicing(EInvoicing.Create(configure, pdf));
    }

    /// <summary>The Norwegian side of a library instance you already have.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="library"/> is <c>null</c>.</exception>
    public static SingaporeEInvoicing Over(EInvoicing library)
    {
        ArgumentNullException.ThrowIfNull(library);

        return new SingaporeEInvoicing(library);
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
            .Create(syntax == DocumentSyntax.Cii ? SgProfiles.PintBilling : SgProfiles.PintBilling)
            .InCurrency("SGD")
            .ForPeppolPint()
            .Extend(invoice => invoice.TaxSchemeIdentifier = "GST");

    /// <summary>A credit note declaring Peppol BIS Billing, in UBL.</summary>
    public EInvoiceBuilder CreditNote() => CreditNote(DocumentSyntax.Ubl);

    /// <summary>The same, in the syntax you name.</summary>
    public EInvoiceBuilder CreditNote(DocumentSyntax syntax) =>
        Invoice(syntax).OfType(InvoiceTypeCodes.CreditNote);

    /// <summary>
    /// The scheme a Singaporean party's UEN is declared under is <b>not</b> set for you.
    /// </summary>
    /// <remarks>
    /// Every other country package here writes its legal identifier into the scheme its own rules name. For
    /// Singapore this library has no such source: the Singaporean PINT rules constrain arithmetic, decimals
    /// and GST terminology, and say nothing about an identifier scheme, and the EAS code list it ships
    /// carries codes without meanings. Rather than guess one — a wrong scheme identifier is as wrong as a
    /// wrong profile — describe the party yourself:
    /// <code>
    /// singapore.Invoice().From(seller => seller
    ///     .Named("Supplier Pte Ltd")
    ///     .WithElectronicAddress(uen, scheme));   // the scheme your access point tells you to use
    /// </code>
    /// It goes in here the day the scheme can be read from something authoritative.
    /// </remarks>
    public static string WhyThereIsNoDescribe =>
        "Singapore's own rules name no identifier scheme, and this library does not guess identifiers.";

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
