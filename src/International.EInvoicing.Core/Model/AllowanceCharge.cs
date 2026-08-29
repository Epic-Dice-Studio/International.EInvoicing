using International.EInvoicing.Values;

namespace International.EInvoicing.Model;

/// <summary>
/// A deduction or an addition, at document level (BG-20, BG-21) or on a line (BG-27, BG-28).
/// </summary>
/// <remarks>
/// The semantic model describes allowances and charges as separate groups with identical shapes. Here they
/// share a type and are told apart by <see cref="IsCharge"/>, which is also how both syntaxes encode them.
/// </remarks>
public sealed class AllowanceCharge : InvoiceNode
{
    /// <summary>Whether this adds to the amount payable (a charge) or subtracts from it (an allowance).</summary>
    public bool IsCharge { get; set; }

    /// <summary>BT-92 / BT-99 / BT-136 / BT-141 — the amount.</summary>
    public AmountField Amount { get; set; }

    /// <summary>BT-93 / BT-100 / BT-137 / BT-142 — the amount the percentage applies to.</summary>
    public AmountField BaseAmount { get; set; }

    /// <summary>BT-94 / BT-101 / BT-138 / BT-143 — the percentage applied to the base amount.</summary>
    public Field<decimal> Percentage { get; set; }

    /// <summary>BT-95 / BT-102 — VAT category code. Document level only.</summary>
    public CodeField VatCategoryCode { get; set; }

    /// <summary>BT-96 / BT-103 — VAT rate. Document level only.</summary>
    public Field<decimal> VatRate { get; set; }

    /// <summary>BT-97 / BT-104 / BT-139 / BT-144 — why it applies, in words.</summary>
    public TextField Reason { get; set; }

    /// <summary>BT-98 / BT-105 / BT-140 / BT-145 — why it applies, as a code.</summary>
    public CodeField ReasonCode { get; set; }
}
