using International.EInvoicing.Countries.France.EReporting.Model;

namespace International.EInvoicing.Countries.France.EReporting.Building;

/// <summary>Reports what was sold: day by day as totals, or invoice by invoice.</summary>
public sealed class FrTransactionsBuilder
{
    private readonly FrEReporting _transmission;
    private readonly FrTransactionsReport _report;

    internal FrTransactionsBuilder(FrEReporting transmission, FrReportPeriod period)
    {
        _transmission = transmission;
        _report = new FrTransactionsReport { Period = period };
        transmission.Report.Transactions = _report;
    }

    /// <summary>The platform transmitting the report.</summary>
    /// <exception cref="ArgumentException"><paramref name="platformIdentifier"/> is empty.</exception>
    public FrTransactionsBuilder From(string platformIdentifier, string name)
    {
        _transmission.From(platformIdentifier, name);
        return this;
    }

    /// <summary>The company the report is about.</summary>
    /// <exception cref="ArgumentException"><paramref name="siren"/> is empty.</exception>
    public FrTransactionsBuilder For(string siren, string name)
    {
        _transmission.For(siren, name);
        return this;
    }

    /// <summary>Anything else about the transmission itself — its identifier, its name, when it was made.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="configure"/> is <c>null</c>.</exception>
    public FrTransactionsBuilder Transmission(Action<FrEReporting> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        configure(_transmission);
        return this;
    }

    /// <summary>
    /// A day of transactions, totalled — flux 10.3. This is how sales to consumers are reported.
    /// </summary>
    /// <param name="day">The day being reported.</param>
    /// <param name="categoryCode">What kind of transactions, from <see cref="FrEReportCodes"/>.</param>
    /// <param name="split">The split by VAT rate. The totals are added up from it.</param>
    /// <param name="currencyCode">The currency. Euro unless said otherwise.</param>
    /// <exception cref="ArgumentNullException"><paramref name="split"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">The split is empty.</exception>
    public FrTransactionsBuilder Day(
        DateOnly day,
        string categoryCode,
        Action<FrTaxSplitBuilder> split,
        string currencyCode = "EUR")
    {
        ArgumentNullException.ThrowIfNull(split);

        var builder = new FrTaxSplitBuilder();
        split(builder);

        if (builder.Entries.Count == 0)
        {
            throw new ArgumentException(
                "A day of transactions is reported split by VAT rate, at least once.",
                nameof(split));
        }

        var summary = new FrTransactionSummary
        {
            Date = day,
            CurrencyCode = currencyCode,
            CategoryCode = categoryCode,
            TaxExclusiveAmount = builder.TaxableTotal,
            TaxAmount = builder.TaxTotal,
        };

        foreach ((decimal rate, decimal taxable, decimal tax) in builder.Entries)
        {
            summary.TaxSubtotals.Add(new FrTransactionTaxSubtotal
            {
                Percent = rate,
                TaxableAmount = taxable,
                TaxAmount = tax,
            });
        }

        _report.Summaries.Add(summary);
        return this;
    }

    /// <summary>How many transactions the last day reported covers.</summary>
    /// <exception cref="InvalidOperationException">No day has been reported yet.</exception>
    public FrTransactionsBuilder Counting(int transactions)
    {
        if (_report.Summaries.Count == 0)
        {
            throw new InvalidOperationException("Report a day with Day(...) before counting its transactions.");
        }

        _report.Summaries[^1].TransactionCount = transactions;
        return this;
    }

    /// <summary>An invoice reported one by one — flux 10.1.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="configure"/> is <c>null</c>.</exception>
    public FrTransactionsBuilder Invoice(Action<FrReportedInvoiceBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new FrReportedInvoiceBuilder();
        configure(builder);
        _report.Invoices.Add(builder.Complete());
        return this;
    }

    /// <summary>The finished transmission.</summary>
    /// <exception cref="InvalidOperationException">The sender or the company was not named.</exception>
    public FrEReport Build() => _transmission.Complete(_report.Period.StartDate.Value ?? default);
}
