using International.EInvoicing.Peppol;
using International.EInvoicing.Profiles;

namespace International.EInvoicing.Countries.Greece;

/// <summary>
/// The Greek profiles.
/// </summary>
/// <remarks>
/// Greece exchanges Peppol BIS Billing, with Greek rules that travel inside the Peppol rule set. Its
/// national reporting platform, <b>myDATA</b>, is a separate obligation: every invoice is reported to it,
/// and that is a transmission rather than a document — see <c>docs/roadmap.md</c>.
/// </remarks>
public static class GrProfiles
{
    /// <summary>Peppol BIS Billing 3.0 in UBL, the syntax Greece exchanges in.</summary>
    public static Profile PeppolBillingUbl => PeppolProfiles.BillingUbl;

    /// <summary>Peppol BIS Billing 3.0 in CII.</summary>
    public static Profile PeppolBillingCii => PeppolProfiles.BillingCii;

    /// <summary>Every profile Greece uses.</summary>
    public static IReadOnlyList<Profile> All { get; } = [PeppolBillingUbl, PeppolBillingCii];
}
