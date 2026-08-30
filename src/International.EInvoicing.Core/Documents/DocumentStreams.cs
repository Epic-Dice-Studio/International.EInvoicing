namespace International.EInvoicing.Documents;

/// <summary>
/// Moving a document between a stream and memory, which is the part of reading and writing that waits.
/// </summary>
/// <remarks>
/// Every reader and writer shares these, so the asynchronous boundary is in one place and behaves the same
/// everywhere: the transfer is awaited, the parsing or serialising that follows is not.
/// </remarks>
public static class DocumentStreams
{
    /// <summary>Reads a whole document into memory without blocking while it arrives.</summary>
    /// <param name="stream">Where the document comes from. Left open.</param>
    /// <param name="cancellationToken">Stops the transfer.</param>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <c>null</c>.</exception>
    /// <exception cref="OperationCanceledException">The token was cancelled.</exception>
    public static async Task<byte[]> ReadAllAsync(
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);

        return buffer.ToArray();
    }

    /// <summary>Reads a whole document into memory.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <c>null</c>.</exception>
    public static byte[] ReadAll(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);

        return buffer.ToArray();
    }

    /// <summary>Sends text to a stream as UTF-8, without blocking while it is sent.</summary>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    /// <exception cref="OperationCanceledException">The token was cancelled.</exception>
    public static async Task WriteAllAsync(
        string content,
        Stream destination,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(destination);

        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(content);
        await destination.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
    }
}
