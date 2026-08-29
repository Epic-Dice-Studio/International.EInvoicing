using International.EInvoicing.Values;

namespace International.EInvoicing.Model;

/// <summary>A free-text note attached to the invoice (BG-1).</summary>
public sealed class InvoiceNote : InvoiceNode
{
    /// <summary>BT-21 — subject code, saying what the note is about.</summary>
    public CodeField SubjectCode { get; set; }

    /// <summary>BT-22 — the note itself.</summary>
    public TextField Text { get; set; }
}
