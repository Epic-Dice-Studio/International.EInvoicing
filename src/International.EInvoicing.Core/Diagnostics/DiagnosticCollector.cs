namespace International.EInvoicing.Diagnostics;

/// <summary>
/// Gathers diagnostics while a document is read, applying the caller's policy as they arrive. Readers hold
/// one of these instead of throwing.
/// </summary>
public sealed class DiagnosticCollector
{
    private readonly List<Diagnostic> _diagnostics = [];

    /// <summary>Creates a collector using <paramref name="policy"/>, or the balanced policy when omitted.</summary>
    public DiagnosticCollector(DiagnosticPolicy? policy = null) => Policy = policy ?? DiagnosticPolicy.Balanced;

    /// <summary>The policy applied to every diagnostic added.</summary>
    public DiagnosticPolicy Policy { get; }

    /// <summary>Everything reported so far, in the order it was found.</summary>
    public IReadOnlyList<Diagnostic> Diagnostics => _diagnostics;

    /// <summary>Whether anything reported prevents the document from being produced.</summary>
    public bool HasFatal => _diagnostics.Any(d => d.Severity == DiagnosticSeverity.Fatal);

    /// <summary>Whether anything reported means the result cannot be trusted for compliance.</summary>
    public bool HasErrors => _diagnostics.Any(d => d.Severity >= DiagnosticSeverity.Error);

    /// <summary>Applies the policy and records the result. Suppressed diagnostics are dropped.</summary>
    /// <returns>The diagnostic as recorded, or <c>null</c> when the policy suppressed it.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="diagnostic"/> is <c>null</c>.</exception>
    public Diagnostic? Add(Diagnostic diagnostic)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);

        Diagnostic? applied = Policy.Apply(diagnostic);
        if (applied is not null)
        {
            _diagnostics.Add(applied);
        }

        return applied;
    }

    /// <summary>Creates the diagnostic from its descriptor, then records it.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="descriptor"/> is <c>null</c>.</exception>
    public Diagnostic? Add(DiagnosticDescriptor descriptor, params object?[] messageArguments) =>
        Add(Diagnostic.Create(descriptor, messageArguments));

    /// <summary>Pairs the collected diagnostics with the document that was produced, if any.</summary>
    public ParseResult<T> ToResult<T>(T? value)
        where T : class => new(HasFatal ? null : value, [.. _diagnostics]);
}
