using International.EInvoicing.Model;
using International.EInvoicing.Values;

namespace International.EInvoicing.Countries.France.EReporting.Model;

/// <summary>When and where goods or services were delivered.</summary>
public sealed class FrReportedDelivery : InvoiceNode
{
    /// <summary>The effective delivery date.</summary>
    public DateField Date { get; set; }

    /// <summary>What the place is called, on a line-level delivery.</summary>
    public TextField Name { get; set; }

    /// <summary>Where it was delivered.</summary>
    public FrPostalLocation? Location { get; set; }
}
