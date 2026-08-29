namespace International.EInvoicing.Diagnostics;

/// <summary>How much a diagnostic should worry the caller.</summary>
public enum DiagnosticSeverity
{
    /// <summary>Something was noted and handled. Nothing is lost.</summary>
    Info,

    /// <summary>A fallback was applied. The document is usable; the affected data deserves a look.</summary>
    Warning,

    /// <summary>The result is incomplete or cannot be trusted for compliance purposes.</summary>
    Error,

    /// <summary>No usable document was produced.</summary>
    Fatal,
}
