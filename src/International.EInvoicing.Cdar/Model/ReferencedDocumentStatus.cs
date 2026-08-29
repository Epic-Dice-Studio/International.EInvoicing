using International.EInvoicing.Model;
using International.EInvoicing.Values;

namespace International.EInvoicing.Cdar.Model;

/// <summary>
/// What happened to one referenced document. A lifecycle message carries one of these per document it
/// reports on, which is why a single message can cover a batch.
/// </summary>
public sealed class ReferencedDocumentStatus : InvoiceNode
{
    /// <summary>The identifier of the document reported on — an invoice's BT-1.</summary>
    public IdentifierField DocumentIdentifier { get; set; }

    /// <summary>The document's status code in the sender's own vocabulary.</summary>
    public CodeField StatusCode { get; set; }

    /// <summary>The type of the document reported on — an invoice's BT-3.</summary>
    public CodeField DocumentTypeCode { get; set; }

    /// <summary>When the document was received.</summary>
    public DateTimeField ReceivedAt { get; set; }

    /// <summary>The date the document reported on was issued — an invoice's BT-2.</summary>
    public DateField DocumentIssueDate { get; set; }

    /// <summary>
    /// The lifecycle status itself, as a code. In France this is the value that matters: 200 filed,
    /// 202 received, 205 approved, 207 disputed, 210 refused, 212 collected, and so on.
    /// </summary>
    public CodeField ProcessConditionCode { get; set; }

    /// <summary>The lifecycle status in words, as the sender wrote it.</summary>
    public TextField ProcessCondition { get; set; }

    /// <summary>Why the status was set, when the sender gave a reason.</summary>
    public TextField Reason { get; set; }

    /// <summary>Who issued the document being reported on.</summary>
    public StatusParty? Issuer { get; set; }
}
