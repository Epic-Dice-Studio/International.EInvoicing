using International.EInvoicing.Cdar.Model;
using International.EInvoicing.Diagnostics;
using International.EInvoicing.Model;
using International.EInvoicing.Validation;

namespace International.EInvoicing.Playground.Services;

/// <summary>Everything the site learned about one document.</summary>
public sealed record InspectionResult
{
    /// <summary>What the document turned out to be.</summary>
    public DocumentKind Kind { get; init; }

    /// <summary>The invoice, when the document was one.</summary>
    public EInvoice? Invoice { get; init; }

    /// <summary>The lifecycle message, when the document was one.</summary>
    public LifecycleStatusMessage? Status { get; init; }

    /// <summary>What reading it reported: unknown profiles, unreadable values, unmapped elements.</summary>
    public IReadOnlyList<Diagnostic> Diagnostics { get; init; } = [];

    /// <summary>What validating it found, and which rule sets ran.</summary>
    public ValidationReport? Validation { get; init; }

    /// <summary>Why nothing could be read, when nothing could.</summary>
    public string? Failure { get; init; }

    /// <summary>Whether something usable came out.</summary>
    public bool IsUsable => Invoice is not null || Status is not null;
}
