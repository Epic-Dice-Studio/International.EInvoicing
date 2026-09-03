using System.Xml.Linq;
using International.EInvoicing.Cdar;
using International.EInvoicing.Cdar.Reading;
using International.EInvoicing.Cdar.Writing;
using International.EInvoicing.Cii;
using International.EInvoicing.Cii.Reading;
using International.EInvoicing.Cii.Writing;
using International.EInvoicing.Configuration;
using International.EInvoicing.Diagnostics;
using International.EInvoicing.Documents;
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
    private readonly IReadOnlyList<IDocumentRuleSet> _ruleSets;

    /// <summary>
    /// Assembles the facade from its parts. Prefer <see cref="Create(Action{EInvoicingBuilder}, IPdfAttachmentReader)"/>,
    /// or let a container do it; this exists so a container can.
    /// </summary>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    public EInvoicing(
        EInvoicingOptions options,
        IProfileResolver profiles,
        IEnumerable<IDocumentRuleSet> ruleSets)
        : this(options, profiles, ruleSets, null)
    {
    }

    /// <summary>The same, able to open hybrid PDFs.</summary>
    /// <exception cref="ArgumentNullException">An argument other than <paramref name="pdf"/> is <c>null</c>.</exception>
    public EInvoicing(
        EInvoicingOptions options,
        IProfileResolver profiles,
        IEnumerable<IDocumentRuleSet> ruleSets,
        IPdfAttachmentReader? pdf)
        : this(options, profiles, ruleSets, DocumentHandlers.CreateDefault(options, profiles), pdf)
    {
    }

    /// <summary>
    /// Assembles the facade over the readers and writers you name — the ones a container registered, which
    /// is how your own take the place of the built-in ones.
    /// </summary>
    /// <exception cref="ArgumentNullException">An argument other than <paramref name="pdf"/> is <c>null</c>.</exception>
    public EInvoicing(
        EInvoicingOptions options,
        IProfileResolver profiles,
        IEnumerable<IDocumentRuleSet> ruleSets,
        DocumentHandlers handlers,
        IPdfAttachmentReader? pdf = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(profiles);
        ArgumentNullException.ThrowIfNull(ruleSets);
        ArgumentNullException.ThrowIfNull(handlers);

        _options = options;
        _pdf = pdf;
        _ruleSets = [.. ruleSets];
        Profiles = profiles;
        Handlers = handlers;
    }

    /// <summary>Which reader and writer this instance uses for each syntax.</summary>
    public DocumentHandlers Handlers { get; }

    /// <summary>The UBL reader, for a caller that already knows what it holds.</summary>
    /// <exception cref="InvalidOperationException">Nothing is registered to read UBL.</exception>
    public IDocumentReader<EInvoice> Ubl => Required(Handlers.InvoiceReaderFor(DocumentSyntax.Ubl), "read UBL");

    /// <summary>The CII reader.</summary>
    /// <exception cref="InvalidOperationException">Nothing is registered to read CII.</exception>
    public IDocumentReader<EInvoice> Cii => Required(Handlers.InvoiceReaderFor(DocumentSyntax.Cii), "read CII");

    /// <summary>The lifecycle message reader.</summary>
    /// <exception cref="InvalidOperationException">Nothing is registered to read lifecycle messages.</exception>
    public IDocumentReader<LifecycleStatusMessage> Lifecycle =>
        Required(Handlers.LifecycleReader(), "read lifecycle messages");

    /// <summary>The UBL writer.</summary>
    /// <exception cref="InvalidOperationException">Nothing is registered to write UBL.</exception>
    public IDocumentWriter<EInvoice> UblWriter => Required(Handlers.InvoiceWriterFor(DocumentSyntax.Ubl), "write UBL");

    /// <summary>The CII writer.</summary>
    /// <exception cref="InvalidOperationException">Nothing is registered to write CII.</exception>
    public IDocumentWriter<EInvoice> CiiWriter => Required(Handlers.InvoiceWriterFor(DocumentSyntax.Cii), "write CII");

    /// <summary>The lifecycle message writer.</summary>
    /// <exception cref="InvalidOperationException">Nothing is registered to write lifecycle messages.</exception>
    public IDocumentWriter<LifecycleStatusMessage> LifecycleWriter =>
        Required(Handlers.LifecycleWriter(), "write lifecycle messages");

    /// <summary>
    /// The reader for a UBL <c>ApplicationResponse</c> — the Peppol Invoice Response and Message Level
    /// Response, which say what happened to a document rather than what is owed.
    /// </summary>
    public IDocumentReader<LifecycleStatusMessage> UblResponse =>
        Required(Handlers.LifecycleReaderFor(DocumentSyntax.Ubl), "read UBL application responses");

    /// <summary>The writer for a UBL <c>ApplicationResponse</c>.</summary>
    public IDocumentWriter<LifecycleStatusMessage> UblResponseWriter =>
        Required(Handlers.LifecycleWriterFor(DocumentSyntax.Ubl), "write UBL application responses");

    /// <summary>The reader for a UBL <c>DespatchAdvice</c> — what was actually sent.</summary>
    public IDocumentReader<DespatchAdvice> UblDespatchAdvice =>
        Required(Handlers.DespatchAdviceReaderFor(DocumentSyntax.Ubl), "read UBL despatch advices");

    /// <summary>The writer for a UBL <c>DespatchAdvice</c>.</summary>
    public IDocumentWriter<DespatchAdvice> UblDespatchAdviceWriter =>
        Required(Handlers.DespatchAdviceWriterFor(DocumentSyntax.Ubl), "write UBL despatch advices");

    /// <summary>The reader for a UBL <c>Order</c> — what the buyer asked for.</summary>
    public IDocumentReader<Order> UblOrder =>
        Required(Handlers.OrderReaderFor(DocumentSyntax.Ubl), "read UBL orders");

    /// <summary>The writer for a UBL <c>Order</c>.</summary>
    public IDocumentWriter<Order> UblOrderWriter =>
        Required(Handlers.OrderWriterFor(DocumentSyntax.Ubl), "write UBL orders");

    /// <summary>The reader for a UBL <c>OrderResponse</c> — the seller's answer to an order.</summary>
    public IDocumentReader<OrderResponse> UblOrderResponse =>
        Required(Handlers.OrderResponseReaderFor(DocumentSyntax.Ubl), "read UBL order responses");

    /// <summary>The writer for a UBL <c>OrderResponse</c>.</summary>
    public IDocumentWriter<OrderResponse> UblOrderResponseWriter =>
        Required(Handlers.OrderResponseWriterFor(DocumentSyntax.Ubl), "write UBL order responses");

    /// <summary>The reader for a UBL <c>OrderCancellation</c>.</summary>
    public IDocumentReader<OrderCancellation> UblOrderCancellation =>
        Required(Handlers.OrderCancellationReaderFor(DocumentSyntax.Ubl), "read UBL order cancellations");

    /// <summary>The writer for a UBL <c>OrderCancellation</c>.</summary>
    public IDocumentWriter<OrderCancellation> UblOrderCancellationWriter =>
        Required(Handlers.OrderCancellationWriterFor(DocumentSyntax.Ubl), "write UBL order cancellations");

    private static THandler Required<THandler>(THandler? handler, string what)
        where THandler : class =>
        handler ?? throw new InvalidOperationException(
            $"Nothing registered can {what}. Add the package that does — AddUbl(), AddCii(), AddCdar() — or "
            + "register your own.");

    /// <summary>How declared profiles are resolved, and what this instance implements.</summary>
    public IProfileResolver Profiles { get; }

    /// <summary>
    /// Every profile this instance knows, for a caller asking what it supports before handing it a document.
    /// </summary>
    public IReadOnlyCollection<Profile> KnownProfiles =>
        Profiles is ProfileResolver resolver ? resolver.Registry.All : [];

    /// <summary>The rule sets this instance validates against, in the order they were added.</summary>
    public IReadOnlyList<IDocumentRuleSet> RuleSets => _ruleSets;

    /// <summary>
    /// Everything this library ships: UBL, CII, Factur-X and lifecycle profiles, the EN 16931 rules, and
    /// balanced diagnostics.
    /// </summary>
    /// <param name="pdf">
    /// A PDF reader, if hybrid invoices should be opened. Reference
    /// <c>International.EInvoicing.FacturX.PdfSharp</c> for one; without it a PDF is reported rather than read.
    /// </param>
    public static EInvoicing CreateDefault(IPdfAttachmentReader? pdf = null) =>
        Create(einvoicing => einvoicing.AddDefaults(), pdf);

    /// <summary>
    /// A library instance assembled the way you want it.
    /// </summary>
    /// <remarks>
    /// The same calls a container takes, without the container:
    /// <code>
    /// EInvoicing library = EInvoicing.Create(e => e
    ///     .AddDefaults()
    ///     .AddFrance()
    ///     .AddRulesFromFile(DocumentSyntax.Ubl, "artefacts/PEPPOL-EN16931-UBL.sch", "Peppol", "3.0")
    ///     .UseDiagnosticPreset(DiagnosticPreset.Strict));
    /// </code>
    /// </remarks>
    /// <param name="configure">What to assemble.</param>
    /// <exception cref="ArgumentNullException"><paramref name="configure"/> is <c>null</c>.</exception>
    public static EInvoicing Create(Action<EInvoicingBuilder> configure) => Create(configure, null);

    /// <summary>A library instance assembled the way you want it, able to open hybrid PDFs.</summary>
    /// <param name="configure">What to assemble.</param>
    /// <param name="pdf">A PDF reader, if hybrid invoices should be opened.</param>
    /// <exception cref="ArgumentNullException"><paramref name="configure"/> is <c>null</c>.</exception>
    public static EInvoicing Create(Action<EInvoicingBuilder> configure, IPdfAttachmentReader? pdf)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new EInvoicingBuilder();
        configure(builder);

        EInvoicingOptions options = builder.BuildOptions();
        var resolver = new ProfileResolver(builder.BuildRegistry());

        return new EInvoicing(
            options,
            resolver,
            builder.BuildRuleSets(),
            DocumentHandlers.CreateDefault(options, resolver, builder.BuildWriteSteps()),
            pdf);
    }

    /// <summary>The same, from parts you already have.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <c>null</c>.</exception>
    public static EInvoicing Create(EInvoicingOptions options) => Create(options, null, null);

    /// <summary>The same, from parts you already have.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <c>null</c>.</exception>
    public static EInvoicing Create(
        EInvoicingOptions options,
        IProfileResolver? profiles,
        IPdfAttachmentReader? pdf)
    {
        ArgumentNullException.ThrowIfNull(options);

        EInvoicing defaults = CreateDefault();
        return new EInvoicing(options, profiles ?? defaults.Profiles, defaults.RuleSets, pdf);
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

        // The document says what encoding it is in, and senders get that wrong often enough that decoding
        // everything as UTF-8 turns Müller into MÃ¼ller in the one field a human reads.
        DecodedDocument decoded = DocumentText.Decode(document);
        string text = decoded.Text;

        return WithDecoding(decoded, Detect(text) switch
        {
            DocumentKind.Ubl => FromInvoice(DocumentKind.Ubl, Ubl.Read(text)),
            DocumentKind.UblCreditNote => FromInvoice(DocumentKind.UblCreditNote, Ubl.Read(text)),
            DocumentKind.Cii => FromInvoice(DocumentKind.Cii, Cii.Read(text)),
            DocumentKind.Cdar => FromStatus(DocumentKind.Cdar, Lifecycle.Read(text)),
            DocumentKind.UblApplicationResponse =>
                FromStatus(DocumentKind.UblApplicationResponse, UblResponse.Read(text)),
            DocumentKind.UblDespatchAdvice => FromDespatchAdvice(UblDespatchAdvice.Read(text)),
            DocumentKind.UblOrder => FromOrder(UblOrder.Read(text)),
            DocumentKind.UblOrderResponse => FromOrderResponse(UblOrderResponse.Read(text)),
            DocumentKind.UblOrderCancellation => FromOrderCancellation(UblOrderCancellation.Read(text)),
            _ => new DocumentResult
            {
                Kind = DocumentKind.Unknown,
                Diagnostics = [WhyNot(text)],
            },
        });
    }

    /// <summary>Adds what decoding reported, if anything, to what reading reported.</summary>
    private static DocumentResult WithDecoding(DecodedDocument decoded, DocumentResult result) =>
        decoded.Diagnostic is { } diagnostic
            ? result with { Diagnostics = [diagnostic, .. result.Diagnostics] }
            : result;

    /// <summary>
    /// Reads a document from a stream without blocking on the read. The stream is left open.
    /// </summary>
    /// <remarks>
    /// Only the reading is asynchronous — parsing happens in memory once the bytes have arrived, which is
    /// where the wait actually is when a document comes off a network or a blob store.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> is <c>null</c>.</exception>
    public async Task<DocumentResult> ReadAsync(Stream document, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        using var buffer = new MemoryStream();
        await document.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);

        return Read(buffer.ToArray());
    }

    /// <summary>Reads whatever the file holds, XML or PDF.</summary>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty.</exception>
    /// <exception cref="FileNotFoundException">There is no file there.</exception>
    public DocumentResult ReadFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return Named(Read(File.ReadAllBytes(path)), path);
    }

    /// <summary>Reads whatever the file holds, without blocking on the read.</summary>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty.</exception>
    /// <exception cref="FileNotFoundException">There is no file there.</exception>
    public async Task<DocumentResult> ReadFileAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        byte[] content = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
        return Named(Read(content), path);
    }

    /// <summary>Gives the readable copy the name of the file it was read from, which only this call knows.</summary>
    private static DocumentResult Named(DocumentResult result, string path) =>
        result.Rendition is { } rendition
            ? result with { Rendition = rendition with { FileName = Path.GetFileName(path) } }
            : result;

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

        if (root.Namespace == UblNames.CreditNote)
        {
            return DocumentKind.UblCreditNote;
        }

        if (root.Namespace == UblApplicationResponseNames.ApplicationResponse)
        {
            return DocumentKind.UblApplicationResponse;
        }

        if (root.Namespace == UblDespatchAdviceNames.DespatchAdvice)
        {
            return DocumentKind.UblDespatchAdvice;
        }

        if (root.Namespace == UblOrderNames.Order)
        {
            return DocumentKind.UblOrder;
        }

        if (root.Namespace == UblOrderResponseNames.OrderResponse)
        {
            return DocumentKind.UblOrderResponse;
        }

        if (root.Namespace == UblOrderCancellationNames.OrderCancellation)
        {
            return DocumentKind.UblOrderCancellation;
        }

        return root.Namespace == UblNames.Invoice ? DocumentKind.Ubl : DocumentKind.Unknown;
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

    /// <summary>
    /// Converts an invoice to another syntax, and says what the conversion cost.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Converting between UBL and CII is a real requirement — a French recipient must accept both — and
    /// doing it silently is the dangerous version, which is why this returns a report rather than a string.
    /// </para>
    /// <para>
    /// The losses are found rather than predicted: the converted document is read back, and what that
    /// reports is recorded, along with every extension element the source carried. Those are syntax-specific
    /// by definition and have nowhere to go in the other syntax; everything the model maps survives by
    /// construction, because both writers write from the same model.
    /// </para>
    /// </remarks>
    /// <param name="invoice">The invoice to convert.</param>
    /// <param name="format">The syntax to write it in.</param>
    /// <exception cref="ArgumentNullException"><paramref name="invoice"/> is <c>null</c>.</exception>
    /// <exception cref="NotSupportedException">No writer is registered for that syntax.</exception>
    public ConversionResult Convert(EInvoice invoice, DocumentFormat format)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        string xml = Write(invoice, format);
        DocumentResult read = Read(xml);

        List<ConversionLoss> losses =
        [
            .. invoice.Extensions().Select(extension => new ConversionLoss(
                ConversionLossKind.SyntaxSpecificContent,
                extension.Location.Path is { Length: > 0 } path ? path : extension.QualifiedName,
                extension.QualifiedName)),
            .. read.Diagnostics
                .Where(diagnostic => diagnostic.Severity >= DiagnosticSeverity.Warning)
                .Select(diagnostic => new ConversionLoss(
                    ConversionLossKind.ReportedOnReread,
                        diagnostic.Location.Path ?? "(unknown)",
                    diagnostic.Message)),
        ];

        return new ConversionResult(xml, format, read.Invoice, losses, read.Diagnostics);
    }

    /// <summary>
    /// Converts a document to another syntax, and says what the conversion cost.
    /// </summary>
    /// <remarks>
    /// The document is read first, so what reading it reported is part of the report: a conversion built on a
    /// document that would not read cleanly is not a clean conversion, and the caller is told so rather than
    /// handed a plausible-looking result.
    /// </remarks>
    /// <param name="xml">The document to convert.</param>
    /// <param name="format">The syntax to write it in.</param>
    /// <exception cref="ArgumentNullException"><paramref name="xml"/> is <c>null</c>.</exception>
    /// <exception cref="NotSupportedException">No writer is registered for that syntax.</exception>
    public ConversionResult Convert(string xml, DocumentFormat format)
    {
        ArgumentNullException.ThrowIfNull(xml);

        DocumentResult source = Read(xml);

        if (source.Invoice is not { } invoice)
        {
            return new ConversionResult(string.Empty, format, null, [], source.Diagnostics);
        }

        ConversionResult converted = Convert(invoice, format);

        return converted with { Diagnostics = [.. source.Diagnostics, .. converted.Diagnostics] };
    }

    /// <summary>
    /// Writes an invoice in the syntax its own profile is written in.
    /// </summary>
    /// <remarks>
    /// An invoice declares what it conforms to (BT-24), and a profile belongs to one syntax: an XRechnung CII
    /// invoice is CII, a Peppol one is UBL. Naming the syntax again is a chance to name the wrong one, so
    /// this asks the profile instead. Say it explicitly with the overload when the profile is unknown to this
    /// instance, or when you deliberately want the other syntax.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="invoice"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">
    /// The invoice declares no profile, or one this instance does not know, so the syntax cannot be inferred.
    /// </exception>
    public string Write(EInvoice invoice)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        ProfileResolution resolution = Profiles.Resolve(invoice.SpecificationIdentifier, DocumentSyntax.Ubl);
        ProfileResolution cii = Profiles.Resolve(invoice.SpecificationIdentifier, DocumentSyntax.Cii);

        if (resolution.IsExact)
        {
            return Write(invoice, DocumentFormat.Ubl);
        }

        if (cii.IsExact)
        {
            return Write(invoice, DocumentFormat.Cii);
        }

        throw new InvalidOperationException(
            $"The syntax cannot be inferred from '{invoice.SpecificationIdentifier}': it is not a profile this "
            + "instance knows. Name the syntax with Write(invoice, DocumentFormat.Ubl) or register the profile "
            + "with AddProfile(...).");
    }

    /// <summary>Writes an invoice to a stream. The stream is left open.</summary>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    public void Write(EInvoice invoice, DocumentFormat format, Stream destination)
    {
        ArgumentNullException.ThrowIfNull(invoice);
        ArgumentNullException.ThrowIfNull(destination);

        if (format == DocumentFormat.Cii)
        {
            CiiWriter.Write(invoice, destination);
            return;
        }

        UblWriter.Write(invoice, destination);
    }

    /// <summary>
    /// Writes an invoice to a stream without blocking while it is sent. The stream is left open.
    /// </summary>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    /// <exception cref="OperationCanceledException">The token was cancelled while the document was sent.</exception>
    public Task WriteAsync(
        EInvoice invoice,
        DocumentFormat format,
        Stream destination,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(invoice);
        ArgumentNullException.ThrowIfNull(destination);

        IDocumentWriter<EInvoice> writer = format == DocumentFormat.Cii ? CiiWriter : UblWriter;

        return writer.WriteAsync(invoice, destination, cancellationToken);
    }

    /// <summary>Writes an order cancellation as UBL.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="cancellation"/> is <c>null</c>.</exception>
    public string Write(OrderCancellation cancellation)
    {
        ArgumentNullException.ThrowIfNull(cancellation);
        return UblOrderCancellationWriter.WriteToString(cancellation);
    }

    /// <summary>Writes an order response as UBL.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="response"/> is <c>null</c>.</exception>
    public string Write(OrderResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        return UblOrderResponseWriter.WriteToString(response);
    }

    /// <summary>Writes an order as UBL.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="order"/> is <c>null</c>.</exception>
    public string Write(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);
        return UblOrderWriter.WriteToString(order);
    }

    /// <summary>Writes a despatch advice as UBL.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="advice"/> is <c>null</c>.</exception>
    public string Write(DespatchAdvice advice)
    {
        ArgumentNullException.ThrowIfNull(advice);
        return UblDespatchAdviceWriter.WriteToString(advice);
    }

    /// <summary>Writes a lifecycle status message in UN/CEFACT CDAR.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="status"/> is <c>null</c>.</exception>
    public string Write(LifecycleStatusMessage status)
    {
        ArgumentNullException.ThrowIfNull(status);
        return LifecycleWriter.WriteToString(status);
    }

    /// <summary>
    /// Writes a lifecycle status message in the syntax asked for.
    /// </summary>
    /// <remarks>
    /// The same statement travels as UN/CEFACT CDAR between the French platforms and as a UBL
    /// <c>ApplicationResponse</c> over the Peppol network, so which one to write is the network's decision
    /// rather than the message's.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="status"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">Nothing registered writes that syntax.</exception>
    public string Write(LifecycleStatusMessage status, DocumentSyntax syntax)
    {
        ArgumentNullException.ThrowIfNull(status);

        return Required(Handlers.LifecycleWriterFor(syntax), $"write lifecycle messages as {syntax}")
            .WriteToString(status);
    }

    /// <summary>
    /// Validates a document against every rule set registered for it, and says what it could not check.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Which rule sets those are is what you assembled: <c>AddDefaults()</c> brings EN 16931,
    /// <c>AddXRechnungRules()</c> the German ones, <c>AddRulesFromFile(...)</c> the artefacts that may not be
    /// redistributed. Each one decides for itself whether it governs the document in front of it.
    /// </para>
    /// <para>
    /// A profile no registered rule set covers is reported as not checked rather than passed over, so
    /// <see cref="ValidationReport.IsComplete"/> tells the truth about how much was verified.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> is <c>null</c>.</exception>
    public ValidationReport Validate(string document)
    {
        ArgumentNullException.ThrowIfNull(document);

        DocumentKind kind = Detect(document);
        DocumentSyntax? syntax = SyntaxOf(kind);

        if (syntax is not { } documentSyntax)
        {
            return new ValidationReport(
                [],
                [new RuleSetOutcome("(none)", "—", Ran: false, $"{kind} is not a syntax this library validates")]);
        }

        DocumentResult read = Read(document);
        ProfileIdentifier declared = read.Profile?.Declared ?? default;

        ValidationReport report = ValidationReport.Empty;
        bool ranSomething = false;

        foreach (IDocumentRuleSet ruleSet in _ruleSets)
        {
            if (!ruleSet.AppliesTo(documentSyntax, declared))
            {
                continue;
            }

            report = report.And(ruleSet.Validate(document));
            ranSomething = true;
        }

        if (!ranSomething)
        {
            return report.And(new ValidationReport(
                [],
                [
                    new RuleSetOutcome(
                        declared.IsDeclared ? declared.ToString() : "(no profile declared)",
                        "—",
                        Ran: false,
                        "no rule set is registered for that profile; add one with AddRules(...)"),
                ]));
        }

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
                    "this library implements no rule set for that profile, so only the general rules ran"),
            ]));
    }

    /// <summary>Which syntax a detected document is written in, or <c>null</c> when it is not one.</summary>
    private static DocumentSyntax? SyntaxOf(DocumentKind kind) => kind switch
    {
        DocumentKind.Ubl
            or DocumentKind.UblCreditNote
            or DocumentKind.UblApplicationResponse
            or DocumentKind.UblDespatchAdvice
            or DocumentKind.UblOrder
            or DocumentKind.UblOrderResponse
            or DocumentKind.UblOrderCancellation => DocumentSyntax.Ubl,
        DocumentKind.Cii => DocumentSyntax.Cii,
        DocumentKind.Cdar => DocumentSyntax.Cdar,
        _ => null,
    };

    /// <summary>
    /// Why nothing could be read: the document is not well-formed, or it is well-formed and not ours.
    /// </summary>
    /// <remarks>
    /// Worth telling apart. "This is not a document I recognise" sent at a truncated file has somebody
    /// checking their profile identifiers for an hour before they notice the file ends mid-element.
    /// </remarks>
    private static Diagnostic WhyNot(string text)
    {
        try
        {
            using var reader = SecureXml.CreateReader(text);
            XDocument.Load(reader);
        }
        catch (System.Xml.XmlException malformed)
        {
            return Diagnostic.Create(DiagnosticCodes.MalformedDocument, malformed.Message);
        }

        return Unrecognised();
    }

    private const string PdfMediaType = "application/pdf";

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
            Rendition = new InvoiceRendition(pdf, PdfMediaType),
        };
    }

    private static DocumentResult FromInvoice(DocumentKind kind, ParseResult<EInvoice> result) => new()
    {
        Kind = kind,
        Invoice = result.Value,
        Diagnostics = result.Diagnostics,
        Profile = result.Value?.Profile,
    };

    private static DocumentResult FromOrderCancellation(ParseResult<OrderCancellation> result) => new()
    {
        Kind = DocumentKind.UblOrderCancellation,
        OrderCancellation = result.Value,
        Diagnostics = result.Diagnostics,
        Profile = result.Value?.Profile,
    };

    private static DocumentResult FromOrderResponse(ParseResult<OrderResponse> result) => new()
    {
        Kind = DocumentKind.UblOrderResponse,
        OrderResponse = result.Value,
        Diagnostics = result.Diagnostics,
        Profile = result.Value?.Profile,
    };

    private static DocumentResult FromOrder(ParseResult<Order> result) => new()
    {
        Kind = DocumentKind.UblOrder,
        Order = result.Value,
        Diagnostics = result.Diagnostics,
        Profile = result.Value?.Profile,
    };

    private static DocumentResult FromDespatchAdvice(ParseResult<DespatchAdvice> result) => new()
    {
        Kind = DocumentKind.UblDespatchAdvice,
        DespatchAdvice = result.Value,
        Diagnostics = result.Diagnostics,
        Profile = result.Value?.Profile,
    };

    private static DocumentResult FromStatus(DocumentKind kind, ParseResult<LifecycleStatusMessage> result) => new()
    {
        Kind = kind,
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
