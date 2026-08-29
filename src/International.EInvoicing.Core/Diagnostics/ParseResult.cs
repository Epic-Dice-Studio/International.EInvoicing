namespace International.EInvoicing.Diagnostics;

/// <summary>
/// What reading a document produced: the document itself when one could be produced, and everything worth
/// telling the caller about it.
/// </summary>
/// <typeparam name="T">The document type.</typeparam>
/// <param name="Value">The document, or <c>null</c> when nothing usable came out.</param>
/// <param name="Diagnostics">Everything reported, in the order it was found.</param>
public sealed record ParseResult<T>(T? Value, IReadOnlyList<Diagnostic> Diagnostics)
    where T : class
{
    /// <summary>Whether a document was produced. It may still carry errors.</summary>
    public bool IsUsable => Value is not null;

    /// <summary>Whether anything reported means the result cannot be trusted for compliance.</summary>
    public bool HasErrors => Diagnostics.Any(d => d.Severity >= DiagnosticSeverity.Error);

    /// <summary>The diagnostics of at least the given severity.</summary>
    public IEnumerable<Diagnostic> OfAtLeast(DiagnosticSeverity severity) =>
        Diagnostics.Where(d => d.Severity >= severity);

    /// <summary>The document, or the fallback when none was produced.</summary>
    public T? ValueOr(T? fallback) => Value ?? fallback;
}

/// <summary>Creates <see cref="ParseResult{T}"/> values with the document type inferred.</summary>
public static class ParseResult
{
    /// <summary>A result carrying a document and no diagnostics.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
    public static ParseResult<T> Success<T>(T value)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(value);
        return new ParseResult<T>(value, []);
    }

    /// <summary>A result carrying a document alongside what was reported while reading it.</summary>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    public static ParseResult<T> From<T>(T value, IReadOnlyList<Diagnostic> diagnostics)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(diagnostics);
        return new ParseResult<T>(value, diagnostics);
    }

    /// <summary>A result carrying no document, because reading could not produce one.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="diagnostics"/> is <c>null</c>.</exception>
    public static ParseResult<T> Failed<T>(IReadOnlyList<Diagnostic> diagnostics)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(diagnostics);
        return new ParseResult<T>(null, diagnostics);
    }
}
