using International.EInvoicing.Peppol;
using International.EInvoicing.Profiles;

namespace International.EInvoicing.Countries.Australia;

/// <summary>
/// The Australian profiles.
/// </summary>
/// <remarks>
/// Australia and New Zealand share one Peppol authority and one PINT specialisation, <c>@aunz-1</c>. That is
/// PINT rather than BIS Billing: Australia is outside the European family, which is the distinction
/// <see cref="PeppolPintProfiles"/> exists to make.
/// </remarks>
public static class AuProfiles
{
    /// <summary>Peppol PINT Billing, A-NZ specialisation — what Australia exchanges.</summary>
    public static Profile PintBilling => PeppolPintProfiles.BillingAuNz;

    /// <summary>Every profile Australia uses.</summary>
    public static IReadOnlyList<Profile> All { get; } = [PintBilling];
}
