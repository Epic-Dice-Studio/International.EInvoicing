using International.EInvoicing.Cdar.Model;
using International.EInvoicing.Cdar.Reading;
using International.EInvoicing.Cdar.Writing;
using International.EInvoicing.Cii.Reading;
using International.EInvoicing.Cii.Writing;
using International.EInvoicing.Configuration;
using International.EInvoicing.Documents;
using International.EInvoicing.Model;
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

    /// <summary>Collects the handlers a container has registered.</summary>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    public DocumentHandlers(
        IEnumerable<IDocumentReader<EInvoice>> invoiceReaders,
        IEnumerable<IDocumentWriter<EInvoice>> invoiceWriters,
        IEnumerable<IDocumentReader<LifecycleStatusMessage>> lifecycleReaders,
        IEnumerable<IDocumentWriter<LifecycleStatusMessage>> lifecycleWriters)
    {
        ArgumentNullException.ThrowIfNull(invoiceReaders);
        ArgumentNullException.ThrowIfNull(invoiceWriters);
        ArgumentNullException.ThrowIfNull(lifecycleReaders);
        ArgumentNullException.ThrowIfNull(lifecycleWriters);

        _invoiceReaders = [.. invoiceReaders];
        _invoiceWriters = [.. invoiceWriters];
        _lifecycleReaders = [.. lifecycleReaders];
        _lifecycleWriters = [.. lifecycleWriters];
    }

    /// <summary>The handlers this library ships, for a caller assembling it without a container.</summary>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    public static DocumentHandlers CreateDefault(EInvoicingOptions options, IProfileResolver profiles)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(profiles);

        return new DocumentHandlers(
            [new UblInvoiceReader(options, profiles), new CiiInvoiceReader(options, profiles)],
            [new UblInvoiceWriter(), new CiiInvoiceWriter()],
            [new CdarReader(options, profiles)],
            [new CdarWriter()]);
    }

    /// <summary>The invoice reader for a syntax, or <c>null</c> when nothing handles it.</summary>
    public IDocumentReader<EInvoice>? InvoiceReaderFor(DocumentSyntax syntax) => Last(_invoiceReaders, syntax);

    /// <summary>The invoice writer for a syntax, or <c>null</c> when nothing handles it.</summary>
    public IDocumentWriter<EInvoice>? InvoiceWriterFor(DocumentSyntax syntax) => Last(_invoiceWriters, syntax);

    /// <summary>The lifecycle reader, or <c>null</c> when nothing handles lifecycle messages.</summary>
    public IDocumentReader<LifecycleStatusMessage>? LifecycleReader() =>
        Last(_lifecycleReaders, DocumentSyntax.Cdar);

    /// <summary>The lifecycle writer, or <c>null</c> when nothing handles lifecycle messages.</summary>
    public IDocumentWriter<LifecycleStatusMessage>? LifecycleWriter() =>
        Last(_lifecycleWriters, DocumentSyntax.Cdar);

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
