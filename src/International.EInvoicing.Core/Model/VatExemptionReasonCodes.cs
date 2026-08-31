using System.Collections.Frozen;

namespace International.EInvoicing.Model;

/// <summary>
/// The VAT exemption reason codes (BT-121) EN 16931 accepts, from the European Commission's VATEX list.
/// </summary>
/// <remarks>
/// One of the two ways to answer <c>BR-E-10</c> and its siblings — the other being the free text of BT-120.
/// The code is the better answer where one fits: a receiver can act on <c>VATEX-EU-AE</c>, and cannot act on
/// "reverse charge" written in Finnish. Note the French block at the end: the list carries national codes,
/// which is why it is read out of the artefact rather than out of the directive.
/// </remarks>
public static class VatExemptionReasonCodes
{
    private static readonly string[] Codes =
    [
        "VATEX-EU-79-C", "VATEX-EU-132", "VATEX-EU-132-1A", "VATEX-EU-132-1B", "VATEX-EU-132-1C", "VATEX-EU-132-1D", "VATEX-EU-132-1E", "VATEX-EU-132-1F", "VATEX-EU-132-1G", "VATEX-EU-132-1H", "VATEX-EU-132-1I", "VATEX-EU-132-1J",
        "VATEX-EU-132-1K", "VATEX-EU-132-1L", "VATEX-EU-132-1M", "VATEX-EU-132-1N", "VATEX-EU-132-1O", "VATEX-EU-132-1P", "VATEX-EU-132-1Q", "VATEX-EU-135-1", "VATEX-EU-143", "VATEX-EU-143-1A", "VATEX-EU-143-1B", "VATEX-EU-143-1C",
        "VATEX-EU-143-1D", "VATEX-EU-143-1E", "VATEX-EU-143-1F", "VATEX-EU-143-1FA", "VATEX-EU-143-1G", "VATEX-EU-143-1H", "VATEX-EU-143-1I", "VATEX-EU-143-1J", "VATEX-EU-143-1K", "VATEX-EU-143-1L", "VATEX-EU-144", "VATEX-EU-146-1E",
        "VATEX-EU-159", "VATEX-EU-309", "VATEX-EU-148", "VATEX-EU-148-A", "VATEX-EU-148-B", "VATEX-EU-148-C", "VATEX-EU-148-D", "VATEX-EU-148-E", "VATEX-EU-148-F", "VATEX-EU-148-G", "VATEX-EU-151", "VATEX-EU-151-1A",
        "VATEX-EU-151-1AA", "VATEX-EU-151-1B", "VATEX-EU-151-1C", "VATEX-EU-151-1D", "VATEX-EU-151-1E", "VATEX-EU-G", "VATEX-EU-O", "VATEX-EU-IC", "VATEX-EU-AE", "VATEX-EU-D", "VATEX-EU-F", "VATEX-EU-I",
        "VATEX-EU-J", "VATEX-FR-FRANCHISE", "VATEX-FR-CNWVAT", "VATEX-EU-153", "VATEX-FR-CGI261-1", "VATEX-FR-CGI261-2", "VATEX-FR-CGI261-3", "VATEX-FR-CGI261-4", "VATEX-FR-CGI261-5", "VATEX-FR-CGI261-7", "VATEX-FR-CGI261-8", "VATEX-FR-CGI261A",
        "VATEX-FR-CGI261B", "VATEX-FR-CGI261C-1", "VATEX-FR-CGI261C-2", "VATEX-FR-CGI261C-3", "VATEX-FR-CGI261D-1", "VATEX-FR-CGI261D-1BIS", "VATEX-FR-CGI261D-2", "VATEX-FR-CGI261D-3", "VATEX-FR-CGI261D-4", "VATEX-FR-CGI261E-1", "VATEX-FR-CGI261E-2", "VATEX-FR-CGI277A",
        "VATEX-FR-CGI275", "VATEX-FR-298SEXDECIESA", "VATEX-FR-CGI295", "VATEX-FR-AE",
    ];

    private static readonly FrozenSet<string> Known = Codes.ToFrozenSet(StringComparer.Ordinal);

    /// <summary>Every code, in the order the artefact lists them.</summary>
    public static IReadOnlyList<string> All => Codes;

    /// <summary>Whether a VAT exemption reason code is one EN 16931 accepts.</summary>
    public static bool IsKnown(string? code) => code is not null && Known.Contains(code);
}
