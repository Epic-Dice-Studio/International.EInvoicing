using System.Collections.Frozen;

namespace International.EInvoicing.Model;

/// <summary>
/// The item classification scheme identifiers (BT-158-1) EN 16931 accepts, from UNTDID 7143.
/// </summary>
/// <remarks>
/// The one that says <em>what kind of thing</em> a line is, in somebody else's taxonomy: <c>ST</c> for
/// CPV, <c>MP</c> for UNSPSC, <c>SRV</c> for GS1 GPC, <c>TSP</c> for the EU's own. A buyer's procurement
/// system usually cares more about this than about the item name.
/// </remarks>
public static class ItemClassificationSchemeCodes
{
    private static readonly string[] Codes =
    [
        "AA", "AB", "AC", "AD", "AE", "AF", "AG", "AH", "AI", "AJ", "AK", "AL",
        "AM", "AN", "AO", "AP", "AQ", "AR", "AS", "AT", "AU", "AV", "AW", "AX",
        "AY", "AZ", "BA", "BB", "BC", "BD", "BE", "BF", "BG", "BH", "BI", "BJ",
        "BK", "BL", "BM", "BN", "BO", "BP", "BQ", "BR", "BS", "BT", "BU", "BV",
        "BW", "BX", "BY", "BZ", "CC", "CG", "CL", "CR", "CV", "DR", "DW", "EC",
        "EF", "EMD", "EN", "FS", "GB", "GN", "GMN", "GS", "HS", "IB", "IN", "IS",
        "IT", "IZ", "MA", "MF", "MN", "MP", "NB", "ON", "PD", "PL", "PO", "PPI",
        "PV", "QS", "RC", "RN", "RU", "RY", "SA", "SG", "SK", "SN", "SRS", "SRT",
        "SRU", "SRV", "SRW", "SRX", "SRY", "SRZ", "SS", "SSA", "SSB", "SSC", "SSD", "SSE",
        "SSF", "SSG", "SSH", "SSI", "SSJ", "SSK", "SSL", "SSM", "SSN", "SSO", "SSP", "SSQ",
        "SSR", "SSS", "SST", "SSU", "SSV", "SSW", "SSX", "SSY", "SSZ", "ST", "STA", "STB",
        "STC", "STD", "STE", "STF", "STG", "STH", "STI", "STJ", "STK", "STL", "STM", "STN",
        "STO", "STP", "STQ", "STR", "STS", "STT", "STU", "STV", "STW", "STX", "STY", "STZ",
        "SUA", "SUB", "SUC", "SUD", "SUE", "SUF", "SUG", "SUH", "SUI", "SUJ", "SUK", "SUL",
        "SUM", "TG", "TSN", "TSO", "TSP", "TSQ", "TSR", "TSS", "TST", "TSU", "UA", "UP",
        "VN", "VP", "VS", "VX", "ZZZ",
    ];

    private static readonly FrozenSet<string> Known = Codes.ToFrozenSet(StringComparer.Ordinal);

    /// <summary>Every code, in the order the artefact lists them.</summary>
    public static IReadOnlyList<string> All => Codes;

    /// <summary>Whether a item classification scheme identifier is one EN 16931 accepts.</summary>
    public static bool IsKnown(string? code) => code is not null && Known.Contains(code);
}
