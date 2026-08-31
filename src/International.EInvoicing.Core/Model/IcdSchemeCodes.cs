using System.Collections.Frozen;

namespace International.EInvoicing.Model;

/// <summary>
/// The ISO 6523 ICD scheme identifiers EN 16931 accepts, for identifiers that name their own scheme.
/// </summary>
/// <remarks>
/// <para>
/// Used by the seller and buyer identifiers (BT-29, BT-46), the legal registration identifiers (BT-30,
/// BT-47), the item standard identifier (BT-157) and the delivery location (BT-71) — <c>BR-CL-10</c>,
/// <c>BR-CL-11</c>, <c>BR-CL-21</c> and <c>BR-CL-26</c> respectively, all against the same list.
/// </para>
/// <para>
/// Not the same list as the Peppol electronic-address schemes (EAS, BT-34 and BT-49), which look alike and
/// are not: <c>0088</c> is a GLN in both, but the two lists have diverged. See the Peppol package for that one.
/// </para>
/// </remarks>
public static class IcdSchemeCodes
{
    private static readonly string[] Codes =
    [
        "0002", "0003", "0004", "0005", "0006", "0007", "0008", "0009", "0010", "0011", "0012", "0013",
        "0014", "0015", "0016", "0017", "0018", "0019", "0020", "0021", "0022", "0023", "0024", "0025",
        "0026", "0027", "0028", "0029", "0030", "0031", "0032", "0033", "0034", "0035", "0036", "0037",
        "0038", "0039", "0040", "0041", "0042", "0043", "0044", "0045", "0046", "0047", "0048", "0049",
        "0050", "0051", "0052", "0053", "0054", "0055", "0056", "0057", "0058", "0059", "0060", "0061",
        "0062", "0063", "0064", "0065", "0066", "0067", "0068", "0069", "0070", "0071", "0072", "0073",
        "0074", "0075", "0076", "0077", "0078", "0079", "0080", "0081", "0082", "0083", "0084", "0085",
        "0086", "0087", "0088", "0089", "0090", "0091", "0093", "0094", "0095", "0096", "0097", "0098",
        "0099", "0100", "0101", "0102", "0104", "0105", "0106", "0107", "0108", "0109", "0110", "0111",
        "0112", "0113", "0114", "0115", "0116", "0117", "0118", "0119", "0120", "0121", "0122", "0123",
        "0124", "0125", "0126", "0127", "0128", "0129", "0130", "0131", "0132", "0133", "0134", "0135",
        "0136", "0137", "0138", "0139", "0140", "0141", "0142", "0143", "0144", "0145", "0146", "0147",
        "0148", "0149", "0150", "0151", "0152", "0153", "0154", "0155", "0156", "0157", "0158", "0159",
        "0160", "0161", "0162", "0163", "0164", "0165", "0166", "0167", "0168", "0169", "0170", "0171",
        "0172", "0173", "0174", "0175", "0176", "0177", "0178", "0179", "0180", "0183", "0184", "0185",
        "0186", "0187", "0188", "0189", "0190", "0191", "0192", "0193", "0194", "0195", "0196", "0197",
        "0198", "0199", "0200", "0201", "0202", "0203", "0204", "0205", "0206", "0207", "0208", "0209",
        "0210", "0211", "0212", "0213", "0214", "0215", "0216", "0217", "0218", "0219", "0220", "0221",
        "0222", "0223", "0224", "0225", "0226", "0227", "0228", "0229", "0230", "0231", "0232", "0233",
        "0234", "0235", "0236", "0237", "0238", "0239", "0240", "0241", "0242", "0243", "0244", "0245",
        "0246", "0247", "0248",
    ];

    private static readonly FrozenSet<string> Known = Codes.ToFrozenSet(StringComparer.Ordinal);

    /// <summary>Every code, in the order the artefact lists them.</summary>
    public static IReadOnlyList<string> All => Codes;

    /// <summary>Whether a ICD scheme identifier is one EN 16931 accepts.</summary>
    public static bool IsKnown(string? code) => code is not null && Known.Contains(code);
}
