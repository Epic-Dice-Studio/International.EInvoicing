using System.Collections.Frozen;

namespace International.EInvoicing.Countries.Denmark;

/// <summary>
/// The payment means codes a Danish invoice may carry.
/// </summary>
/// <remarks>
/// Denmark allows a subset of UNTDID 4461 and rejects the rest — <c>DK-R-005</c>, a fatal rule, when both
/// parties are Danish. Plain credit transfer, code 30, is <em>not</em> in it, which is the trap: it is the
/// obvious code, it is valid EN 16931, and a Danish recipient will refuse the invoice. Use 58 for a SEPA
/// credit transfer instead.
/// </remarks>
public static class DkPaymentMeans
{
    /// <summary>SEPA credit transfer — the ordinary way to be paid, and the one Denmark accepts.</summary>
    public const string SepaCreditTransfer = "58";

    /// <summary>Direct debit.</summary>
    public const string DirectDebit = "49";

    /// <summary>SEPA direct debit.</summary>
    public const string SepaDirectDebit = "59";

    /// <summary>Payment card.</summary>
    public const string PaymentCard = "48";

    /// <summary>Cash.</summary>
    public const string Cash = "10";

    private static readonly string[] Codes =
        ["1", "10", "31", "42", "48", "49", "50", "58", "59", "93", "97"];

    private static readonly FrozenSet<string> Known = Codes.ToFrozenSet(StringComparer.Ordinal);

    /// <summary>Every code Denmark allows, in the order the rule lists them.</summary>
    public static IReadOnlyList<string> All => Codes;

    /// <summary>Whether a payment means code is one Denmark allows.</summary>
    public static bool IsAllowed(string? code) => code is not null && Known.Contains(code);
}
