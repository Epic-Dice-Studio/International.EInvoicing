using International.EInvoicing.Diagnostics;
using International.EInvoicing.Profiles;

namespace International.EInvoicing.Documents;

/// <summary>
/// Reads one syntax into the model.
/// </summary>
/// <remarks>
/// <para>
/// Implement this to teach the library a syntax it does not know, or to replace one it does. A reader
/// registered in the container is used in place of the built-in one for its syntax, which is what "extensible
/// without forking" means for reading.
/// </para>
/// <para>
/// A reader does not throw on a document it was handed. Unknown profiles, unreadable values and elements the
/// model has no field for come back as diagnostics with a fallback that is named. Exceptions are for
/// programming errors — a null argument, a disposed stream.
/// </para>
/// </remarks>
/// <typeparam name="TDocument">What this reader produces.</typeparam>
public interface IDocumentReader<TDocument>
    where TDocument : class
{
    /// <summary>The syntax this reader understands.</summary>
    DocumentSyntax Syntax { get; }

    /// <summary>Reads a document from a stream. The stream is left open.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <c>null</c>.</exception>
    ParseResult<TDocument> Read(Stream stream);

    /// <summary>Reads a document from XML text.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="xml"/> is <c>null</c>.</exception>
    ParseResult<TDocument> Read(string xml);

    /// <summary>
    /// Reads a document from a stream without blocking while it arrives. The stream is left open.
    /// </summary>
    /// <remarks>
    /// The awaiting is the transfer; the parsing that follows is work, not waiting. See
    /// <c>docs/adr/0012-async-at-the-boundary.md</c>.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <c>null</c>.</exception>
    /// <exception cref="OperationCanceledException">The token was cancelled while the document arrived.</exception>
    Task<ParseResult<TDocument>> ReadAsync(Stream stream, CancellationToken cancellationToken = default);
}
