using International.EInvoicing.Values;

namespace International.EInvoicing.Model;

/// <summary>One line of the invoice (BG-25).</summary>
public sealed class InvoiceLine : InvoiceNode
{
    /// <summary>BT-126 — the line's identifier, unique within the invoice.</summary>
    public IdentifierField Identifier { get; set; }

    /// <summary>BT-127 — a free-text note about the line.</summary>
    public TextField Note { get; set; }

    /// <summary>
    /// The line this one belongs under, when the invoice groups its lines.
    /// </summary>
    /// <remarks>
    /// EN 16931 has no term for it and Factur-X EXTENDED does: the hierarchy is expressed by reference
    /// rather than by nesting, so the lines stay a flat list and each child names its parent's
    /// <see cref="Identifier"/>. A reader that ignores this reads a grouped invoice as a flat one and adds
    /// the group headers up with the details, which double-counts.
    /// </remarks>
    public IdentifierField ParentLineIdentifier { get; set; }

    /// <summary>
    /// What this line is for: <c>GROUP</c>, <c>DETAIL</c> or <c>INFORMATION</c>.
    /// </summary>
    /// <remarks>
    /// A <c>GROUP</c> line is a heading whose amount is the sum of its children, and an
    /// <c>INFORMATION</c> line carries no amount at all — so this is what tells a caller which lines to
    /// total and which to display.
    /// </remarks>
    public CodeField LineStatusReasonCode { get; set; }

    /// <summary>The line's status, from UNTDID 1229, when the sender gives one.</summary>
    public CodeField LineStatusCode { get; set; }

    /// <summary>BT-128 — an identifier for the object this line refers to, with its scheme.</summary>
    public IdentifierField ObjectIdentifier { get; set; }

    /// <summary>BT-129 and BT-130 — the quantity invoiced and the unit it is measured in.</summary>
    public QuantityField Quantity { get; set; }

    /// <summary>BT-131 — the line's net amount, after line allowances and charges, excluding VAT.</summary>
    public AmountField NetAmount { get; set; }

    /// <summary>BT-132 — the line's reference in the purchase order.</summary>
    public IdentifierField OrderLineReference { get; set; }

    /// <summary>BT-133 — the buyer's accounting reference for this line.</summary>
    public TextField BuyerAccountingReference { get; set; }

    /// <summary>BT-151 — VAT category code for the line (UNTDID 5305).</summary>
    public CodeField VatCategoryCode { get; set; }

    /// <summary>BT-152 — VAT rate for the line, as a percentage.</summary>
    public Field<decimal> VatRate { get; set; }

    /// <summary>BG-26 — the period this line covers.</summary>
    public InvoicingPeriod? Period { get; set; }

    /// <summary>BG-27 and BG-28 — allowances and charges applying to this line.</summary>
    public List<AllowanceCharge> AllowancesAndCharges { get; } = [];

    /// <summary>BG-29 — the price the line is charged at.</summary>
    public LinePrice? Price { get; set; }

    /// <summary>BG-31 — what is being invoiced.</summary>
    public Item? Item { get; set; }
}
