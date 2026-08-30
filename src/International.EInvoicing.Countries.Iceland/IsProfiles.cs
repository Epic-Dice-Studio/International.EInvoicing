using International.EInvoicing.Peppol;
using International.EInvoicing.Profiles;

namespace International.EInvoicing.Countries.Iceland;

/// <summary>
/// The Icelandic profiles.
/// </summary>
/// <remarks>
/// Iceland exchanges Peppol BIS Billing, with Icelandic rules that travel inside the Peppol rule set.
/// </remarks>
public static class IsProfiles
{
    /// <summary>Peppol BIS Billing 3.0 in UBL, the syntax Iceland exchanges in.</summary>
    public static Profile PeppolBillingUbl => PeppolProfiles.BillingUbl;

    /// <summary>Peppol BIS Billing 3.0 in CII.</summary>
    public static Profile PeppolBillingCii => PeppolProfiles.BillingCii;

    /// <summary>Every profile Iceland uses.</summary>
    public static IReadOnlyList<Profile> All { get; } = [PeppolBillingUbl, PeppolBillingCii];
}
