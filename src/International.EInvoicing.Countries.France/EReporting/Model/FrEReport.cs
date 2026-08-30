using International.EInvoicing.Diagnostics;
using International.EInvoicing.Model;

namespace International.EInvoicing.Countries.France.EReporting.Model;

/// <summary>
/// A French e-reporting transmission — <em>flux 10</em>.
/// </summary>
/// <remarks>
/// <para>
/// E-reporting is not invoicing. It reports to the tax administration what invoicing does not carry: sales to
/// consumers, transactions with parties abroad, and when the money actually arrived. It has its own document,
/// which is neither UBL nor CII and carries no XML namespace at all.
/// </para>
/// <para>
/// A transmission reports transactions <em>or</em> payments, never both: the header names one of the two, and
/// the rules reject a document carrying either none or each.
/// </para>
/// </remarks>
public sealed class FrEReport : InvoiceNode
{
    /// <summary>Who is transmitting, when, and whether this replaces an earlier transmission.</summary>
    public FrEReportDocument Document { get; set; } = new();

    /// <summary>Transactions — flux 10.1 and 10.3. <c>null</c> when the transmission reports payments.</summary>
    public FrTransactionsReport? Transactions { get; set; }

    /// <summary>Payments — flux 10.2 and 10.4. <c>null</c> when the transmission reports transactions.</summary>
    public FrPaymentsReport? Payments { get; set; }

    /// <summary>What was reported while this transmission was read. Empty for one built in code.</summary>
    public IReadOnlyList<Diagnostic> Diagnostics { get; set; } = [];
}
