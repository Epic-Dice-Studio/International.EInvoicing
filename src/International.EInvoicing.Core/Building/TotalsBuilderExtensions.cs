using International.EInvoicing.Model;
using International.EInvoicing.Values;

namespace International.EInvoicing.Building;

/// <summary>
/// Works out what an invoice adds up to, so the arithmetic is done once and in one place.
/// </summary>
/// <remarks>
/// The totals rules — BR-CO-10 through BR-CO-17 — are where documents most often stop validating, and the
/// reason is almost never the arithmetic itself: it is a total typed in beside the lines it is supposed to
/// summarise, and then one of the two changes. Deriving them removes the chance to disagree.
/// </remarks>
public static class TotalsBuilderExtensions
{
    /// <summary>
    /// Groups the lines into a VAT breakdown (BG-23), one entry per category and rate.
    /// </summary>
    /// <remarks>
    /// Document-level allowances and charges are applied to the entry with the same category and rate, as
    /// EN 16931 requires: a discount on the whole invoice reduces the base it was taken from, not every base.
    /// Any breakdown already present is replaced.
    /// </remarks>
    /// <param name="builder">The invoice being built.</param>
    /// <param name="decimals">How many decimals the tax is rounded to. Two, unless your currency says otherwise.</param>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
    public static EInvoiceBuilder WithComputedVatBreakdown(this EInvoiceBuilder builder, int decimals = 2)
    {
        ArgumentNullException.ThrowIfNull(builder);

        EInvoice invoice = builder.Build();
        var bases = new Dictionary<(string Category, decimal Rate), decimal>();

        foreach (InvoiceLine line in invoice.Lines)
        {
            Add(bases, line.VatCategoryCode.Value, line.VatRate.Value, line.NetAmount.Value ?? 0m);
        }

        foreach (AllowanceCharge allowance in invoice.AllowancesAndCharges)
        {
            decimal amount = allowance.Amount.Value ?? 0m;
            Add(
                bases,
                allowance.VatCategoryCode.Value,
                allowance.VatRate.Value,
                allowance.IsCharge ? amount : -amount);
        }

        invoice.VatBreakdown.Clear();

        foreach (((string category, decimal rate), decimal taxable) in bases.OrderBy(entry => entry.Key.Rate))
        {
            invoice.VatBreakdown.Add(new VatBreakdownEntry
            {
                CategoryCode = category,
                Rate = rate,
                TaxableAmount = builder.Amount(Round(taxable, decimals)),
                TaxAmount = builder.Amount(Round(taxable * rate / 100m, decimals)),
            });
        }

        return builder;
    }

    /// <summary>
    /// Works the document totals (BG-22) out from the lines, the document-level allowances and charges, and
    /// the VAT breakdown.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Sets BT-106 to BT-115. Amounts a caller set that are not derived — the prepaid amount (BT-113) and the
    /// rounding amount (BT-114) — are kept and taken into account.
    /// </para>
    /// <para>
    /// Call it last, after the lines and the breakdown; with
    /// <see cref="WithComputedVatBreakdown"/> before it when the breakdown should come from the lines too.
    /// </para>
    /// </remarks>
    /// <param name="builder">The invoice being built.</param>
    /// <param name="decimals">How many decimals amounts are rounded to.</param>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
    public static EInvoiceBuilder WithComputedTotals(this EInvoiceBuilder builder, int decimals = 2)
    {
        ArgumentNullException.ThrowIfNull(builder);

        EInvoice invoice = builder.Build();
        DocumentTotals totals = invoice.Totals;

        decimal lines = invoice.Lines.Sum(line => line.NetAmount.Value ?? 0m);
        decimal allowances = invoice.AllowancesAndCharges
            .Where(entry => !entry.IsCharge)
            .Sum(entry => entry.Amount.Value ?? 0m);
        decimal charges = invoice.AllowancesAndCharges
            .Where(entry => entry.IsCharge)
            .Sum(entry => entry.Amount.Value ?? 0m);

        decimal exclusive = lines - allowances + charges;
        decimal tax = invoice.VatBreakdown.Sum(entry => entry.TaxAmount.Value ?? 0m);
        decimal inclusive = exclusive + tax;
        decimal prepaid = totals.PrepaidAmount.Value ?? 0m;
        decimal rounding = totals.RoundingAmount.Value ?? 0m;

        totals.LineTotalAmount = builder.Amount(Round(lines, decimals));
        totals.TaxExclusiveAmount = builder.Amount(Round(exclusive, decimals));
        totals.TaxAmount = builder.Amount(Round(tax, decimals));
        totals.TaxInclusiveAmount = builder.Amount(Round(inclusive, decimals));
        totals.DuePayableAmount = builder.Amount(Round(inclusive - prepaid + rounding, decimals));

        if (allowances != 0m)
        {
            totals.AllowanceTotalAmount = builder.Amount(Round(allowances, decimals));
        }

        if (charges != 0m)
        {
            totals.ChargeTotalAmount = builder.Amount(Round(charges, decimals));
        }

        return builder;
    }

    private static void Add(
        Dictionary<(string Category, decimal Rate), decimal> bases,
        string? category,
        decimal? rate,
        decimal amount)
    {
        (string, decimal) key = (category ?? "S", rate ?? 0m);
        bases[key] = bases.TryGetValue(key, out decimal running) ? running + amount : amount;
    }

    private static decimal Round(decimal value, int decimals) =>
        Math.Round(value, decimals, MidpointRounding.AwayFromZero);
}
