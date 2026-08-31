using International.EInvoicing.Peppol;
using International.EInvoicing.Profiles;

namespace International.EInvoicing.Countries.NewZealand;

/// <summary>
/// The New Zealand profiles.
/// </summary>
/// <remarks>
/// New Zealand and Australia share one Peppol authority and one PINT specialisation, <c>@aunz-1</c>. What
/// differs between the two countries is the business identifier, not the document.
/// </remarks>
public static class NzProfiles
{
    /// <summary>Peppol PINT Billing, A-NZ specialisation — what New Zealand exchanges.</summary>
    public static Profile PintBilling => PeppolPintProfiles.BillingAuNz;

    /// <summary>Every profile New Zealand uses.</summary>
    public static IReadOnlyList<Profile> All { get; } = [PintBilling];
}
