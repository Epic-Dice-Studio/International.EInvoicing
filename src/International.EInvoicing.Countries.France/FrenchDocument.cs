using System.Diagnostics.CodeAnalysis;
using International.EInvoicing.Countries.France.EReporting.Model;
using International.EInvoicing.Diagnostics;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;

namespace International.EInvoicing.Countries.France;

/// <summary>What a French document turned out to be.</summary>
public enum FrenchDocumentKind
{
    /// <summary>Nothing this library recognises.</summary>
    Unknown,

    /// <summary>An invoice, in UBL, CII, or extracted from a Factur-X PDF.</summary>
    Invoice,

    /// <summary>A credit note.</summary>
    CreditNote,

    /// <summary>A lifecycle status message — a CDAR, carrying a <em>statut de cycle de vie</em>.</summary>
    LifecycleStatus,

    /// <summary>An e-reporting transmission — <em>flux 10</em>.</summary>
    EReport,
}

/// <summary>
/// Whatever came out of reading a French document, without the caller having had to say what it was.
/// </summary>
/// <remarks>
/// France exchanges four things and the reform requires all of them: invoices, credit notes, lifecycle
/// statuses and e-reporting transmissions. They are four different documents — two syntaxes, a third
/// vocabulary, and one that carries no XML namespace at all — and a French integration receives all four
/// through the same channel. So this reads all four.
/// </remarks>
public sealed record FrenchDocument
{
    /// <summary>What the document turned out to be.</summary>
    public FrenchDocumentKind Kind { get; init; }

    /// <summary>The invoice or credit note, when the document was one.</summary>
    public EInvoice? Invoice { get; init; }

    /// <summary>The lifecycle status message, when the document was one.</summary>
    public LifecycleStatusMessage? LifecycleStatus { get; init; }

    /// <summary>The e-reporting transmission, when the document was one.</summary>
    public FrEReport? EReport { get; init; }

    /// <summary>Everything reading reported.</summary>
    public IReadOnlyList<Diagnostic> Diagnostics { get; init; } = [];

    /// <summary>How the declared profile was resolved. <c>null</c> for a document that declares none.</summary>
    public ProfileResolution? Profile { get; init; }

    /// <summary>Whether something usable came out.</summary>
    public bool IsUsable => Kind != FrenchDocumentKind.Unknown;

    /// <summary>What went wrong, when something did.</summary>
    public IEnumerable<Diagnostic> Errors =>
        Diagnostics.Where(diagnostic => diagnostic.Severity >= DiagnosticSeverity.Error);

    /// <summary>The invoice or credit note, when the document was one.</summary>
    public bool TryGetInvoice([NotNullWhen(true)] out EInvoice? invoice)
    {
        invoice = Invoice;
        return invoice is not null;
    }

    /// <summary>The lifecycle status message, when the document was one.</summary>
    public bool TryGetLifecycleStatus([NotNullWhen(true)] out LifecycleStatusMessage? status)
    {
        status = LifecycleStatus;
        return status is not null;
    }

    /// <summary>The e-reporting transmission, when the document was one.</summary>
    public bool TryGetEReport([NotNullWhen(true)] out FrEReport? report)
    {
        report = EReport;
        return report is not null;
    }

    /// <summary>Takes the result apart, for a caller that wants to switch on what arrived.</summary>
    public void Deconstruct(
        out FrenchDocumentKind kind,
        out EInvoice? invoice,
        out LifecycleStatusMessage? status,
        out FrEReport? report)
    {
        kind = Kind;
        invoice = Invoice;
        status = LifecycleStatus;
        report = EReport;
    }
}
