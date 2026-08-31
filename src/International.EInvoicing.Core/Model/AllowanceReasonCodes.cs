using System.Collections.Frozen;

namespace International.EInvoicing.Model;

/// <summary>
/// The coded reasons for a document- or line-level allowance (BT-98, BT-140), from UNTDID 5189.
/// </summary>
/// <remarks>
/// Nineteen codes, against the hundred and seventy-eight for charges: the norm is more interested in why you
/// added money than in why you took it off. <c>95</c> — discount — is the one most invoices want.
/// </remarks>
public static class AllowanceReasonCodes
{
    private static readonly string[] Codes =
    [
        "41", "42", "60", "62", "63", "64", "65", "66", "67", "68", "70", "71",
        "88", "95", "100", "102", "103", "104", "105",
    ];

    private static readonly FrozenSet<string> Known = Codes.ToFrozenSet(StringComparer.Ordinal);

    /// <summary>Every code, in the order the artefact lists them.</summary>
    public static IReadOnlyList<string> All => Codes;

    /// <summary>Whether a allowance reason code is one EN 16931 accepts.</summary>
    public static bool IsKnown(string? code) => code is not null && Known.Contains(code);
}
