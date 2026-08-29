using International.EInvoicing.Values;

namespace International.EInvoicing.Model;

/// <summary>The period an invoice or an invoice line covers (BG-14, BG-26).</summary>
public sealed class InvoicingPeriod : InvoiceNode
{
    /// <summary>BT-73 / BT-134 — start of the period.</summary>
    public DateField StartDate { get; set; }

    /// <summary>BT-74 / BT-135 — end of the period.</summary>
    public DateField EndDate { get; set; }
}
