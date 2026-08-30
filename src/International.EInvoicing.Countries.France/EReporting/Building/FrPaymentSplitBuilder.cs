namespace International.EInvoicing.Countries.France.EReporting.Building;

/// <summary>What a payment came to at each VAT rate.</summary>
/// <remarks>
/// A payment reports the amount collected per rate, not a taxable base and a tax: the administration works
/// out the VAT due from the rate, which is the whole point of reporting collection separately.
/// </remarks>
public sealed class FrPaymentSplitBuilder
{
    private readonly List<(decimal Rate, decimal Amount, string Currency)> _entries = [];

    internal IReadOnlyList<(decimal Rate, decimal Amount, string Currency)> Entries => _entries;

    /// <summary>What was collected at one rate.</summary>
    /// <param name="ratePercent">The VAT rate, as a percentage.</param>
    /// <param name="amount">The amount collected.</param>
    /// <param name="currencyCode">The currency. Only euro is accepted by the rules.</param>
    public FrPaymentSplitBuilder At(decimal ratePercent, decimal amount, string currencyCode = "EUR")
    {
        _entries.Add((ratePercent, amount, currencyCode));
        return this;
    }
}
