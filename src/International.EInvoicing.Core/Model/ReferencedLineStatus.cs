using International.EInvoicing.Values;

namespace International.EInvoicing.Model;

/// <summary>
/// What happened to one line of a referenced document, when the status differs from line to line.
/// </summary>
/// <remarks>
/// The line is named as the sender chose to name it: a line number for a document with numbered lines, an
/// XPath into the document for a validator reporting where a rule failed. Both arrive in the wild, so it is
/// carried as text rather than parsed into something it may not be.
/// </remarks>
public sealed class ReferencedLineStatus : InvoiceNode
{
    /// <summary>Which line this is about.</summary>
    public IdentifierField LineIdentifier { get; set; }

    /// <summary>The line's status, as a code.</summary>
    public CodeField ProcessConditionCode { get; set; }

    /// <summary>The line's status in words, as the sender wrote it.</summary>
    public TextField ProcessCondition { get; set; }

    /// <summary>The detail behind the status: reasons, requested actions, the values at issue.</summary>
    public List<DocumentStatusDetail> StatusDetails { get; } = [];
}
