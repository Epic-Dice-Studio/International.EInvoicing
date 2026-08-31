using System.Collections.Frozen;

namespace International.EInvoicing.Model;

/// <summary>
/// The coded reasons for a document- or line-level charge (BT-105, BT-145), from UNTDID 7161.
/// </summary>
/// <remarks>
/// <c>FC</c> is freight, <c>PC</c> packing, <c>ABK</c> miscellaneous — the one to reach for when nothing
/// fits, rather than leaving the code out and relying on the free text.
/// </remarks>
public static class ChargeReasonCodes
{
    private static readonly string[] Codes =
    [
        "AA", "AAA", "AAC", "AAD", "AAE", "AAF", "AAH", "AAI", "AAS", "AAT", "AAV", "AAY",
        "AAZ", "ABA", "ABB", "ABC", "ABD", "ABF", "ABK", "ABL", "ABN", "ABR", "ABS", "ABT",
        "ABU", "ACF", "ACG", "ACH", "ACI", "ACJ", "ACK", "ACL", "ACM", "ACS", "ADC", "ADE",
        "ADJ", "ADK", "ADL", "ADM", "ADN", "ADO", "ADP", "ADQ", "ADR", "ADT", "ADW", "ADY",
        "ADZ", "AEA", "AEB", "AEC", "AED", "AEF", "AEH", "AEI", "AEJ", "AEK", "AEL", "AEM",
        "AEN", "AEO", "AEP", "AES", "AET", "AEU", "AEV", "AEW", "AEX", "AEY", "AEZ", "AJ",
        "AU", "CA", "CAB", "CAD", "CAE", "CAF", "CAI", "CAJ", "CAK", "CAL", "CAM", "CAN",
        "CAO", "CAP", "CAQ", "CAR", "CAS", "CAT", "CAU", "CAV", "CAW", "CAX", "CAY", "CAZ",
        "CD", "CG", "CS", "CT", "DAB", "DAD", "DAC", "DAF", "DAG", "DAH", "DAI", "DAJ",
        "DAK", "DAL", "DAM", "DAN", "DAO", "DAP", "DAQ", "DL", "EG", "EP", "ER", "FAA",
        "FAB", "FAC", "FC", "FH", "FI", "GAA", "HAA", "HD", "HH", "IAA", "IAB", "ID",
        "IF", "IR", "IS", "KO", "L1", "LA", "LAA", "LAB", "LF", "MAE", "MI", "ML",
        "NAA", "OA", "PA", "PAA", "PC", "PL", "PRV", "RAB", "RAC", "RAD", "RAF", "RE",
        "RF", "RH", "RV", "SA", "SAA", "SAD", "SAE", "SAI", "SG", "SH", "SM", "SU",
        "TAB", "TAC", "TT", "TV", "V1", "V2", "WH", "XAA", "YY", "ZZZ",
    ];

    private static readonly FrozenSet<string> Known = Codes.ToFrozenSet(StringComparer.Ordinal);

    /// <summary>Every code, in the order the artefact lists them.</summary>
    public static IReadOnlyList<string> All => Codes;

    /// <summary>Whether a charge reason code is one EN 16931 accepts.</summary>
    public static bool IsKnown(string? code) => code is not null && Known.Contains(code);
}
