using System.Diagnostics.CodeAnalysis;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;

namespace International.EInvoicing.Documents;

/// <summary>
/// What is being written, while it is being written.
/// </summary>
/// <remarks>
/// A step sees this before serialisation, when only <see cref="Invoice"/> is filled, and again after, when
/// <see cref="Xml"/> is. Both are writable: change the invoice on the way in, change the document on the way
/// out, or both.
/// </remarks>
public sealed class WriteContext
{
    /// <summary>Starts a context for an invoice about to be written in a syntax.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="invoice"/> is <c>null</c>.</exception>
    public WriteContext(EInvoice invoice, DocumentSyntax syntax)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        Invoice = invoice;
        Syntax = syntax;
    }

    /// <summary>The invoice being written. A step may change it, or replace it, before calling the next one.</summary>
    public EInvoice Invoice { get; set; }

    /// <summary>The syntax it is being written in.</summary>
    public DocumentSyntax Syntax { get; }

    /// <summary>
    /// The document. Empty until the writer at the end of the pipeline has run; a step that wants to see it
    /// or change it does so after calling the next step.
    /// </summary>
    public string Xml { get; set; } = string.Empty;

    /// <summary>Anything a step wants to hand to a later one, or to the caller.</summary>
    public IDictionary<string, object?> Items { get; } = new Dictionary<string, object?>(StringComparer.Ordinal);
}

/// <summary>
/// One step of the write pipeline: your own logic, running as part of generation.
/// </summary>
/// <remarks>
/// <para>
/// Numbering, house rounding, a signature, an audit line, an element your ERP insists on — none of that
/// belongs in a fork of this library, and none of it belongs sprinkled through calling code where it can be
/// forgotten. A step runs for every document the library writes, whichever syntax and whoever asked.
/// </para>
/// <para>
/// The shape is ASP.NET Core's, for the same reason: work before <c>next</c>, work after it, or decline to
/// call it at all. A step that does not call <c>next</c> stops the write, and
/// <see cref="WriteContext.Xml"/> is whatever it left there.
/// </para>
/// <code>
/// internal sealed class StampTheHouseReference : IWritePipelineStep
/// {
///     public void Write(WriteContext context, Action&lt;WriteContext&gt; next)
///     {
///         context.Invoice.BuyerReference = References.For(context.Invoice);
///         next(context);
///         context.Xml = Signatures.Sign(context.Xml);
///     }
/// }
/// </code>
/// </remarks>
public interface IWritePipelineStep
{
    /// <summary>Runs this step. Call <paramref name="next"/> to continue to the writer.</summary>
    [SuppressMessage(
        "Naming",
        "CA1716:Identifiers should not match keywords",
        Justification = "next is the word every middleware pipeline uses; renaming it would only obscure the shape.")]
    void Write(WriteContext context, Action<WriteContext> next);
}

/// <summary>
/// A writer with the pipeline in front of it.
/// </summary>
/// <remarks>
/// The steps are wrapped around the writer rather than called by the facade, so there is no way past them:
/// <c>library.Write(...)</c>, <c>library.UblWriter.WriteToString(...)</c> and a writer resolved straight out
/// of the container all run the same steps. A guarantee with a bypass is not a guarantee.
/// </remarks>
public sealed class WritePipeline : IDocumentWriter<EInvoice>
{
    private readonly IDocumentWriter<EInvoice> _writer;
    private readonly IReadOnlyList<IWritePipelineStep> _steps;

    /// <summary>Puts <paramref name="steps"/> in front of <paramref name="writer"/>, first registered first run.</summary>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    public WritePipeline(IDocumentWriter<EInvoice> writer, IEnumerable<IWritePipelineStep> steps)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(steps);

        _writer = writer;
        _steps = [.. steps];
    }

    /// <inheritdoc />
    public DocumentSyntax Syntax => _writer.Syntax;

    /// <summary>The writer at the end of the pipeline.</summary>
    public IDocumentWriter<EInvoice> Inner => _writer;

    /// <summary>The steps, in the order they run.</summary>
    public IReadOnlyList<IWritePipelineStep> Steps => _steps;

    /// <summary>
    /// Wraps <paramref name="writer"/> when there is anything to wrap it in, and hands it back untouched
    /// when there is not.
    /// </summary>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    public static IDocumentWriter<EInvoice> Around(
        IDocumentWriter<EInvoice> writer,
        IReadOnlyList<IWritePipelineStep> steps)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(steps);

        return steps.Count == 0 ? writer : new WritePipeline(writer, steps);
    }

    /// <inheritdoc />
    public string WriteToString(EInvoice document)
    {
        ArgumentNullException.ThrowIfNull(document);

        return Run(document).Xml;
    }

    /// <inheritdoc />
    public void Write(EInvoice document, Stream destination)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(destination);

        DocumentStreams.WriteAll(Run(document).Xml, destination);
    }

    /// <inheritdoc />
    public Task WriteAsync(EInvoice document, Stream destination, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(destination);

        return DocumentStreams.WriteAllAsync(Run(document).Xml, destination, cancellationToken);
    }

    /// <summary>Runs the pipeline and hands back the context, for a caller that wants what the steps left in it.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> is <c>null</c>.</exception>
    public WriteContext Run(EInvoice document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var context = new WriteContext(document, _writer.Syntax);
        Next(0)(context);
        return context;
    }

    private Action<WriteContext> Next(int index) => index == _steps.Count
        ? context => context.Xml = _writer.WriteToString(context.Invoice)
        : context => _steps[index].Write(context, Next(index + 1));
}
