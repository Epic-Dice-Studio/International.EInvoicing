using International.EInvoicing.Building;
using International.EInvoicing.Configuration;
using International.EInvoicing.Countries.Netherlands.Identifiers;
using International.EInvoicing.FacturX.Pdf;
using International.EInvoicing.Model;
using International.EInvoicing.Peppol;
using International.EInvoicing.Profiles;
using International.EInvoicing.Validation;

namespace International.EInvoicing.Countries.Netherlands;

/// <summary>
/// Everything Dutch, from one object.
/// </summary>
/// <remarks>
/// The Netherlands exchanges Peppol BIS Billing, with Dutch rules that travel inside the Peppol rule set.
/// What this holds is the profile, the business process the network requires and EN 16931 does not, and the
/// one thing the Dutch rules are strict about: both parties' legal entity identifiers must carry a KvK or OIN
/// scheme, or the invoice is refused.
/// </remarks>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Performance",
    "CA1822:Mark members as static",
    Justification = "Everything Dutch hangs off one object on purpose; a static member here would send "
        + "the caller back to remembering which type builds what, which is the problem this type solves.")]
public sealed class DutchEInvoicing
{
    private readonly EInvoicing _library;

    private DutchEInvoicing(EInvoicing library) => _library = library;

    /// <summary>The whole library underneath, for anything this shortcut does not cover.</summary>
    public EInvoicing Library => _library;

    /// <summary>A Dutch library instance: the Peppol profiles Netherlands exchanges.</summary>
    public static DutchEInvoicing Create() => Create(pdf: null);

    /// <summary>The same, able to open hybrid PDFs.</summary>
    public static DutchEInvoicing Create(IPdfAttachmentReader? pdf) =>
        Create(library => library.AddDefaults().AddNetherlands(), pdf);

    /// <summary>The same, with anything else you want registered — the Peppol rules above all.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="configure"/> is <c>null</c>.</exception>
    public static DutchEInvoicing Create(Action<EInvoicingBuilder> configure) => Create(configure, null);

    /// <summary>The same, able to open hybrid PDFs.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="configure"/> is <c>null</c>.</exception>
    public static DutchEInvoicing Create(Action<EInvoicingBuilder> configure, IPdfAttachmentReader? pdf)
    {
        ArgumentNullException.ThrowIfNull(configure);

        return new DutchEInvoicing(EInvoicing.Create(configure, pdf));
    }

    /// <summary>The Norwegian side of a library instance you already have.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="library"/> is <c>null</c>.</exception>
    public static DutchEInvoicing Over(EInvoicing library)
    {
        ArgumentNullException.ThrowIfNull(library);

        return new DutchEInvoicing(library);
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
            .Create(syntax == DocumentSyntax.Cii ? NlProfiles.PeppolBillingCii : NlProfiles.PeppolBillingUbl)
            .InCurrency("EUR")
            .ForPeppol();

    /// <summary>A credit note declaring Peppol BIS Billing, in UBL.</summary>
    public EInvoiceBuilder CreditNote() => CreditNote(DocumentSyntax.Ubl);

    /// <summary>The same, in the syntax you name.</summary>
    public EInvoiceBuilder CreditNote(DocumentSyntax syntax) =>
        Invoice(syntax).OfType(InvoiceTypeCodes.CreditNote);

    /// <summary>
    /// A Dutch party, with the legal entity identifier the Dutch rules require.
    /// </summary>
    /// <remarks>
    /// <c>NL-R-003</c> and <c>NL-R-005</c> are fatal: when the supplier is Dutch, both parties' legal entity
    /// identifiers must carry scheme <c>0106</c> (KvK) or <c>0190</c> (OIN). This puts it there.
    /// </remarks>
    /// <param name="party">The party being described.</param>
    /// <param name="identifier">The KvK or OIN number.</param>
    /// <param name="scheme"><see cref="NlLegalIdentifier.Kvk"/> or <see cref="NlLegalIdentifier.Oin"/>.</param>
    /// <param name="name">The party's legal name.</param>
    /// <exception cref="ArgumentNullException"><paramref name="party"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">The scheme is not one the Dutch rules accept.</exception>
    public PartyBuilder Describe(PartyBuilder party, string identifier, string scheme, string name)
    {
        ArgumentNullException.ThrowIfNull(party);

        if (!NlLegalIdentifier.IsAccepted(scheme))
        {
            throw new ArgumentException(
                $"NL-R-003 accepts only a KvK ({NlLegalIdentifier.Kvk}) or OIN ({NlLegalIdentifier.Oin}) "
                + $"scheme on a Dutch legal entity, not '{scheme}'.",
                nameof(scheme));
        }

        return party
            .Named(name)
            .WithLegalRegistration(identifier, scheme)
            .WithElectronicAddress(identifier, scheme);
    }

    /// <summary>The same, with a KvK number, which is what an ordinary Dutch company has.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="party"/> is <c>null</c>.</exception>
    public PartyBuilder Describe(PartyBuilder party, string kvkNumber, string name) =>
        Describe(party, kvkNumber, NlLegalIdentifier.Kvk, name);

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
