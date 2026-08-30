using International.EInvoicing.Building;
using International.EInvoicing.Configuration;
using International.EInvoicing.Countries.Belgium.Identifiers;
using International.EInvoicing.FacturX.Pdf;
using International.EInvoicing.Model;
using International.EInvoicing.Peppol;
using International.EInvoicing.Profiles;
using International.EInvoicing.Validation;

namespace International.EInvoicing.Countries.Belgium;

/// <summary>
/// Everything Belgian, from one object.
/// </summary>
/// <remarks>
/// The Belgian mandate is Peppol BIS Billing rather than a Belgian format, so most of what this does is
/// making that fact easy to act on: the profile, the business process the network requires and EN 16931 does
/// not, the enterprise number in the scheme Peppol reserves for it, and the structured communication a
/// Belgian bank transfer is reconciled by.
/// </remarks>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Performance",
    "CA1822:Mark members as static",
    Justification = "Everything Belgian hangs off one object on purpose; a static member here would send the "
        + "caller back to remembering which type builds what, which is the problem this type exists to solve.")]
public sealed class BelgianEInvoicing
{
    private readonly EInvoicing _library;

    private BelgianEInvoicing(EInvoicing library) => _library = library;

    /// <summary>The whole library underneath, for anything this shortcut does not cover.</summary>
    public EInvoicing Library => _library;

    /// <summary>A Belgian library instance: Peppol BIS Billing in both syntaxes.</summary>
    /// <remarks>
    /// The Peppol rules declare no licence and are therefore fetched, not shipped. Use the overload taking a
    /// configuration and call <c>AddPeppolRulesFrom(directory)</c> once you have them.
    /// </remarks>
    public static BelgianEInvoicing Create() => Create(pdf: null);

    /// <summary>The same, able to open hybrid PDFs.</summary>
    /// <param name="pdf">
    /// A PDF reader. Reference <c>International.EInvoicing.FacturX.PdfSharp</c> for one.
    /// </param>
    public static BelgianEInvoicing Create(IPdfAttachmentReader? pdf) =>
        Create(belgium => belgium.AddDefaults().AddBelgium(), pdf);

    /// <summary>The same, with anything else you want registered — the Peppol rules above all.</summary>
    /// <example>
    /// <code>
    /// BelgianEInvoicing belgium = BelgianEInvoicing.Create(library => library
    ///     .AddDefaults()
    ///     .AddBelgium()
    ///     .AddPeppolRulesFrom("specs/peppol/rules"));
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException"><paramref name="configure"/> is <c>null</c>.</exception>
    public static BelgianEInvoicing Create(Action<EInvoicingBuilder> configure) => Create(configure, null);

    /// <summary>The same, able to open hybrid PDFs.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="configure"/> is <c>null</c>.</exception>
    public static BelgianEInvoicing Create(Action<EInvoicingBuilder> configure, IPdfAttachmentReader? pdf)
    {
        ArgumentNullException.ThrowIfNull(configure);

        return new BelgianEInvoicing(EInvoicing.Create(configure, pdf));
    }

    /// <summary>The Belgian side of a library instance you already have.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="library"/> is <c>null</c>.</exception>
    public static BelgianEInvoicing Over(EInvoicing library)
    {
        ArgumentNullException.ThrowIfNull(library);

        return new BelgianEInvoicing(library);
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

    /// <summary>
    /// An invoice declaring Peppol BIS Billing, in UBL, with the business process the network requires.
    /// </summary>
    public EInvoiceBuilder Invoice() => Invoice(DocumentSyntax.Ubl);

    /// <summary>The same, in the syntax you name.</summary>
    public EInvoiceBuilder Invoice(DocumentSyntax syntax) =>
        EInvoiceBuilder
            .Create(syntax == DocumentSyntax.Cii ? PeppolProfiles.BillingCii : PeppolProfiles.BillingUbl)
            .InCurrency("EUR")
            .ForPeppol();

    /// <summary>A credit note declaring Peppol BIS Billing, in UBL.</summary>
    public EInvoiceBuilder CreditNote() => CreditNote(DocumentSyntax.Ubl);

    /// <summary>The same, in the syntax you name.</summary>
    public EInvoiceBuilder CreditNote(DocumentSyntax syntax) =>
        Invoice(syntax).OfType(InvoiceTypeCodes.CreditNote);

    /// <summary>
    /// A Belgian party, addressed by its enterprise number in the scheme Peppol reserves for it.
    /// </summary>
    /// <remarks>The number is checked modulo 97 before it is written.</remarks>
    /// <param name="party">The party being described.</param>
    /// <param name="enterpriseNumber">The KBO/BCE number, however it is punctuated.</param>
    /// <param name="name">The party's legal name.</param>
    /// <exception cref="ArgumentNullException"><paramref name="party"/> is <c>null</c>.</exception>
    /// <exception cref="FormatException">The number does not satisfy its check digits.</exception>
    public PartyBuilder Describe(PartyBuilder party, string enterpriseNumber, string name)
    {
        ArgumentNullException.ThrowIfNull(party);

        BeEnterpriseNumber number = BeEnterpriseNumber.Parse(enterpriseNumber);

        return party
            .Named(name)
            .WithVatIdentifier(number.VatNumber)
            .WithElectronicAddress(number.Value, PeppolEndpointScheme.BelgianEnterprise);
    }

    /// <summary>The <c>+++/+++</c> reference a Belgian bank transfer is reconciled by.</summary>
    /// <param name="reference">Your own reference, at most ten digits.</param>
    /// <exception cref="ArgumentOutOfRangeException">The reference does not fit.</exception>
    public string StructuredCommunication(long reference) =>
        BeStructuredCommunication.ForInvoice(reference).ToString();

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
