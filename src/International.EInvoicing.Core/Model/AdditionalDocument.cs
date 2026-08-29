using International.EInvoicing.Values;

namespace International.EInvoicing.Model;

/// <summary>A supporting document, referenced or attached (BG-24).</summary>
public sealed class AdditionalDocument : InvoiceNode
{
    /// <summary>BT-122 — the supporting document's identifier.</summary>
    public IdentifierField Identifier { get; set; }

    /// <summary>BT-123 — what the supporting document is.</summary>
    public TextField Description { get; set; }

    /// <summary>BT-124 — where the document can be retrieved, when it is not attached.</summary>
    public TextField ExternalLocation { get; set; }

    /// <summary>
    /// BT-125 — the attached document itself, with its media type and file name. Attachments are the largest
    /// thing an invoice can carry, and are bounded by <see cref="Xml.DocumentLimits.MaxAttachmentBytes"/>.
    /// </summary>
    public BinaryField Attachment { get; set; }
}
