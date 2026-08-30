namespace International.EInvoicing.Countries.France.EReporting;

/// <summary>
/// The closed code lists e-reporting uses, as the DGFiP publishes them.
/// </summary>
/// <remarks>
/// They are here to be chosen from rather than typed out. The published rules remain the authority; nothing
/// here refuses a value they would accept.
/// </remarks>
public static class FrEReportCodes
{
    /// <summary>The profile every reported invoice declares.</summary>
    public const string ProfileIdentifier = "urn.cpro.gouv.fr:1p0:ereporting";

    /// <summary>A first transmission for the period.</summary>
    public const string InitialTransmission = "IN";

    /// <summary>A transmission replacing an earlier one.</summary>
    public const string Replacement = "RE";

    /// <summary>Sales at a counter or online to consumers.</summary>
    public const string RetailTransactions = "TLB1";

    /// <summary>Services supplied.</summary>
    public const string ServiceTransactions = "TPS1";

    /// <summary>Transactions outside the scope of VAT.</summary>
    public const string OutOfScopeTransactions = "TNT1";

    /// <summary>Transactions of a mixed kind.</summary>
    public const string MixedTransactions = "TMA1";

    /// <summary>The VAT rates a report may carry, as percentages.</summary>
    public static IReadOnlyList<decimal> VatRates { get; } =
        [0m, 0.9m, 1.05m, 1.75m, 2.1m, 5.5m, 7m, 8.5m, 9.2m, 9.6m, 10m, 13m, 19.6m, 20m, 20.6m];

    /// <summary>The VAT category codes a report may carry.</summary>
    public static IReadOnlyList<string> VatCategories { get; } = ["S", "E", "AE", "K", "G", "O", "Z"];

    /// <summary>The invoice type codes a report may carry.</summary>
    public static IReadOnlyList<string> InvoiceTypes { get; } =
    [
        "261", "380", "381", "384", "386", "389", "393", "396",
        "471", "472", "473", "500", "501", "502", "503",
    ];

    /// <summary>The invoicing frameworks a reported invoice may belong to.</summary>
    public static IReadOnlyList<string> BusinessProcesses { get; } =
        ["B1", "S1", "M1", "B2", "S2", "M2", "B4", "S4", "M4", "S5", "S6", "B7", "S7"];

    /// <summary>The codes saying when VAT becomes chargeable.</summary>
    public static IReadOnlyList<string> TaxDueDateTypes { get; } = ["5", "29", "72", "3", "35", "432"];

    /// <summary>The categories a day of totalled transactions may fall under.</summary>
    public static IReadOnlyList<string> TransactionCategories { get; } =
        [RetailTransactions, ServiceTransactions, OutOfScopeTransactions, MixedTransactions];
}
