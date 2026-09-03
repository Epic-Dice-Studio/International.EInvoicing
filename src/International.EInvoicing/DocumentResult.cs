using System.Diagnostics.CodeAnalysis;
using International.EInvoicing.Diagnostics;
using International.EInvoicing.Documents;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;

namespace International.EInvoicing;

/// <summary>What a document turned out to be, judged by looking at it.</summary>
public enum DocumentKind
{
    /// <summary>Nothing this library recognises.</summary>
    Unknown,

    /// <summary>An invoice in OASIS UBL 2.1.</summary>
    Ubl,

    /// <summary>A credit note in OASIS UBL 2.1, which has its own root element.</summary>
    UblCreditNote,

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

    /// <summary>
    /// The invoice as a person reads it — the PDF a hybrid invoice arrived in. <c>null</c> for a document
    /// that arrived as bare XML, which has no readable copy to hand back.
    /// </summary>
    /// <remarks>
    /// The container used to be dropped once the XML was out of it, which left a caller holding an invoice
    /// they could not show anybody. This is the invoice itself, readable; what the invoice carries
    /// <em>beside</em> itself is <see cref="EInvoice.SupportingDocuments"/>.
    /// </remarks>
    public InvoiceRendition? Rendition { get; init; }

    /// <summary>Everything reading reported: unknown profiles, unreadable values, unmapped elements.</summary>
    public IReadOnlyList<Diagnostic> Diagnostics { get; init; } = [];

    /// <summary>How the declared profile was resolved, and what was given up. <c>null</c> when nothing was read.</summary>
    public ProfileResolution? Profile { get; init; }

    /// <summary>Whether something usable came out.</summary>
    public bool IsUsable => Invoice is not null || LifecycleStatus is not null;

    /// <summary>Whether anything reported means the result cannot be trusted for compliance.</summary>
    public bool HasErrors => Diagnostics.Any(diagnostic => diagnostic.Severity >= DiagnosticSeverity.Error);

    /// <summary>
    /// Whether the document is a credit note rather than an invoice.
    /// </summary>
    /// <remarks>
    /// In UBL the root element says so outright; in CII it is the type code (BT-3) that does. Both are
    /// consulted, so a credit note is recognised whichever syntax it arrived in.
    /// </remarks>
    public bool IsCreditNote =>
        Kind == DocumentKind.UblCreditNote || InvoiceTypeCodes.IsCreditNote(Invoice?.TypeCode.Value);

    /// <summary>The diagnostics of at least the given severity.</summary>
    public IEnumerable<Diagnostic> OfAtLeast(DiagnosticSeverity severity) =>
        Diagnostics.Where(diagnostic => diagnostic.Severity >= severity);

    /// <summary>What went wrong, when something did.</summary>
    public IEnumerable<Diagnostic> Errors => OfAtLeast(DiagnosticSeverity.Error);

    /// <summary>What was worth mentioning without stopping anything.</summary>
    public IEnumerable<Diagnostic> Warnings =>
        Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Warning);

    /// <summary>The invoice, when the document was one.</summary>
    /// <example>
    /// <code>
    /// if (result.TryGetInvoice(out EInvoice? invoice))
    /// {
    ///     Console.WriteLine(invoice.Number.Value);
    /// }
    /// </code>
    /// </example>
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

    /// <summary>
    /// The invoice, or an exception explaining what arrived instead.
    /// </summary>
    /// <remarks>
    /// For the code path that already knows what it was handed — an endpoint that only ever receives
    /// invoices — and would rather fail loudly than branch. Everywhere else, <see cref="TryGetInvoice"/>.
    /// </remarks>
    /// <exception cref="DocumentException">The document was not a readable invoice.</exception>
    public EInvoice RequireInvoice() =>
        Invoice ?? throw new DocumentException(
            $"Expected an invoice; the document was read as {Kind}.",
            Diagnostics);

    /// <summary>The lifecycle status message, or an exception explaining what arrived instead.</summary>
    /// <exception cref="DocumentException">The document was not a readable lifecycle status message.</exception>
    public LifecycleStatusMessage RequireLifecycleStatus() =>
        LifecycleStatus ?? throw new DocumentException(
            $"Expected a lifecycle status message; the document was read as {Kind}.",
            Diagnostics);

    /// <summary>Takes the result apart, for a caller that wants to switch on what arrived.</summary>
    /// <example>
    /// <code>
    /// var (kind, invoice, status) = einvoicing.Read(stream);
    /// </code>
    /// </example>
    public void Deconstruct(out DocumentKind kind, out EInvoice? invoice, out LifecycleStatusMessage? status)
    {
        kind = Kind;
        invoice = Invoice;
        status = LifecycleStatus;
    }
}
