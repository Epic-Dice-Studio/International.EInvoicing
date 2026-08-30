namespace International.EInvoicing.Model;

/// <summary>
/// The document type codes (BT-3) that decide what a document is.
/// </summary>
/// <remarks>
/// EN 16931 makes an invoice and a credit note the same document with a different code, which is why the
/// model has one type for both. The syntaxes disagree: CII keeps them in one root element and UBL gives a
/// credit note its own, so writing one means reading the code first.
/// </remarks>
public static class InvoiceTypeCodes
{
    /// <summary>A commercial invoice.</summary>
    public const string CommercialInvoice = "380";

    /// <summary>A credit note.</summary>
    public const string CreditNote = "381";

    /// <summary>A corrected invoice.</summary>
    public const string CorrectedInvoice = "384";

    /// <summary>A self-billed invoice.</summary>
    public const string SelfBilledInvoice = "389";

    private static readonly string[] CreditNoteCodes =
        ["81", "83", "261", "262", "296", "308", "381", "396"];

    /// <summary>Whether a type code names a credit note rather than an invoice.</summary>
    /// <remarks>
    /// From UNTDID 1001, restricted to the codes EN 16931 allows: a credit note (381), a self-billed one
    /// (261), a credit note related to goods or services (81, 83), and the corrective forms.
    /// </remarks>
    public static bool IsCreditNote(string? typeCode) =>
        typeCode is not null && Array.IndexOf(CreditNoteCodes, typeCode) >= 0;
}
