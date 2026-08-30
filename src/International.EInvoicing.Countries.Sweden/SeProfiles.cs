using International.EInvoicing.Peppol;
using International.EInvoicing.Profiles;

namespace International.EInvoicing.Countries.Sweden;

/// <summary>
/// The Swedish profiles.
/// </summary>
/// <remarks>
/// Sweden exchanges Peppol BIS Billing itself rather than a national CIUS: what it adds is a set of national
/// rules, which travel inside the Peppol rule set. So these are the Peppol profiles.
/// </remarks>
public static class SeProfiles
{
    /// <summary>Peppol BIS Billing 3.0 in UBL, the syntax Sweden exchanges in.</summary>
    public static Profile PeppolBillingUbl => PeppolProfiles.BillingUbl;

    /// <summary>Peppol BIS Billing 3.0 in CII.</summary>
    public static Profile PeppolBillingCii => PeppolProfiles.BillingCii;

    /// <summary>Every profile Sweden uses.</summary>
    public static IReadOnlyList<Profile> All { get; } = [PeppolBillingUbl, PeppolBillingCii];
}
