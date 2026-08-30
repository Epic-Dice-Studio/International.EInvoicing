namespace International.EInvoicing.Countries.France.Lifecycle;

/// <summary>What the sender of a status expects the other party to do about it.</summary>
public static class FrRequestedAction
{
    /// <summary>No action.</summary>
    public const string None = "NOA";

    /// <summary>Issue a partial corrective invoice.</summary>
    public const string PartialCorrectiveInvoice = "PIN";

    /// <summary>Issue a corrective invoice.</summary>
    public const string CorrectiveInvoice = "NIN";

    /// <summary>Issue a full credit note.</summary>
    public const string FullCreditNote = "CNF";

    /// <summary>Issue a partial credit note.</summary>
    public const string PartialCreditNote = "CNP";

    /// <summary>Issue a credit note and a new invoice.</summary>
    public const string CreditNoteAndNewInvoice = "CNA";

    /// <summary>Something else, spelled out in words alongside.</summary>
    public const string Other = "OTH";
}
