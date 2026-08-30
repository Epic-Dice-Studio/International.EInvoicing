using International.EInvoicing.Cdar.Model;
using International.EInvoicing.Diagnostics;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;

namespace International.EInvoicing;

/// <summary>What a document turned out to be, judged by looking at it.</summary>
public enum DocumentKind
{
    /// <summary>Nothing this library recognises.</summary>
    Unknown,

    /// <summary>An invoice or credit note in OASIS UBL 2.1.</summary>
    Ubl,

    /// <summary>An invoice in UN/CEFACT CII — the payload of Factur-X and ZUGFeRD.</summary>
    Cii,

    /// <summary>A lifecycle status message.</summary>
    Cdar,

    /// <summary>A PDF, which may carry an invoice inside it.</summary>
    Pdf,
}

/// <summary>
/// Whatever came out of reading a document, without the caller having had to say what it was.
/// </summary>
/// <remarks>
/// Exactly one of <see cref="Invoice"/> and <see cref="LifecycleStatus"/> is set when reading succeeded.
/// Both are <c>null</c> when it did not, and <see cref="Diagnostics"/> says why.
/// </remarks>
public sealed record DocumentResult
{
    /// <summary>What the document turned out to be.</summary>
    public DocumentKind Kind { get; init; }

    /// <summary>The invoice or credit note, when the document was one.</summary>
    public EInvoice? Invoice { get; init; }

    /// <summary>The lifecycle status message, when the document was one.</summary>
    public LifecycleStatusMessage? LifecycleStatus { get; init; }

    /// <summary>Everything reading reported: unknown profiles, unreadable values, unmapped elements.</summary>
    public IReadOnlyList<Diagnostic> Diagnostics { get; init; } = [];

    /// <summary>How the declared profile was resolved, and what was given up. <c>null</c> when nothing was read.</summary>
    public ProfileResolution? Profile { get; init; }

    /// <summary>Whether something usable came out.</summary>
    public bool IsUsable => Invoice is not null || LifecycleStatus is not null;

    /// <summary>Whether anything reported means the result cannot be trusted for compliance.</summary>
    public bool HasErrors => Diagnostics.Any(diagnostic => diagnostic.Severity >= DiagnosticSeverity.Error);

    /// <summary>Whether the document is a credit note rather than an invoice, read from BT-3.</summary>
    public bool IsCreditNote =>
        Invoice?.TypeCode.Value is "381" or "83" or "261" or "262" or "296" or "308" or "396";

    /// <summary>The diagnostics of at least the given severity.</summary>
    public IEnumerable<Diagnostic> OfAtLeast(DiagnosticSeverity severity) =>
        Diagnostics.Where(diagnostic => diagnostic.Severity >= severity);
}
