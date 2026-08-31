using System.Collections.Frozen;

namespace International.EInvoicing.Countries.Singapore;

/// <summary>
/// The tax category codes a Singaporean invoice may carry.
/// </summary>
/// <remarks>
/// Singapore does not use EN 16931's category codes. <c>S</c> for standard-rated — the code every European
/// example uses and the one a caller reaches for first — is rejected outright by <c>BR-CL-17-GST-SG</c>, a
/// fatal rule. Singapore's own code is <c>SR</c>, and the rest of the list is its own too.
/// </remarks>
public static class SgTaxCategory
{
    /// <summary>Standard-rated supply. The code EN 16931 would write as <c>S</c>.</summary>
    public const string StandardRated = "SR";

    /// <summary>Zero-rated supply.</summary>
    public const string ZeroRated = "ZR";

    /// <summary>Exempt supply under regulation 33.</summary>
    public const string Exempt33 = "ES33";

    /// <summary>Deemed supply.</summary>
    public const string DeemedSupply = "DS";

    /// <summary>Out of scope.</summary>
    public const string OutOfScope = "OS";

    /// <summary>Not applicable.</summary>
    public const string NotApplicable = "NA";

    private static readonly string[] Codes =
    [
        "SR", "SRCA-S", "SRCA-C", "ZR", "ES33", "ESN33", "DS", "OS", "NA", "NG",
        "SRRC", "SROVR-RS", "SROVR-LVG", "SRLVG",
    ];

    private static readonly FrozenSet<string> Known = Codes.ToFrozenSet(StringComparer.Ordinal);

    /// <summary>Every code Singapore allows, in the order the rule lists them.</summary>
    public static IReadOnlyList<string> All => Codes;

    /// <summary>Whether a tax category code is one Singapore allows.</summary>
    public static bool IsAllowed(string? code) => code is not null && Known.Contains(code);
}
