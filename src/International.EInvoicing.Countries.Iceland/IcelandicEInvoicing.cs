using International.EInvoicing.Building;
using International.EInvoicing.Configuration;
using International.EInvoicing.Countries.Iceland.Identifiers;
using International.EInvoicing.FacturX.Pdf;
using International.EInvoicing.Model;
using International.EInvoicing.Peppol;
using International.EInvoicing.Profiles;
using International.EInvoicing.Validation;

namespace International.EInvoicing.Countries.Iceland;

/// <summary>
/// Everything Icelandic, from one object.
/// </summary>
/// <remarks>
/// Iceland exchanges Peppol BIS Billing, with Icelandic rules that travel inside the Peppol rule set. What
/// this holds is the profile, the business process the network requires and EN 16931 does not, and the
/// kennitala in the scheme the Icelandic rules insist on — checked before it is written.
/// </remarks>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Performance",
    "CA1822:Mark members as static",
    Justification = "Everything Icelandic hangs off one object on purpose; a static member here would send "
        + "the caller back to remembering which type builds what, which is the problem this type solves.")]
public sealed class IcelandicEInvoicing
{
    private readonly EInvoicing _library;

    private IcelandicEInvoicing(EInvoicing library) => _library = library;

    /// <summary>The whole library underneath, for anything this shortcut does not cover.</summary>
    public EInvoicing Library => _library;

    /// <summary>An Icelandic library instance: the Peppol profiles Iceland exchanges.</summary>
    public static IcelandicEInvoicing Create() => Create(pdf: null);

    /// <summary>The same, able to open hybrid PDFs.</summary>
    public static IcelandicEInvoicing Create(IPdfAttachmentReader? pdf) =>
        Create(library => library.AddDefaults().AddIceland(), pdf);

    /// <summary>The same, with anything else you want registered — the Peppol rules above all.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="configure"/> is <c>null</c>.</exception>
    public static IcelandicEInvoicing Create(Action<EInvoicingBuilder> configure) => Create(configure, null);

    /// <summary>The same, able to open hybrid PDFs.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="configure"/> is <c>null</c>.</exception>
    public static IcelandicEInvoicing Create(Action<EInvoicingBuilder> configure, IPdfAttachmentReader? pdf)
    {
        ArgumentNullException.ThrowIfNull(configure);

        return new IcelandicEInvoicing(EInvoicing.Create(configure, pdf));
    }

    /// <summary>The Norwegian side of a library instance you already have.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="library"/> is <c>null</c>.</exception>
    public static IcelandicEInvoicing Over(EInvoicing library)
    {
        ArgumentNullException.ThrowIfNull(library);

        return new IcelandicEInvoicing(library);
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
            .Create(syntax == DocumentSyntax.Cii ? IsProfiles.PeppolBillingCii : IsProfiles.PeppolBillingUbl)
            .InCurrency("ISK")
            .ForPeppol();

    /// <summary>A credit note declaring Peppol BIS Billing, in UBL.</summary>
    public EInvoiceBuilder CreditNote() => CreditNote(DocumentSyntax.Ubl);

    /// <summary>The same, in the syntax you name.</summary>
    public EInvoiceBuilder CreditNote(DocumentSyntax syntax) =>
        Invoice(syntax).OfType(InvoiceTypeCodes.CreditNote);

    /// <summary>
    /// An Icelandic party, identified by its kennitala where the Icelandic rules look for it.
    /// </summary>
    /// <remarks>IS-R-002 and IS-R-004 are fatal: both parties need a legal entity identifier in scheme 0196.</remarks>
    /// <param name="party">The party being described.</param>
    /// <param name="identifier">The kennitala, however it is punctuated.</param>
    /// <param name="name">The party's legal name.</param>
    /// <exception cref="ArgumentNullException"><paramref name="party"/> is <c>null</c>.</exception>
    /// <exception cref="FormatException">The value is not a kennitala.</exception>
    public PartyBuilder Describe(PartyBuilder party, string identifier, string name)
    {
        ArgumentNullException.ThrowIfNull(party);

        IsKennitala kennitala = IsKennitala.Parse(identifier);

        return party
            .Named(name)
            .WithLegalRegistration(kennitala.Value, IsKennitala.Scheme)
            .WithElectronicAddress(kennitala.Value, IsKennitala.Scheme);
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
