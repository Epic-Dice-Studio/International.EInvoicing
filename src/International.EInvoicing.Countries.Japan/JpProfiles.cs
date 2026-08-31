using International.EInvoicing.Peppol;
using International.EInvoicing.Profiles;

namespace International.EInvoicing.Countries.Japan;

/// <summary>
/// The Japanese profiles.
/// </summary>
/// <remarks>
/// Japan is on Peppol PINT — <c>urn:peppol:pint:billing-1@jp-1</c>. Its rules also still accept the older
/// <c>urn:fdc:peppol:jp:billing:3.0</c> and either family's business process, which is unusual: most
/// jurisdictions accept one. This package writes the PINT pair, which is the one being migrated to.
/// </remarks>
public static class JpProfiles
{
    /// <summary>Peppol PINT Billing, Japanese specialisation.</summary>
    public static Profile PintBilling => PeppolPintProfiles.BillingJp;

    /// <summary>Every profile Japan uses.</summary>
    public static IReadOnlyList<Profile> All { get; } = [PintBilling];
}
