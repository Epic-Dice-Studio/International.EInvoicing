namespace International.EInvoicing.Countries.France.EReporting.Building;

/// <summary>
/// A split by VAT rate, which is how every total in e-reporting is reported.
/// </summary>
/// <remarks>
/// The totals are added up from the split rather than asked for separately: the published rules check that
/// they agree, and two numbers a caller has to keep consistent are two numbers that drift apart.
/// </remarks>
public sealed class FrTaxSplitBuilder
{
    private readonly List<(decimal Rate, decimal Taxable, decimal Tax)> _entries = [];

    internal IReadOnlyList<(decimal Rate, decimal Taxable, decimal Tax)> Entries => _entries;

    internal decimal TaxableTotal => _entries.Sum(entry => entry.Taxable);

    internal decimal TaxTotal => _entries.Sum(entry => entry.Tax);

    /// <summary>What was sold at one rate, and the VAT on it.</summary>
    /// <param name="ratePercent">The VAT rate, as a percentage.</param>
    /// <param name="taxableAmount">What the rate applies to.</param>
    /// <param name="taxAmount">The VAT it comes to.</param>
    public FrTaxSplitBuilder At(decimal ratePercent, decimal taxableAmount, decimal taxAmount)
    {
        _entries.Add((ratePercent, taxableAmount, taxAmount));
        return this;
    }

    /// <summary>
    /// What was sold at one rate, with the VAT worked out from it and rounded to the cent.
    /// </summary>
    public FrTaxSplitBuilder At(decimal ratePercent, decimal taxableAmount) =>
        At(ratePercent, taxableAmount, Math.Round(taxableAmount * ratePercent / 100m, 2, MidpointRounding.AwayFromZero));
}
