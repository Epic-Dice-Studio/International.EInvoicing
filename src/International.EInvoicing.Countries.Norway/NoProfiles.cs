using International.EInvoicing.Peppol;
using International.EInvoicing.Profiles;

namespace International.EInvoicing.Countries.Norway;

/// <summary>
/// The Norwegian profiles.
/// </summary>
/// <remarks>
/// EHF 3.0 is a CIUS of Peppol BIS Billing, which is itself a CIUS of EN 16931 — so its identifier carries
/// all three, in that order. Documents declaring plain Peppol BIS are exchanged in Norway too, which is why
/// both are here.
/// </remarks>
public static class NoProfiles
{
    private static readonly ProfileIdentifier Ehf3Id = new(
        "urn:cen.eu:en16931:2017#compliant#urn:fdc:peppol.eu:2017:poacc:billing:3.0"
        + "#compliant#urn:www.difi.no:ehf:ver3.0");

    /// <summary>EHF 3.0 — <em>Elektronisk handelsformat</em> — in UBL.</summary>
    public static Profile Ehf3Ubl { get; } =
        new(Ehf3Id, "EHF 3.0", DocumentSyntax.Ubl, PeppolProfiles.BillingUbl.Id);

    /// <summary>EHF 3.0 in CII.</summary>
    public static Profile Ehf3Cii { get; } =
        new(Ehf3Id, "EHF 3.0", DocumentSyntax.Cii, PeppolProfiles.BillingCii.Id);

    /// <summary>Peppol BIS Billing 3.0 in UBL, exchanged in Norway alongside EHF.</summary>
    public static Profile PeppolBillingUbl => PeppolProfiles.BillingUbl;

    /// <summary>Peppol BIS Billing 3.0 in CII.</summary>
    public static Profile PeppolBillingCii => PeppolProfiles.BillingCii;

    /// <summary>Every profile Norway uses.</summary>
    public static IReadOnlyList<Profile> All { get; } =
        [Ehf3Ubl, Ehf3Cii, PeppolBillingUbl, PeppolBillingCii];
}
