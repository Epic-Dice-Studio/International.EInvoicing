using International.EInvoicing.Values;

namespace International.EInvoicing.Model;

/// <summary>
/// The detail behind a status: why it was set, what the sender wants done about it, and which values are
/// disputed.
/// </summary>
/// <remarks>
/// A status code says an invoice was refused; this says the VAT rate was wrong, that a corrective invoice is
/// expected, and which rate was applied instead. France requires it for every status that carries a reason,
/// and numbers each detail so a recipient can tell one from another.
/// </remarks>
public sealed class DocumentStatusDetail : InvoiceNode
{
    /// <summary>The status this detail is about, when a message carries details for several.</summary>
    public CodeField ProcessConditionCode { get; set; }

    /// <summary>Why the status was set, from the list the profiling publishes.</summary>
    public CodeField ReasonCode { get; set; }

    /// <summary>The reason in words.</summary>
    public TextField Reason { get; set; }

    /// <summary>What the sender expects in return — a corrective invoice, a credit note.</summary>
    public CodeField RequestedActionCode { get; set; }

    /// <summary>The requested action in words.</summary>
    public TextField RequestedAction { get; set; }

    /// <summary>Which detail this is, counting from one.</summary>
    public Field<int> SequenceNumber { get; set; }

    /// <summary>The values this detail is about: an amount collected, a rate disputed.</summary>
    public List<DocumentStatusCharacteristic> Characteristics { get; } = [];
}
