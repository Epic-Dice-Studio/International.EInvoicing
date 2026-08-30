using System.Xml.Linq;
using International.EInvoicing.Model;

namespace International.EInvoicing.Ubl;

/// <summary>
/// Which of the two UBL documents is in hand, and the four element names that differ between them.
/// </summary>
/// <remarks>
/// UBL gives a credit note its own root element and renames three things inside it; everything else is
/// identical. Reading and writing therefore differ by this record and nothing more, rather than by two
/// parallel implementations that drift apart.
/// </remarks>
internal readonly record struct UblDocumentShape(
    XName Root,
    XName TypeCode,
    XName Line,
    XName Quantity)
{
    /// <summary>An invoice: <c>ubl:Invoice</c>, with <c>InvoiceLine</c> and <c>InvoicedQuantity</c>.</summary>
    public static UblDocumentShape Invoice { get; } = new(
        UblNames.Invoice + "Invoice",
        UblNames.Cbc + "InvoiceTypeCode",
        UblNames.Cac + "InvoiceLine",
        UblNames.Cbc + "InvoicedQuantity");

    /// <summary>A credit note: its own root, with <c>CreditNoteLine</c> and <c>CreditedQuantity</c>.</summary>
    public static UblDocumentShape CreditNote { get; } = new(
        UblNames.CreditNote + "CreditNote",
        UblNames.Cbc + "CreditNoteTypeCode",
        UblNames.Cac + "CreditNoteLine",
        UblNames.Cbc + "CreditedQuantity");

    /// <summary>Whether this is the credit-note shape.</summary>
    public bool IsCreditNote => Root == CreditNote.Root;

    /// <summary>The shape a document already has, judged by its root element.</summary>
    public static UblDocumentShape Of(XElement root) =>
        root.Name == CreditNote.Root ? CreditNote : Invoice;

    /// <summary>The shape a document should be written in, judged by its type code (BT-3).</summary>
    public static UblDocumentShape For(EInvoice invoice) =>
        InvoiceTypeCodes.IsCreditNote(invoice.TypeCode.Value) ? CreditNote : Invoice;
}
