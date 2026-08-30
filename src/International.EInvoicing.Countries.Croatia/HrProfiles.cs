using International.EInvoicing.Peppol;
using International.EInvoicing.Profiles;

namespace International.EInvoicing.Countries.Croatia;

/// <summary>
/// The Croatian profiles.
/// </summary>
/// <remarks>
/// <para>
/// Croatia's <em>Fiskalizacija 2.0</em> mandate, live for domestic B2B since 1 January 2026, exchanges
/// UBL 2.1 over a five-corner Peppol-style network, so these are the Peppol profiles.
/// </para>
/// <para>
/// What is <b>not</b> here is <b>HR-FISK 2.0</b>, Croatia's own CIUS. Its published specification identifier
/// is not in any artefact this repository carries, and a guessed identifier in BT-24 makes every document
/// written with it wrong. Register it from your own code and it wins — see
/// <c>docs/recipes/add-a-profile.md</c>. See also <c>docs/standards/country-hr.md</c> for what else the
/// mandate requires that a document library cannot supply.
/// </para>
/// </remarks>
public static class HrProfiles
{
    /// <summary>Peppol BIS Billing 3.0 in UBL, the syntax Croatia exchanges in.</summary>
    public static Profile PeppolBillingUbl => PeppolProfiles.BillingUbl;

    /// <summary>Peppol BIS Billing 3.0 in CII.</summary>
    public static Profile PeppolBillingCii => PeppolProfiles.BillingCii;

    /// <summary>Every profile this package registers.</summary>
    public static IReadOnlyList<Profile> All { get; } = [PeppolBillingUbl, PeppolBillingCii];
}
