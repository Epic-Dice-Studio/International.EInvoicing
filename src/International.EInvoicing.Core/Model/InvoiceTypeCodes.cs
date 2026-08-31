using System.Collections.Frozen;

namespace International.EInvoicing.Model;

/// <summary>
/// The document type codes (BT-3) that decide what a document is.
/// </summary>
/// <remarks>
/// <para>
/// EN 16931 makes an invoice and a credit note the same document with a different code, which is why the
/// model has one type for both. The syntaxes disagree: CII keeps them in one root element and UBL gives a
/// credit note its own, so writing one means reading the code first.
/// </para>
/// <para>
/// Both lists below are the ones <c>BR-CL-01</c> tests, taken from the EN 16931 artefacts this library
/// ships rather than transcribed from UNTDID 1001 — the standard allows a subset, and which subset is a
/// question only the artefact answers. <c>InvoiceTypeCodesTests</c> compares the two on every build.
/// </para>
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

    /// <summary>A self-billed credit note.</summary>
    public const string SelfBilledCreditNote = "261";

    /// <summary>A prepayment invoice.</summary>
    public const string PrepaymentInvoice = "386";

    private static readonly string[] Invoices =
    [
        "71", "80", "81", "82", "84", "102", "130", "202", "203", "204", "211", "218", "219", "295", "325",
        "326", "331", "380", "382", "383", "384", "385", "386", "387", "388", "389", "390", "393", "394",
        "395", "456", "457", "471", "472", "473", "500", "501", "527", "553", "575", "623", "633", "751",
        "780", "817", "870", "875", "876", "877", "935",
    ];

    private static readonly string[] CreditNotes =
        ["81", "83", "261", "262", "296", "308", "381", "396", "420", "458", "502", "503", "532"];

    private static readonly FrozenSet<string> InvoiceSet = Invoices.ToFrozenSet(StringComparer.Ordinal);

    private static readonly FrozenSet<string> CreditNoteSet = CreditNotes.ToFrozenSet(StringComparer.Ordinal);

    /// <summary>Every code EN 16931 accepts on an invoice, in the order the rule lists them.</summary>
    public static IReadOnlyList<string> ForInvoices => Invoices;

    /// <summary>Every code EN 16931 accepts on a credit note.</summary>
    public static IReadOnlyList<string> ForCreditNotes => CreditNotes;

    /// <summary>
    /// Whether a type code names a credit note rather than an invoice.
    /// </summary>
    /// <remarks>
    /// <c>81</c> is in both lists — a code the two document kinds share — so it answers <c>true</c> here and
    /// <see cref="IsInvoice"/> answers <c>true</c> as well. In UBL the root element settles it; in CII, only
    /// the code is available, and treating a shared code as a credit note is the safer reading of the two.
    /// </remarks>
    public static bool IsCreditNote(string? typeCode) => typeCode is not null && CreditNoteSet.Contains(typeCode);

    /// <summary>Whether a type code names an invoice.</summary>
    public static bool IsInvoice(string? typeCode) => typeCode is not null && InvoiceSet.Contains(typeCode);

    /// <summary>Whether EN 16931 accepts this code on either kind of document.</summary>
    public static bool IsKnown(string? typeCode) => IsInvoice(typeCode) || IsCreditNote(typeCode);
}
