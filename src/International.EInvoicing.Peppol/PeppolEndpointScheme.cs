using System.Collections.Frozen;

namespace International.EInvoicing.Peppol;

/// <summary>
/// The Electronic Address Scheme codes, which say what kind of identifier an electronic address is.
/// </summary>
/// <remarks>
/// <para>
/// BT-34 and BT-49 carry an address plus the scheme it belongs to, and guessing the scheme from the country
/// is the shortcut that gets invoices rejected. The list here is the one EN 16931 checks against in
/// <c>BR-CL-25</c>, taken from the artefacts this library ships rather than transcribed.
/// </para>
/// <para>
/// It is versioned, like everything else in this space: a scheme a national authority has begun using may not
/// be in the list yet. <see cref="IsKnown"/> answers for the version shipped, and no more than that.
/// </para>
/// </remarks>
public static class PeppolEndpointScheme
{
    /// <summary>A French company, by its SIREN.</summary>
    public const string FrenchSiren = "0002";

    /// <summary>A Swedish organisation number.</summary>
    public const string SwedishOrganisation = "0007";

    /// <summary>A GS1 Global Location Number.</summary>
    public const string GlobalLocationNumber = "0088";

    /// <summary>A Danish organisation, by its CVR number.</summary>
    public const string DanishOrganisation = "0184";

    /// <summary>A Norwegian organisation number.</summary>
    public const string NorwegianOrganisation = "0192";

    /// <summary>A Danish SE number.</summary>
    public const string DanishSeNumber = "0198";

    /// <summary>An Italian public administration, by its IPA code.</summary>
    public const string ItalianPublicAdministration = "0201";

    /// <summary>A Belgian enterprise number.</summary>
    public const string BelgianEnterprise = "0208";

    /// <summary>An Italian <em>codice fiscale</em>.</summary>
    public const string ItalianFiscalCode = "0210";

    /// <summary>An Italian VAT number.</summary>
    public const string ItalianVat = "0211";

    /// <summary>A French routing address, which is how a platform is reached.</summary>
    public const string FrenchRoutingAddress = "0225";

    private static readonly string[] Codes =
    [
        "0002", "0007", "0009", "0037", "0060", "0088", "0096", "0097", "0106", "0130",
        "0135", "0142", "0147", "0151", "0154", "0158", "0170", "0177", "0183", "0184",
        "0188", "0190", "0191", "0192", "0193", "0194", "0195", "0196", "0198", "0199",
        "0200", "0201", "0202", "0203", "0204", "0205", "0208", "0209", "0210", "0211",
        "0212", "0213", "0215", "0216", "0217", "0218", "0219", "0220", "0221", "0225",
        "0230", "0235", "0240", "0244", "0242", "0245", "0246", "0248", "9910", "9913",
        "9914", "9915", "9918", "9919", "9920", "9922", "9923", "9924", "9925", "9926",
        "9927", "9928", "9929", "9930", "9931", "9932", "9933", "9934", "9935", "9936",
        "9937", "9938", "9939", "9940", "9941", "9942", "9943", "9944", "9945", "9946",
        "9947", "9948", "9949", "9950", "9951", "9952", "9953", "9957", "9959", "AN",
        "AQ", "AS", "AU", "EM",
    ];

    private static readonly FrozenSet<string> Known = Codes.ToFrozenSet(StringComparer.Ordinal);

    /// <summary>The version of the EN 16931 artefacts this list was taken from.</summary>
    public const string ArtefactVersion = "1.3.16";

    /// <summary>Every scheme code the shipped artefacts accept.</summary>
    public static IReadOnlyList<string> All => Codes;

    /// <summary>Whether a scheme code is one the shipped artefacts accept.</summary>
    public static bool IsKnown(string? code) => code is not null && Known.Contains(code);

    /// <summary>The code, or an exception naming what was wrong with it.</summary>
    /// <exception cref="ArgumentException">The code is empty, or not in the list.</exception>
    public static string Require(string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        return IsKnown(code)
            ? code
            : throw new ArgumentException(
                $"'{code}' is not an electronic address scheme in the EN 16931 code list version "
                + $"{ArtefactVersion}. An invoice using it fails BR-CL-25. Check the CEF EAS list for a "
                + "newer entry, or use PeppolEndpointScheme.All to see what this version accepts.",
                nameof(code));
    }
}
