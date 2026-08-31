using International.EInvoicing.Profiles;

namespace International.EInvoicing.Countries.Serbia;

/// <summary>
/// The Serbian profiles.
/// </summary>
/// <remarks>
/// <b>SRBDT</b> is Serbia's CIUS of EN 16931, exchanged over the SEF — <em>Sistem e-Faktura</em> — where
/// electronic invoicing has been mandatory since 2023. <see cref="SrbdtExtensionUbl"/> is its conformant
/// extension, for the fields Serbia adds beyond the norm. Both identifiers are read from the published rule
/// set rather than transcribed.
/// </remarks>
public static class RsProfiles
{
    private static readonly ProfileIdentifier SrbdtId =
        new("urn:cen.eu:en16931:2017#compliant#urn:mfin.gov.rs:srbdt:2022");

    /// <summary>SRBDT, the Serbian CIUS, in UBL.</summary>
    public static Profile SrbdtUbl { get; } =
        new(SrbdtId, "SRBDT", DocumentSyntax.Ubl, KnownProfiles.En16931Ubl.Id);

    /// <summary>SRBDT with the Serbian extension, for invoices that go beyond the norm.</summary>
    public static Profile SrbdtExtensionUbl { get; } = new(
        new ProfileIdentifier(
            "urn:cen.eu:en16931:2017#compliant#urn:mfin.gov.rs:srbdt:2022"
            + "#conformant#urn:mfin.gov.rs:srbdtext:2022"),
        "SRBDT with extension",
        DocumentSyntax.Ubl,
        SrbdtId);

    /// <summary>Every profile Serbia uses.</summary>
    public static IReadOnlyList<Profile> All { get; } = [SrbdtUbl, SrbdtExtensionUbl];
}
