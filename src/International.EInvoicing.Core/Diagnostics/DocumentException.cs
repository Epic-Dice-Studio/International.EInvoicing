namespace International.EInvoicing.Diagnostics;

/// <summary>
/// Thrown when a caller asked for something a document did not turn out to hold.
/// </summary>
/// <remarks>
/// Reading never throws this: it is raised only by the methods that say they will, the ones a caller reaches
/// for when they already know what a document is and would rather fail than branch. The diagnostics that
/// explain why travel with it.
/// </remarks>
public sealed class DocumentException : Exception
{
    /// <summary>Creates the exception with the diagnostics that explain it.</summary>
    /// <param name="message">What was asked for and what was there instead.</param>
    /// <param name="diagnostics">Everything reading reported.</param>
    public DocumentException(string message, IReadOnlyList<Diagnostic> diagnostics)
        : base(Describe(message, diagnostics)) =>
        Diagnostics = diagnostics ?? [];

    /// <summary>Creates the exception.</summary>
    public DocumentException(string message)
        : base(message) =>
        Diagnostics = [];

    /// <summary>Creates the exception.</summary>
    public DocumentException()
        : base("The document did not hold what was asked of it.") =>
        Diagnostics = [];

    /// <summary>Creates the exception.</summary>
    public DocumentException(string message, Exception innerException)
        : base(message, innerException) =>
        Diagnostics = [];

    /// <summary>What reading reported, so the caller does not have to go back for it.</summary>
    public IReadOnlyList<Diagnostic> Diagnostics { get; }

    private static string Describe(string message, IReadOnlyList<Diagnostic>? diagnostics)
    {
        if (diagnostics is null || diagnostics.Count == 0)
        {
            return message;
        }

        var text = new System.Text.StringBuilder(message);

        foreach (Diagnostic diagnostic in diagnostics.Where(d => d.Severity >= DiagnosticSeverity.Warning).Take(5))
        {
            text.AppendLine().Append("  ").Append(diagnostic);
        }

        return text.ToString();
    }
}
