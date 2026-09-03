using International.EInvoicing.Values;

namespace International.EInvoicing.Model;

/// <summary>One thing a buyer asked for, and on what terms.</summary>
public sealed class OrderLine : InvoiceNode
{
    /// <summary>The line's identifier within the order.</summary>
    public IdentifierField Identifier { get; set; }

    /// <summary>A free-text note about this line.</summary>
    public TextField Note { get; set; }

    /// <summary>How much is wanted.</summary>
    public QuantityField Quantity { get; set; }

    /// <summary>What the line is expected to come to, before tax.</summary>
    public AmountField NetAmount { get; set; }

    /// <summary>The buyer's accounting cost centre for this line.</summary>
    public TextField AccountingReference { get; set; }

    /// <summary>
    /// Whether the buyer will accept part of this line rather than all of it.
    /// </summary>
    /// <remarks>
    /// It is the difference between a short delivery being acceptable and being a failure, which is why the
    /// despatch advice has to say how much it left outstanding.
    /// </remarks>
    public IndicatorField PartialDeliveryAccepted { get; set; }

    /// <summary>What it costs.</summary>
    public LinePrice? Price { get; set; }

    /// <summary>Allowances and charges applying to this line.</summary>
    public List<AllowanceCharge> AllowancesAndCharges { get; } = [];

    /// <summary>What was ordered.</summary>
    public OrderItem? Item { get; set; }

    /// <summary>Where and when this line in particular is wanted, when it differs from the order.</summary>
    public OrderDelivery? Delivery { get; set; }

    /// <summary>Who asked for this line, when a third party did.</summary>
    public Party? Originator { get; set; }
}
