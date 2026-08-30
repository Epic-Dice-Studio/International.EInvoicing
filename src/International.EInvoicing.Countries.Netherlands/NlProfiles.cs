using International.EInvoicing.Peppol;
using International.EInvoicing.Profiles;

namespace International.EInvoicing.Countries.Netherlands;

/// <summary>
/// The Dutch profiles.
/// </summary>
/// <remarks>
/// These are the Peppol profiles. The Netherlands also publishes **NLCIUS**, carried by SI-UBL, and it is
/// deliberately absent: its published specification identifier is not in any artefact this repository holds,
/// and a guessed identifier is how a library starts rejecting valid documents. Register it yourself when you
/// have it — a registered profile wins over anything built in. The Dutch rules in the Peppol rule set apply
/// to Peppol BIS invoices from Dutch suppliers, which is the path this package covers.
/// </remarks>
public static class NlProfiles
{
    /// <summary>Peppol BIS Billing 3.0 in UBL, the syntax the Netherlands exchanges in.</summary>
    public static Profile PeppolBillingUbl => PeppolProfiles.BillingUbl;

    /// <summary>Peppol BIS Billing 3.0 in CII.</summary>
    public static Profile PeppolBillingCii => PeppolProfiles.BillingCii;

    /// <summary>Every profile this package registers.</summary>
    public static IReadOnlyList<Profile> All { get; } = [PeppolBillingUbl, PeppolBillingCii];
}
