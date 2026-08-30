using International.EInvoicing.Profiles;

namespace International.EInvoicing.Countries.Belgium;

/// <summary>
/// The Belgian profiles.
/// </summary>
/// <remarks>
/// The 2026 mandate is built on Peppol BIS Billing 3.0 rather than on a Belgian format, so these are the
/// Peppol profiles: what Belgium adds is national rules on top, not a different document.
/// </remarks>
public static class BeProfiles
{
    /// <summary>Peppol BIS Billing 3.0 in UBL, the syntax Belgium exchanges in.</summary>
    public static Profile PeppolBillingUbl => KnownProfiles.PeppolBisBilling3Ubl;

    /// <summary>Peppol BIS Billing 3.0 in CII.</summary>
    public static Profile PeppolBillingCii => KnownProfiles.PeppolBisBilling3Cii;

    /// <summary>Every profile Belgium uses.</summary>
    public static IReadOnlyList<Profile> All { get; } = [PeppolBillingUbl, PeppolBillingCii];
}
