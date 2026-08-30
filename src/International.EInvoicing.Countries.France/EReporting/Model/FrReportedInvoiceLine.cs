using International.EInvoicing.Model;
using International.EInvoicing.Values;

namespace International.EInvoicing.Countries.France.EReporting.Model;

/// <summary>A line of a reported invoice.</summary>
public sealed class FrReportedInvoiceLine : InvoiceNode
{
    /// <summary>Notes carried on the line.</summary>
    public List<FrReportedNote> Notes { get; } = [];

    /// <summary>How much was billed, with its unit.</summary>
    public QuantityField BilledQuantity { get; set; }

    /// <summary>The earlier invoice this line corrects.</summary>
    public FrReportedDocumentReference? ReferencedDocument { get; set; }

    /// <summary>Where this line was delivered.</summary>
    public FrReportedDelivery? Delivery { get; set; }

    /// <summary>The period this line covers.</summary>
    public FrReportPeriod? InvoicePeriod { get; set; }

    /// <summary>Discounts and charges on the line.</summary>
    public List<FrReportedAllowanceCharge> AllowancesAndCharges { get; } = [];

    /// <summary>What the item cost.</summary>
    public FrReportedPrice? Price { get; set; }

    /// <summary>What the item is called.</summary>
    public TextField ProductName { get; set; }
}
