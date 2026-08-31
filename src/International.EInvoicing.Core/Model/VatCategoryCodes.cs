using System.Collections.Frozen;

namespace International.EInvoicing.Model;

/// <summary>
/// The VAT category codes (BT-118, BT-151) EN 16931 accepts.
/// </summary>
/// <remarks>
/// Ten of UNTDID 5305, and the choice between them decides which of the <c>BR-S</c>, <c>BR-Z</c>,
/// <c>BR-E</c>, <c>BR-AE</c>, <c>BR-K</c>, <c>BR-G</c>, <c>BR-O</c> and <c>BR-IC</c> rule families judges
/// the invoice — so a wrong one does not fail on its own, it fails somewhere else. The list is the one
/// <c>BR-CL-17</c> tests, taken from the artefact rather than transcribed.
/// </remarks>
public static class VatCategoryCodes
{
    /// <summary>Standard rate.</summary>
    public const string Standard = "S";

    /// <summary>Zero-rated goods.</summary>
    public const string ZeroRated = "Z";

    /// <summary>Exempt from VAT.</summary>
    public const string Exempt = "E";

    /// <summary>VAT reverse charge.</summary>
    public const string ReverseCharge = "AE";

    /// <summary>Intra-community supply — exempt, and reported to the acquirer's member state.</summary>
    public const string IntraCommunitySupply = "K";

    /// <summary>Free export item, VAT not charged.</summary>
    public const string Export = "G";

    /// <summary>Services outside the scope of VAT.</summary>
    public const string OutsideScope = "O";

    /// <summary>Canary Islands general indirect tax.</summary>
    public const string CanaryIslands = "L";

    /// <summary>Tax for production, services and imports in Ceuta and Melilla.</summary>
    public const string CeutaAndMelilla = "M";

    /// <summary>Transferred VAT, as Italy applies it.</summary>
    public const string TransferredVat = "B";

    private static readonly string[] Codes = ["AE", "L", "M", "E", "S", "Z", "G", "O", "K", "B"];

    private static readonly FrozenSet<string> Known = Codes.ToFrozenSet(StringComparer.Ordinal);

    /// <summary>Every code, in the order the rule lists them.</summary>
    public static IReadOnlyList<string> All => Codes;

    /// <summary>Whether a VAT category code is one EN 16931 accepts.</summary>
    public static bool IsKnown(string? code) => code is not null && Known.Contains(code);

    /// <summary>
    /// Whether a category means no VAT is charged, and therefore needs an exemption reason (BT-120 or BT-121).
    /// </summary>
    /// <remarks>
    /// <c>BR-E-10</c>, <c>BR-AE-10</c>, <c>BR-G-10</c>, <c>BR-K-10</c> and <c>BR-O-10</c> each say the same
    /// thing about their own category, which is easy to meet once and forget on the fifth.
    /// </remarks>
    public static bool NeedsExemptionReason(string? code) =>
        code is Exempt or ReverseCharge or Export or IntraCommunitySupply or OutsideScope;

    /// <summary>
    /// Whether a category forbids a VAT rate altogether, rather than requiring it to be zero.
    /// </summary>
    /// <remarks>
    /// The distinction is easy to miss and fatal when missed. Exempt, zero-rated, reverse-charge,
    /// intra-community and export invoices carry a rate of <b>0</b>; <em>not subject to VAT</em> carries
    /// <b>no rate at all</b>, and <c>BR-O-05</c>, <c>BR-O-06</c> and <c>BR-O-07</c> reject a zero written
    /// there. A helper that always writes a rate cannot produce a valid out-of-scope invoice.
    /// </remarks>
    public static bool ForbidsRate(string? code) => code is OutsideScope;
}
