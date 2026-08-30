using International.EInvoicing.Model;
using International.EInvoicing.Values;

namespace International.EInvoicing.Countries.France.EReporting.Model;

/// <summary>
/// When the money arrived — flux 10.2 and 10.4.
/// </summary>
/// <remarks>
/// For services, VAT is due when payment is collected rather than when the invoice is issued, which is why
/// payment has to be reported separately from the sale.
/// </remarks>
public sealed class FrPaymentsReport : InvoiceNode
{
    /// <summary>The period covered.</summary>
    public FrReportPeriod Period { get; set; } = new();

    /// <summary>Payments reported against an invoice — flux 10.2.</summary>
    public List<FrReportedInvoicePayment> Invoices { get; } = [];

    /// <summary>Payments reported as totals, with no invoice behind them — flux 10.4.</summary>
    public List<FrReportedPayment> Transactions { get; } = [];
}

/// <summary>What was collected against one invoice.</summary>
public sealed class FrReportedInvoicePayment : InvoiceNode
{
    /// <summary>The invoice number the payment settles.</summary>
    public IdentifierField InvoiceIdentifier { get; set; }

    /// <summary>When that invoice was issued.</summary>
    public DateField InvoiceIssueDate { get; set; }

    /// <summary>The payment itself.</summary>
    public FrReportedPayment Payment { get; set; } = new();
}

/// <summary>A payment: when it arrived, and how it splits across VAT rates.</summary>
public sealed class FrReportedPayment : InvoiceNode
{
    /// <summary>When the payment was collected.</summary>
    public DateField Date { get; set; }

    /// <summary>What was collected, once per VAT rate. At least one is required.</summary>
    public List<FrPaymentSubtotal> Subtotals { get; } = [];
}

/// <summary>What was collected at one VAT rate.</summary>
public sealed class FrPaymentSubtotal : InvoiceNode
{
    /// <summary>The rate, as a percentage.</summary>
    public Field<decimal> TaxPercent { get; set; }

    /// <summary>The currency. Only euro is accepted.</summary>
    public CodeField CurrencyCode { get; set; }

    /// <summary>The amount collected.</summary>
    public AmountField Amount { get; set; }
}
