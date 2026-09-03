using International.EInvoicing.Values;

namespace International.EInvoicing.Model;

/// <summary>A supporting document, referenced or attached (BG-24).</summary>
public sealed class AdditionalDocument : InvoiceNode
{
    /// <summary>BT-122 — the supporting document's identifier.</summary>
    public IdentifierField Identifier { get; set; }

    /// <summary>BT-123 — what the supporting document is.</summary>
    public TextField Description { get; set; }

    /// <summary>
    /// What kind of document it is, as a code, when the sender gives one.
    /// </summary>
    /// <remarks>
    /// EN 16931 has no term for it. UBL's post-award documents do — an order agreement names the product
    /// description it was agreed against by type as well as by name.
    /// </remarks>
    public CodeField TypeCode { get; set; }

    /// <summary>
    /// What kind of reference the identifier is — an order number, a contract number, a despatch note.
    /// </summary>
    /// <remarks>
    /// UNTDID 1153. The identifier alone says which document; this says what that document is to the sender,
    /// which is what tells a receiver whether it may act on it.
    /// </remarks>
    public CodeField ReferenceTypeCode { get; set; }

    /// <summary>The line of the referenced document, when the reference is to one line of it.</summary>
    public IdentifierField LineReference { get; set; }

    /// <summary>BT-124 — where the document can be retrieved, when it is not attached.</summary>
    public TextField ExternalLocation { get; set; }

    /// <summary>
    /// BT-125 — the attached document itself, with its media type and file name. Attachments are the largest
    /// thing an invoice can carry, and are bounded by <see cref="Xml.DocumentLimits.MaxAttachmentBytes"/>.
    /// </summary>
    public BinaryField Attachment { get; set; }
}
