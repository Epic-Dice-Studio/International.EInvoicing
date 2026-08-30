using International.EInvoicing.Profiles;

namespace International.EInvoicing.Documents;

/// <summary>
/// Writes the model out in one syntax.
/// </summary>
/// <remarks>
/// Implement this to add a syntax, or to replace one — a house dialect, an extra element a partner insists
/// on, a different ordering. A writer registered in the container is used in place of the built-in one for
/// its syntax.
/// </remarks>
/// <typeparam name="TDocument">What this writer accepts.</typeparam>
public interface IDocumentWriter<TDocument>
{
    /// <summary>The syntax this writer produces.</summary>
    DocumentSyntax Syntax { get; }

    /// <summary>Writes a document to a stream. The stream is left open.</summary>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    void Write(TDocument document, Stream destination);

    /// <summary>Writes a document and returns it as XML text.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> is <c>null</c>.</exception>
    string WriteToString(TDocument document);

    /// <summary>
    /// Writes a document to a stream without blocking while it is sent. The stream is left open.
    /// </summary>
    /// <remarks>
    /// The document is serialised in memory and then handed to the stream asynchronously: the awaiting is
    /// the transfer, which is where the waiting actually is when the destination is a network response.
    /// </remarks>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    /// <exception cref="OperationCanceledException">The token was cancelled while the document was sent.</exception>
    Task WriteAsync(TDocument document, Stream destination, CancellationToken cancellationToken = default);
}
