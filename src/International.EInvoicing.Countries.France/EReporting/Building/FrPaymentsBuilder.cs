using International.EInvoicing.Countries.France.EReporting.Model;

namespace International.EInvoicing.Countries.France.EReporting.Building;

/// <summary>Reports when the money arrived: against an invoice, or against a day's transactions.</summary>
public sealed class FrPaymentsBuilder
{
    private readonly FrEReporting _transmission;
    private readonly FrPaymentsReport _report;

    internal FrPaymentsBuilder(FrEReporting transmission, FrReportPeriod period)
    {
        _transmission = transmission;
        _report = new FrPaymentsReport { Period = period };
        transmission.Report.Payments = _report;
    }

    /// <summary>The platform transmitting the report.</summary>
    /// <exception cref="ArgumentException"><paramref name="platformIdentifier"/> is empty.</exception>
    public FrPaymentsBuilder From(string platformIdentifier, string name)
    {
        _transmission.From(platformIdentifier, name);
        return this;
    }

    /// <summary>The company the report is about.</summary>
    /// <exception cref="ArgumentException"><paramref name="siren"/> is empty.</exception>
    public FrPaymentsBuilder For(string siren, string name)
    {
        _transmission.For(siren, name);
        return this;
    }

    /// <summary>Anything else about the transmission itself.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="configure"/> is <c>null</c>.</exception>
    public FrPaymentsBuilder Transmission(Action<FrEReporting> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        configure(_transmission);
        return this;
    }

    /// <summary>What was collected against one invoice — flux 10.2.</summary>
    /// <param name="invoiceNumber">The invoice the payment settles.</param>
    /// <param name="invoiceIssueDate">When that invoice was issued.</param>
    /// <param name="paidOn">When the payment was collected.</param>
    /// <param name="split">What was collected, split by VAT rate.</param>
    /// <exception cref="ArgumentException">The invoice number is empty, or the split is.</exception>
    public FrPaymentsBuilder ForInvoice(
        string invoiceNumber,
        DateOnly invoiceIssueDate,
        DateOnly paidOn,
        Action<FrPaymentSplitBuilder> split)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(invoiceNumber);

        _report.Invoices.Add(new FrReportedInvoicePayment
        {
            InvoiceIdentifier = invoiceNumber,
            InvoiceIssueDate = invoiceIssueDate,
            Payment = Payment(paidOn, split, nameof(split)),
        });

        return this;
    }

    /// <summary>What was collected with no invoice behind it — flux 10.4.</summary>
    /// <param name="paidOn">When the payment was collected.</param>
    /// <param name="split">What was collected, split by VAT rate.</param>
    /// <exception cref="ArgumentException">The split is empty.</exception>
    public FrPaymentsBuilder ForTransactions(DateOnly paidOn, Action<FrPaymentSplitBuilder> split)
    {
        _report.Transactions.Add(Payment(paidOn, split, nameof(split)));
        return this;
    }

    /// <summary>The finished transmission.</summary>
    /// <exception cref="InvalidOperationException">The sender or the company was not named.</exception>
    public FrEReport Build() => _transmission.Complete(_report.Period.StartDate.Value ?? default);

    private static FrReportedPayment Payment(
        DateOnly paidOn,
        Action<FrPaymentSplitBuilder> split,
        string parameter)
    {
        ArgumentNullException.ThrowIfNull(split);

        var builder = new FrPaymentSplitBuilder();
        split(builder);

        if (builder.Entries.Count == 0)
        {
            throw new ArgumentException("A payment is reported split by VAT rate, at least once.", parameter);
        }

        var payment = new FrReportedPayment { Date = paidOn };

        foreach ((decimal rate, decimal collected, string currency) in builder.Entries)
        {
            payment.Subtotals.Add(new FrPaymentSubtotal
            {
                TaxPercent = rate,
                CurrencyCode = currency,
                Amount = collected,
            });
        }

        return payment;
    }
}
