using International.EInvoicing.Model;
using International.EInvoicing.Values;

namespace International.EInvoicing.Countries.France.EReporting.Model;

/// <summary>The period a report covers. The end must come after the start.</summary>
public sealed class FrReportPeriod : InvoiceNode
{
    /// <summary>The first day covered.</summary>
    public DateField StartDate { get; set; }

    /// <summary>The last day covered.</summary>
    public DateField EndDate { get; set; }
}
