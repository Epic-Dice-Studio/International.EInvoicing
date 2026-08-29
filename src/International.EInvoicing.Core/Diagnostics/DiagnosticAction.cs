namespace International.EInvoicing.Diagnostics;

/// <summary>What a <see cref="DiagnosticPolicy"/> decides to do with a diagnostic.</summary>
public enum DiagnosticAction
{
    /// <summary>Report it with the severity its descriptor declares.</summary>
    Keep,

    /// <summary>Report it as <see cref="DiagnosticSeverity.Error"/>, unless it is already more severe.</summary>
    Escalate,

    /// <summary>Drop it. It will not appear in the result.</summary>
    Suppress,

    /// <summary>
    /// Report it as <see cref="DiagnosticSeverity.Fatal"/>, which makes the parse result unusable. For when a
    /// document the library can read is nevertheless unacceptable to the caller.
    /// </summary>
    Fail,
}
