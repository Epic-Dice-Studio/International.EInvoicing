namespace International.EInvoicing.Countries.France.Invoicing;

/// <summary>
/// The <em>cadre de facturation</em> (BT-23): which invoicing case an invoice belongs to.
/// </summary>
/// <remarks>
/// France requires it on every invoice, from a closed list, and it is not a free-text label: it tells the
/// administration whether the invoice is a deposit, a corrective, a self-billed one, and whether the invoice
/// itself travels or only its data. Getting it wrong is a rejection, not a warning.
/// </remarks>
public static class FrBusinessProcess
{
    /// <summary>B1 — an ordinary invoice sent by the seller.</summary>
    public const string Invoice = "B1";

    /// <summary>S1 — an invoice sent by the seller with the data reported alongside it.</summary>
    public const string InvoiceWithReporting = "S1";

    /// <summary>M1 — an invoice in a mixed case.</summary>
    public const string MixedInvoice = "M1";

    /// <summary>B2 — a deposit invoice.</summary>
    public const string DepositInvoice = "B2";

    /// <summary>S2 — a deposit invoice with the data reported alongside it.</summary>
    public const string DepositInvoiceWithReporting = "S2";

    /// <summary>M2 — a deposit invoice in a mixed case.</summary>
    public const string MixedDepositInvoice = "M2";

    /// <summary>B4 — a self-billed invoice.</summary>
    public const string SelfBilledInvoice = "B4";

    /// <summary>S4 — a self-billed invoice with the data reported alongside it.</summary>
    public const string SelfBilledInvoiceWithReporting = "S4";

    /// <summary>M4 — a self-billed invoice in a mixed case.</summary>
    public const string MixedSelfBilledInvoice = "M4";

    private static readonly string[] Codes =
    [
        "B1", "S1", "M1", "B2", "S2", "M2", "S3", "B4", "S4", "M4",
        "S5", "S6", "B7", "S7", "B8", "S8", "M8", "B9", "S9", "M9",
    ];

    /// <summary>Every code the published rules accept.</summary>
    /// <remarks>Read from <c>BR-FR-08</c>, which is what rejects an invoice carrying anything else.</remarks>
    public static IReadOnlyList<string> All => Codes;

    /// <summary>Whether a code is one the published rules accept.</summary>
    public static bool IsKnown(string? code) => code is not null && Array.IndexOf(Codes, code) >= 0;
}
