using International.EInvoicing.Building;
using International.EInvoicing.Configuration;
using International.EInvoicing.Countries.France.EReporting.Building;
using International.EInvoicing.Countries.France.EReporting.Model;
using International.EInvoicing.Countries.France.EReporting.Reading;
using International.EInvoicing.Countries.France.EReporting.Writing;
using International.EInvoicing.Countries.France.Invoicing;
using International.EInvoicing.Countries.France.Lifecycle;
using International.EInvoicing.Diagnostics;
using International.EInvoicing.Documents;
using International.EInvoicing.FacturX.Pdf;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;
using International.EInvoicing.Validation;

namespace International.EInvoicing.Countries.France;

/// <summary>
/// Everything French, from one object.
/// </summary>
/// <remarks>
/// <para>
/// The 2026 reform asks a French integration for four documents — invoices, credit notes, lifecycle
/// statuses and e-reporting transmissions — and they are genuinely four documents: two invoice syntaxes, a
/// UN/CEFACT acknowledgement vocabulary, and a report that carries no XML namespace at all. Knowing which
/// entry point builds which is work nobody should have to do to send an invoice.
/// </para>
/// <para>
/// So this reads all four without being told which arrived, writes all four, validates against the French
/// rules when they are present, and hands out builders that already carry what France requires. Everything
/// underneath stays reachable: this is a shorter way in, not a wall.
/// </para>
/// </remarks>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Performance",
    "CA1822:Mark members as static",
    Justification = "Everything French hangs off one object on purpose; a static member here would send the "
        + "caller back to remembering which type builds what, which is the problem this type exists to solve.")]
public sealed class FrenchEInvoicing
{
    private readonly EInvoicing _library;
    private readonly FrEReportReader _eReportReader;
    private readonly FrEReportWriter _eReportWriter = new();

    private FrenchEInvoicing(EInvoicing library, EInvoicingOptions options)
    {
        _library = library;
        _eReportReader = new FrEReportReader(options);
    }

    /// <summary>The whole library underneath, for anything this shortcut does not cover.</summary>
    public EInvoicing Library => _library;

    /// <summary>
    /// A French library instance: the two invoice syntaxes, Factur-X, lifecycle messages and e-reporting.
    /// </summary>
    public static FrenchEInvoicing Create() => Create(pdf: null);

    /// <summary>
    /// A French library instance able to open Factur-X invoices.
    /// </summary>
    /// <param name="pdf">
    /// A PDF reader. Reference <c>International.EInvoicing.FacturX.PdfSharp</c> for one.
    /// </param>
    public static FrenchEInvoicing Create(IPdfAttachmentReader? pdf) =>
        Create(france => france.AddDefaults().AddFrance(), pdf);

    /// <summary>The same, with anything else you want registered — the French rule sets above all.</summary>
    /// <example>
    /// <code>
    /// FrenchEInvoicing france = FrenchEInvoicing.Create(library => library
    ///     .AddDefaults()
    ///     .AddFrance()
    ///     .AddRulesFromFile(DocumentSyntax.Ubl, "…EXTENDED-CTC-FR-UBL….sch", "EXTENDED CTC FR", "1.4.0.03"));
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException"><paramref name="configure"/> is <c>null</c>.</exception>
    public static FrenchEInvoicing Create(Action<EInvoicingBuilder> configure) => Create(configure, null);

    /// <summary>The same, able to open Factur-X invoices.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="configure"/> is <c>null</c>.</exception>
    public static FrenchEInvoicing Create(Action<EInvoicingBuilder> configure, IPdfAttachmentReader? pdf)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new EInvoicingBuilder();
        configure(builder);

        EInvoicingOptions options = builder.BuildOptions();

        return new FrenchEInvoicing(
            new EInvoicing(options, new ProfileResolver(builder.BuildRegistry()), builder.BuildRuleSets(), pdf),
            options);
    }

    /// <summary>The French side of a library instance you already have.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="library"/> is <c>null</c>.</exception>
    public static FrenchEInvoicing Over(EInvoicing library) => Over(library, new EInvoicingOptions());

    /// <summary>The French side of a library instance you already have, reading with your own options.</summary>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    public static FrenchEInvoicing Over(EInvoicing library, EInvoicingOptions options)
    {
        ArgumentNullException.ThrowIfNull(library);
        ArgumentNullException.ThrowIfNull(options);

        return new FrenchEInvoicing(library, options);
    }

    /// <summary>
    /// Reads whatever arrived: an invoice, a credit note, a lifecycle status, or an e-reporting transmission.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> is <c>null</c>.</exception>
    public FrenchDocument Read(string document)
    {
        ArgumentNullException.ThrowIfNull(document);

        // Flux 10 carries no namespace, so nothing but its root element says what it is.
        if (LooksLikeEReport(document))
        {
            ParseResult<FrEReport> report = _eReportReader.Read(document);

            return new FrenchDocument
            {
                Kind = report.IsUsable ? FrenchDocumentKind.EReport : FrenchDocumentKind.Unknown,
                EReport = report.Value,
                Diagnostics = report.Diagnostics,
            };
        }

        return From(_library.Read(document));
    }

    /// <summary>Reads whatever the bytes hold, a Factur-X PDF included.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> is <c>null</c>.</exception>
    public FrenchDocument Read(byte[] document)
    {
        ArgumentNullException.ThrowIfNull(document);

        return FacturX.FacturXReader.LooksLikePdf(document)
            ? From(_library.Read(document))
            : Read(System.Text.Encoding.UTF8.GetString(document));
    }

    /// <summary>Reads whatever the stream holds. The stream is left open.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> is <c>null</c>.</exception>
    public FrenchDocument Read(Stream document)
    {
        ArgumentNullException.ThrowIfNull(document);

        return Read(DocumentStreams.ReadAll(document));
    }

    /// <summary>Reads whatever the stream holds, without blocking while it arrives.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> is <c>null</c>.</exception>
    /// <exception cref="OperationCanceledException">The token was cancelled while the document arrived.</exception>
    public async Task<FrenchDocument> ReadAsync(Stream document, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        return Read(await DocumentStreams.ReadAllAsync(document, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>Reads whatever the file holds.</summary>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty.</exception>
    public FrenchDocument ReadFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        return Read(File.ReadAllBytes(path));
    }

    /// <summary>
    /// An invoice builder that already carries what France requires: the profile, the invoicing case, and
    /// the three mandatory mentions.
    /// </summary>
    /// <param name="syntax">Which syntax it will be written in. UBL unless said otherwise.</param>
    /// <param name="businessProcess">The invoicing case (BT-23). An ordinary invoice unless said otherwise.</param>
    /// <exception cref="ArgumentException">The invoicing case is not one the published rules accept.</exception>
    public EInvoiceBuilder Invoice(DocumentSyntax syntax, string businessProcess) =>
        EInvoiceBuilder
            .Create(syntax == DocumentSyntax.Cii ? FrProfiles.ExtendedCtcFrCii : FrProfiles.ExtendedCtcFrUbl)
            .InCurrency("EUR")
            .ForFrance(businessProcess);

    /// <summary>A credit note, which is the same document with the code that says so.</summary>
    /// <param name="syntax">Which syntax it will be written in.</param>
    /// <param name="businessProcess">The invoicing case (BT-23).</param>
    /// <exception cref="ArgumentException">The invoicing case is not one the published rules accept.</exception>
    public EInvoiceBuilder CreditNote(DocumentSyntax syntax, string businessProcess) =>
        Invoice(syntax, businessProcess).OfType(InvoiceTypeCodes.CreditNote);

    /// <summary>An ordinary French invoice in UBL, ready for its number, its parties and its lines.</summary>
    public EInvoiceBuilder Invoice() => Invoice(DocumentSyntax.Ubl, FrBusinessProcess.Invoice);

    /// <summary>An ordinary French credit note in UBL.</summary>
    public EInvoiceBuilder CreditNote() => CreditNote(DocumentSyntax.Ubl, FrBusinessProcess.Invoice);

    /// <summary>The buyer reports a status — taken in charge, approved, disputed, refused, payment sent.</summary>
    /// <exception cref="ArgumentException"><paramref name="siren"/> is empty.</exception>
    public FrCdar StatusFromBuyer(string siren) => FrCdar.FromBuyer(siren, null);

    /// <summary>The buyer reports a status, under the name it trades as.</summary>
    /// <exception cref="ArgumentException"><paramref name="siren"/> is empty.</exception>
    public FrCdar StatusFromBuyer(string siren, string? name) => FrCdar.FromBuyer(siren, name);

    /// <summary>The seller reports a status — a collection, above all.</summary>
    /// <exception cref="ArgumentException"><paramref name="siren"/> is empty.</exception>
    public FrCdar StatusFromSeller(string siren) => FrCdar.FromSeller(siren, null);

    /// <summary>The seller reports a status, under the name it trades as.</summary>
    /// <exception cref="ArgumentException"><paramref name="siren"/> is empty.</exception>
    public FrCdar StatusFromSeller(string siren, string? name) => FrCdar.FromSeller(siren, name);

    /// <summary>A platform reports a status — filed, received, made available, rejected.</summary>
    /// <exception cref="ArgumentException"><paramref name="platformIdentifier"/> is empty.</exception>
    public FrCdar StatusFromPlatform(string platformIdentifier) =>
        FrCdar.FromPlatform(platformIdentifier, null);

    /// <summary>A platform reports a status, under its name.</summary>
    /// <exception cref="ArgumentException"><paramref name="platformIdentifier"/> is empty.</exception>
    public FrCdar StatusFromPlatform(string platformIdentifier, string? name) =>
        FrCdar.FromPlatform(platformIdentifier, name);

    /// <summary>An e-reporting transmission of transactions over a period — flux 10.1 and 10.3.</summary>
    /// <exception cref="ArgumentException">The period ends before it starts.</exception>
    public FrTransactionsBuilder ReportTransactions(DateOnly from, DateOnly to) =>
        FrEReporting.Transactions(from, to);

    /// <summary>An e-reporting transmission of payments over a period — flux 10.2 and 10.4.</summary>
    /// <exception cref="ArgumentException">The period ends before it starts.</exception>
    public FrPaymentsBuilder ReportPayments(DateOnly from, DateOnly to) => FrEReporting.Payments(from, to);

    /// <summary>Writes an invoice or credit note, in the syntax its profile is written in.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="invoice"/> is <c>null</c>.</exception>
    public string Write(EInvoice invoice) => _library.Write(invoice);

    /// <summary>Writes an invoice or credit note in the syntax you name.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="invoice"/> is <c>null</c>.</exception>
    public string Write(EInvoice invoice, DocumentFormat format) => _library.Write(invoice, format);

    /// <summary>Writes a lifecycle status message.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="status"/> is <c>null</c>.</exception>
    public string Write(LifecycleStatusMessage status) => _library.Write(status);

    /// <summary>Writes an e-reporting transmission.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="report"/> is <c>null</c>.</exception>
    public string Write(FrEReport report) => _eReportWriter.WriteToString(report);

    /// <summary>
    /// Validates a document against every rule set registered for it.
    /// </summary>
    /// <remarks>
    /// The French artefacts declare no licence, so they are fetched rather than shipped: without them this
    /// checks EN 16931 and says plainly that the French rules did not run. See
    /// <c>docs/standards/country-fr.md</c>.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> is <c>null</c>.</exception>
    public ValidationReport Validate(string document) => _library.Validate(document);

    private static bool LooksLikeEReport(string document)
    {
        ReadOnlySpan<char> start = document.AsSpan().TrimStart();

        // Skip the declaration, if any, then look for the one root element flux 10 uses.
        if (start.StartsWith("<?xml", StringComparison.Ordinal))
        {
            int end = start.IndexOf("?>", StringComparison.Ordinal);
            start = end < 0 ? start : start[(end + 2)..].TrimStart();
        }

        return start.StartsWith("<Report>", StringComparison.Ordinal)
            || start.StartsWith("<Report ", StringComparison.Ordinal);
    }

    private static FrenchDocument From(DocumentResult result) => new()
    {
        Kind = result switch
        {
            { LifecycleStatus: not null } => FrenchDocumentKind.LifecycleStatus,
            { IsCreditNote: true, Invoice: not null } => FrenchDocumentKind.CreditNote,
            { Invoice: not null } => FrenchDocumentKind.Invoice,
            _ => FrenchDocumentKind.Unknown,
        },
        Invoice = result.Invoice,
        LifecycleStatus = result.LifecycleStatus,
        Diagnostics = result.Diagnostics,
        Profile = result.Profile,
    };
}
