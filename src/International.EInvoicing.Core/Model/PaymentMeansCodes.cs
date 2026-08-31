using System.Collections.Frozen;

namespace International.EInvoicing.Model;

/// <summary>
/// The payment means codes (BT-81) EN 16931 accepts.
/// </summary>
/// <remarks>
/// UNTDID 4461, which EN 16931 takes whole — every code from 1 to 97 that the list defines. Countries then
/// narrow it, and narrow it differently: Denmark's <c>DK-R-005</c> rejects the most obvious code of all.
/// The list is the one <c>BR-CL-16</c> tests, taken from the artefact rather than transcribed.
/// </remarks>
public static class PaymentMeansCodes
{
    /// <summary>Credit transfer. Valid everywhere EN 16931 applies, and refused in Denmark.</summary>
    public const string CreditTransfer = "30";

    /// <summary>SEPA credit transfer.</summary>
    public const string SepaCreditTransfer = "58";

    /// <summary>SEPA direct debit.</summary>
    public const string SepaDirectDebit = "59";

    /// <summary>Direct debit.</summary>
    public const string DirectDebit = "49";

    /// <summary>Payment card.</summary>
    public const string PaymentCard = "48";

    /// <summary>Cash.</summary>
    public const string Cash = "10";

    /// <summary>Not defined — the code for "the parties know".</summary>
    public const string NotDefined = "1";

    /// <summary>Mutually defined, the list's own escape hatch.</summary>
    public const string MutuallyDefined = "ZZZ";

    private static readonly string[] Codes =
    [
        "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12",
        "13", "14", "15", "16", "17", "18", "19", "20", "21", "22", "23", "24",
        "25", "26", "27", "28", "29", "30", "31", "32", "33", "34", "35", "36",
        "37", "38", "39", "40", "41", "42", "43", "44", "45", "46", "47", "48",
        "49", "50", "51", "52", "53", "54", "55", "56", "57", "58", "59", "60",
        "61", "62", "63", "64", "65", "66", "67", "68", "69", "70", "74", "75",
        "76", "77", "78", "91", "92", "93", "94", "95", "96", "97", "98", "ZZZ",
    ];

    private static readonly FrozenSet<string> Known = Codes.ToFrozenSet(StringComparer.Ordinal);

    /// <summary>Every code, in the order the rule lists them.</summary>
    public static IReadOnlyList<string> All => Codes;

    /// <summary>Whether a payment means code is one EN 16931 accepts.</summary>
    public static bool IsKnown(string? code) => code is not null && Known.Contains(code);
}
