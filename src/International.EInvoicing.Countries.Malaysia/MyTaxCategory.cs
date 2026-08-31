using System.Collections.Frozen;

namespace International.EInvoicing.Countries.Malaysia;

/// <summary>
/// The tax category codes a Malaysian invoice may carry.
/// </summary>
/// <remarks>
/// Malaysia does not use EN 16931's category codes either. <c>S</c> for standard-rated is not among them:
/// the standard-rated code is <c>SA</c>, and the list has entries — high-value and low-value goods — that
/// have no European equivalent at all.
/// </remarks>
public static class MyTaxCategory
{
    /// <summary>Sales tax. The code EN 16931 would write as <c>S</c>.</summary>
    public const string SalesTax = "SA";

    /// <summary>Service tax.</summary>
    public const string ServiceTax = "SE";

    /// <summary>High-value goods.</summary>
    public const string HighValueGoods = "HVG";

    /// <summary>Low-value goods.</summary>
    public const string LowValueGoods = "LVG";

    /// <summary>Exempt.</summary>
    public const string Exempt = "E";

    /// <summary>Out of scope.</summary>
    public const string OutOfScope = "O";

    /// <summary>Tourism tax.</summary>
    public const string TourismTax = "TTX";

    private static readonly string[] Codes = ["SA", "SE", "HVG", "LVG", "E", "O", "TTX"];

    private static readonly FrozenSet<string> Known = Codes.ToFrozenSet(StringComparer.Ordinal);

    /// <summary>Every code Malaysia allows, in the order the rule lists them.</summary>
    public static IReadOnlyList<string> All => Codes;

    /// <summary>Whether a tax category code is one Malaysia allows.</summary>
    public static bool IsAllowed(string? code) => code is not null && Known.Contains(code);
}
