using International.EInvoicing.Cdar.Reading;
using International.EInvoicing.Cdar.Writing;
using International.EInvoicing.Cii.Reading;
using International.EInvoicing.Cii.Writing;
using International.EInvoicing.Configuration;
using International.EInvoicing.Documents;
using International.EInvoicing.Model;
using International.EInvoicing.OrderX.Reading;
using International.EInvoicing.OrderX.Writing;
using International.EInvoicing.Profiles;
using International.EInvoicing.Ubl.Reading;
using International.EInvoicing.Ubl.Writing;

namespace International.EInvoicing;

/// <summary>
/// Which reader and which writer the facade uses for each syntax.
/// </summary>
/// <remarks>
/// <para>
/// This is what makes "register your own reader or writer and it wins over ours" true rather than a slogan:
/// the facade asks this, and this prefers the **last** registration for a syntax — the container hands them
/// over in registration order, and yours comes after the built-in one.
/// </para>
/// <para>
/// Nothing forces you through here. The individual readers and writers stay public, and a caller who knows
/// what they hold can use them directly.
/// </para>
/// </remarks>
public sealed class DocumentHandlers
{
    private readonly IReadOnlyList<IDocumentReader<EInvoice>> _invoiceReaders;
    private readonly IReadOnlyList<IDocumentWriter<EInvoice>> _invoiceWriters;
    private readonly IReadOnlyList<IDocumentReader<LifecycleStatusMessage>> _lifecycleReaders;
    private readonly IReadOnlyList<IDocumentWriter<LifecycleStatusMessage>> _lifecycleWriters;
    private readonly IReadOnlyList<IDocumentReader<DespatchAdvice>> _despatchReaders;
    private readonly IReadOnlyList<IDocumentWriter<DespatchAdvice>> _despatchWriters;
    private readonly IReadOnlyList<IDocumentReader<Order>> _orderReaders;
    private readonly IReadOnlyList<IDocumentWriter<Order>> _orderWriters;
    private readonly IReadOnlyList<IDocumentReader<OrderResponse>> _orderResponseReaders;
    private readonly IReadOnlyList<IDocumentWriter<OrderResponse>> _orderResponseWriters;
    private readonly IReadOnlyList<IDocumentReader<OrderCancellation>> _cancellationReaders;
    private readonly IReadOnlyList<IDocumentWriter<OrderCancellation>> _cancellationWriters;
    private readonly IReadOnlyList<IWritePipelineStep> _writeSteps;

    /// <summary>Collects the handlers a container has registered.</summary>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    public DocumentHandlers(
        IEnumerable<IDocumentReader<EInvoice>> invoiceReaders,
        IEnumerable<IDocumentWriter<EInvoice>> invoiceWriters,
        IEnumerable<IDocumentReader<LifecycleStatusMessage>> lifecycleReaders,
        IEnumerable<IDocumentWriter<LifecycleStatusMessage>> lifecycleWriters)
        : this(invoiceReaders, invoiceWriters, lifecycleReaders, lifecycleWriters, [])
    {
    }

    /// <summary>The same, with the write pipeline the caller assembled.</summary>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    public DocumentHandlers(
        IEnumerable<IDocumentReader<EInvoice>> invoiceReaders,
        IEnumerable<IDocumentWriter<EInvoice>> invoiceWriters,
        IEnumerable<IDocumentReader<LifecycleStatusMessage>> lifecycleReaders,
        IEnumerable<IDocumentWriter<LifecycleStatusMessage>> lifecycleWriters,
        IEnumerable<IWritePipelineStep> writeSteps)
        : this(invoiceReaders, invoiceWriters, lifecycleReaders, lifecycleWriters, [], [], writeSteps)
    {
    }

    /// <summary>The same, with the despatch advice handlers as well.</summary>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    public DocumentHandlers(
        IEnumerable<IDocumentReader<EInvoice>> invoiceReaders,
        IEnumerable<IDocumentWriter<EInvoice>> invoiceWriters,
        IEnumerable<IDocumentReader<LifecycleStatusMessage>> lifecycleReaders,
        IEnumerable<IDocumentWriter<LifecycleStatusMessage>> lifecycleWriters,
        IEnumerable<IDocumentReader<DespatchAdvice>> despatchReaders,
        IEnumerable<IDocumentWriter<DespatchAdvice>> despatchWriters,
        IEnumerable<IWritePipelineStep> writeSteps)
        : this(invoiceReaders, invoiceWriters, lifecycleReaders, lifecycleWriters, despatchReaders,
            despatchWriters, [], [], writeSteps)
    {
    }

    /// <summary>The same, with the order handlers as well.</summary>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    public DocumentHandlers(
        IEnumerable<IDocumentReader<EInvoice>> invoiceReaders,
        IEnumerable<IDocumentWriter<EInvoice>> invoiceWriters,
        IEnumerable<IDocumentReader<LifecycleStatusMessage>> lifecycleReaders,
        IEnumerable<IDocumentWriter<LifecycleStatusMessage>> lifecycleWriters,
        IEnumerable<IDocumentReader<DespatchAdvice>> despatchReaders,
        IEnumerable<IDocumentWriter<DespatchAdvice>> despatchWriters,
        IEnumerable<IDocumentReader<Order>> orderReaders,
        IEnumerable<IDocumentWriter<Order>> orderWriters,
        IEnumerable<IWritePipelineStep> writeSteps)
        : this(invoiceReaders, invoiceWriters, lifecycleReaders, lifecycleWriters, despatchReaders,
            despatchWriters, orderReaders, orderWriters, [], [], writeSteps)
    {
    }

    /// <summary>The same, with the order response handlers as well.</summary>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    public DocumentHandlers(
        IEnumerable<IDocumentReader<EInvoice>> invoiceReaders,
        IEnumerable<IDocumentWriter<EInvoice>> invoiceWriters,
        IEnumerable<IDocumentReader<LifecycleStatusMessage>> lifecycleReaders,
        IEnumerable<IDocumentWriter<LifecycleStatusMessage>> lifecycleWriters,
        IEnumerable<IDocumentReader<DespatchAdvice>> despatchReaders,
        IEnumerable<IDocumentWriter<DespatchAdvice>> despatchWriters,
        IEnumerable<IDocumentReader<Order>> orderReaders,
        IEnumerable<IDocumentWriter<Order>> orderWriters,
        IEnumerable<IDocumentReader<OrderResponse>> orderResponseReaders,
        IEnumerable<IDocumentWriter<OrderResponse>> orderResponseWriters,
        IEnumerable<IWritePipelineStep> writeSteps)
        : this(invoiceReaders, invoiceWriters, lifecycleReaders, lifecycleWriters, despatchReaders,
            despatchWriters, orderReaders, orderWriters, orderResponseReaders, orderResponseWriters,
            [], [], writeSteps)
    {
    }

    /// <summary>The same, with the order cancellation handlers as well.</summary>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    public DocumentHandlers(
        IEnumerable<IDocumentReader<EInvoice>> invoiceReaders,
        IEnumerable<IDocumentWriter<EInvoice>> invoiceWriters,
        IEnumerable<IDocumentReader<LifecycleStatusMessage>> lifecycleReaders,
        IEnumerable<IDocumentWriter<LifecycleStatusMessage>> lifecycleWriters,
        IEnumerable<IDocumentReader<DespatchAdvice>> despatchReaders,
        IEnumerable<IDocumentWriter<DespatchAdvice>> despatchWriters,
        IEnumerable<IDocumentReader<Order>> orderReaders,
        IEnumerable<IDocumentWriter<Order>> orderWriters,
        IEnumerable<IDocumentReader<OrderResponse>> orderResponseReaders,
        IEnumerable<IDocumentWriter<OrderResponse>> orderResponseWriters,
        IEnumerable<IDocumentReader<OrderCancellation>> cancellationReaders,
        IEnumerable<IDocumentWriter<OrderCancellation>> cancellationWriters,
        IEnumerable<IWritePipelineStep> writeSteps)
    {
        ArgumentNullException.ThrowIfNull(invoiceReaders);
        ArgumentNullException.ThrowIfNull(invoiceWriters);
        ArgumentNullException.ThrowIfNull(lifecycleReaders);
        ArgumentNullException.ThrowIfNull(lifecycleWriters);
        ArgumentNullException.ThrowIfNull(despatchReaders);
        ArgumentNullException.ThrowIfNull(despatchWriters);
        ArgumentNullException.ThrowIfNull(orderReaders);
        ArgumentNullException.ThrowIfNull(orderWriters);
        ArgumentNullException.ThrowIfNull(orderResponseReaders);
        ArgumentNullException.ThrowIfNull(orderResponseWriters);
        ArgumentNullException.ThrowIfNull(cancellationReaders);
        ArgumentNullException.ThrowIfNull(cancellationWriters);
        ArgumentNullException.ThrowIfNull(writeSteps);

        _invoiceReaders = [.. invoiceReaders];
        _invoiceWriters = [.. invoiceWriters];
        _lifecycleReaders = [.. lifecycleReaders];
        _lifecycleWriters = [.. lifecycleWriters];
        _despatchReaders = [.. despatchReaders];
        _despatchWriters = [.. despatchWriters];
        _orderReaders = [.. orderReaders];
        _orderWriters = [.. orderWriters];
        _orderResponseReaders = [.. orderResponseReaders];
        _orderResponseWriters = [.. orderResponseWriters];
        _cancellationReaders = [.. cancellationReaders];
        _cancellationWriters = [.. cancellationWriters];
        _writeSteps = [.. writeSteps];
    }

    /// <summary>The handlers this library ships, for a caller assembling it without a container.</summary>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    public static DocumentHandlers CreateDefault(EInvoicingOptions options, IProfileResolver profiles) =>
        CreateDefault(options, profiles, []);

    /// <summary>The same, with the write pipeline the caller assembled.</summary>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    public static DocumentHandlers CreateDefault(
        EInvoicingOptions options,
        IProfileResolver profiles,
        IEnumerable<IWritePipelineStep> writeSteps)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(profiles);
        ArgumentNullException.ThrowIfNull(writeSteps);

        return new DocumentHandlers(
            [new UblInvoiceReader(options, profiles), new CiiInvoiceReader(options, profiles)],
            [new UblInvoiceWriter(), new CiiInvoiceWriter()],
            [new CdarReader(options, profiles), new UblApplicationResponseReader(options, profiles)],
            [new CdarWriter(), new UblApplicationResponseWriter()],
            [new UblDespatchAdviceReader(options, profiles)],
            [new UblDespatchAdviceWriter()],
            [new UblOrderReader(options, profiles), new OrderXOrderReader(options, profiles)],
            [new UblOrderWriter(), new OrderXOrderWriter()],
            [new UblOrderResponseReader(options, profiles)],
            [new UblOrderResponseWriter()],
            [new UblOrderCancellationReader(options, profiles)],
            [new UblOrderCancellationWriter()],
            writeSteps);
    }

    /// <summary>The invoice reader for a syntax, or <c>null</c> when nothing handles it.</summary>
    public IDocumentReader<EInvoice>? InvoiceReaderFor(DocumentSyntax syntax) => Last(_invoiceReaders, syntax);

    /// <summary>The write pipeline steps that run in front of every invoice writer, in the order they run.</summary>
    public IReadOnlyList<IWritePipelineStep> WriteSteps => _writeSteps;

    /// <summary>
    /// The invoice writer for a syntax, or <c>null</c> when nothing handles it.
    /// </summary>
    /// <remarks>
    /// The write pipeline comes wrapped around it, so a caller that takes the writer and uses it directly
    /// still runs the steps. A guarantee with a bypass is not a guarantee.
    /// </remarks>
    public IDocumentWriter<EInvoice>? InvoiceWriterFor(DocumentSyntax syntax) =>
        Last(_invoiceWriters, syntax) is { } writer ? WritePipeline.Around(writer, _writeSteps) : null;

    /// <summary>
    /// The lifecycle reader for UN/CEFACT CDAR, or <c>null</c> when nothing handles it.
    /// </summary>
    /// <remarks>
    /// A lifecycle status arrives in two syntaxes — CDAR from the French platforms, a UBL
    /// <c>ApplicationResponse</c> from the Peppol network — so ask for the one you hold with
    /// <see cref="LifecycleReaderFor"/>. This overload answers for CDAR, which is what an unqualified
    /// "lifecycle message" meant when there was only one.
    /// </remarks>
    public IDocumentReader<LifecycleStatusMessage>? LifecycleReader() =>
        LifecycleReaderFor(DocumentSyntax.Cdar);

    /// <summary>The lifecycle reader for a syntax, or <c>null</c> when nothing handles it.</summary>
    public IDocumentReader<LifecycleStatusMessage>? LifecycleReaderFor(DocumentSyntax syntax) =>
        Last(_lifecycleReaders, syntax);

    /// <summary>The lifecycle writer for UN/CEFACT CDAR, or <c>null</c> when nothing handles it.</summary>
    public IDocumentWriter<LifecycleStatusMessage>? LifecycleWriter() =>
        LifecycleWriterFor(DocumentSyntax.Cdar);

    /// <summary>The lifecycle writer for a syntax, or <c>null</c> when nothing handles it.</summary>
    public IDocumentWriter<LifecycleStatusMessage>? LifecycleWriterFor(DocumentSyntax syntax) =>
        Last(_lifecycleWriters, syntax);

    /// <summary>The despatch advice reader for a syntax, or <c>null</c> when nothing handles it.</summary>
    public IDocumentReader<DespatchAdvice>? DespatchAdviceReaderFor(DocumentSyntax syntax) =>
        Last(_despatchReaders, syntax);

    /// <summary>The despatch advice writer for a syntax, or <c>null</c> when nothing handles it.</summary>
    public IDocumentWriter<DespatchAdvice>? DespatchAdviceWriterFor(DocumentSyntax syntax) =>
        Last(_despatchWriters, syntax);

    /// <summary>The order reader for a syntax, or <c>null</c> when nothing handles it.</summary>
    public IDocumentReader<Order>? OrderReaderFor(DocumentSyntax syntax) => Last(_orderReaders, syntax);

    /// <summary>The order writer for a syntax, or <c>null</c> when nothing handles it.</summary>
    public IDocumentWriter<Order>? OrderWriterFor(DocumentSyntax syntax) => Last(_orderWriters, syntax);

    /// <summary>The order response reader for a syntax, or <c>null</c> when nothing handles it.</summary>
    public IDocumentReader<OrderResponse>? OrderResponseReaderFor(DocumentSyntax syntax) =>
        Last(_orderResponseReaders, syntax);

    /// <summary>The order response writer for a syntax, or <c>null</c> when nothing handles it.</summary>
    public IDocumentWriter<OrderResponse>? OrderResponseWriterFor(DocumentSyntax syntax) =>
        Last(_orderResponseWriters, syntax);

    /// <summary>The order cancellation reader for a syntax, or <c>null</c> when nothing handles it.</summary>
    public IDocumentReader<OrderCancellation>? OrderCancellationReaderFor(DocumentSyntax syntax) =>
        Last(_cancellationReaders, syntax);

    /// <summary>The order cancellation writer for a syntax, or <c>null</c> when nothing handles it.</summary>
    public IDocumentWriter<OrderCancellation>? OrderCancellationWriterFor(DocumentSyntax syntax) =>
        Last(_cancellationWriters, syntax);

    private static THandler? Last<THandler>(IReadOnlyList<THandler> handlers, DocumentSyntax syntax)
        where THandler : class
    {
        for (int index = handlers.Count - 1; index >= 0; index--)
        {
            DocumentSyntax handled = handlers[index] switch
            {
                IDocumentReader<EInvoice> reader => reader.Syntax,
                IDocumentWriter<EInvoice> writer => writer.Syntax,
                IDocumentReader<LifecycleStatusMessage> reader => reader.Syntax,
                IDocumentWriter<LifecycleStatusMessage> writer => writer.Syntax,
                IDocumentReader<DespatchAdvice> reader => reader.Syntax,
                IDocumentWriter<DespatchAdvice> writer => writer.Syntax,
                IDocumentReader<Order> reader => reader.Syntax,
                IDocumentWriter<Order> writer => writer.Syntax,
                IDocumentReader<OrderResponse> reader => reader.Syntax,
                IDocumentWriter<OrderResponse> writer => writer.Syntax,
                IDocumentReader<OrderCancellation> reader => reader.Syntax,
                IDocumentWriter<OrderCancellation> writer => writer.Syntax,
                _ => default,
            };

            if (handled == syntax)
            {
                return handlers[index];
            }
        }

        return null;
    }
}
