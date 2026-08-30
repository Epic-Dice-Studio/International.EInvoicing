using International.EInvoicing.Model;
using International.EInvoicing.Values;

namespace International.EInvoicing.Countries.France.EReporting.Model;

/// <summary>
/// A day of transactions, totalled rather than listed — flux 10.3.
/// </summary>
/// <remarks>
/// This is how counter sales to consumers are reported: how much was sold on a day, in which category, split
/// by VAT rate. No buyer, no invoice numbers.
/// </remarks>
public sealed class FrTransactionSummary : InvoiceNode
{
    /// <summary>The day being reported.</summary>
    public DateField Date { get; set; }

    /// <summary>The currency, ISO 4217.</summary>
    public CodeField CurrencyCode { get; set; }

    /// <summary>When VAT becomes chargeable.</summary>
    public CodeField TaxDueDateTypeCode { get; set; }

    /// <summary>What kind of transactions these are — <c>TLB1</c>, <c>TPS1</c>, <c>TNT1</c>, <c>TMA1</c>.</summary>
    public CodeField CategoryCode { get; set; }

    /// <summary>The total excluding VAT.</summary>
    public AmountField TaxExclusiveAmount { get; set; }

    /// <summary>The total VAT.</summary>
    public AmountField TaxAmount { get; set; }

    /// <summary>How many transactions the total covers.</summary>
    public Field<int> TransactionCount { get; set; }

    /// <summary>The split by VAT rate. At least one is required, and they must add up to the totals.</summary>
    public List<FrTransactionTaxSubtotal> TaxSubtotals { get; } = [];
}
