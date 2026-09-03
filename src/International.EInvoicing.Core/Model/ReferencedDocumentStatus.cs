using International.EInvoicing.Values;

namespace International.EInvoicing.Model;

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

    /// <summary>Which version of the document is reported on, when the sender numbers them.</summary>
    public IdentifierField DocumentVersion { get; set; }

    /// <summary>When the status takes effect, when that is not the moment the message was written.</summary>
    public DateField EffectiveDate { get; set; }

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

    /// <summary>Who received the document being reported on, when the sender named them.</summary>
    public StatusParty? Recipient { get; set; }

    /// <summary>
    /// What happened to individual lines of the document, when the status is not the same for all of them.
    /// </summary>
    /// <remarks>
    /// A message-level response points at the place in the document that failed, which is how a receiver
    /// tells "this invoice is wrong" from "line 3 of this invoice is wrong".
    /// </remarks>
    public List<ReferencedLineStatus> LineStatuses { get; } = [];

    /// <summary>The detail behind the status: reasons, requested actions, the values at issue.</summary>
    public List<DocumentStatusDetail> StatusDetails { get; } = [];
}
