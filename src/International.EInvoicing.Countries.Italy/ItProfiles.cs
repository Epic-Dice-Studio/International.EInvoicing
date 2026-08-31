using International.EInvoicing.Peppol;
using International.EInvoicing.Profiles;

namespace International.EInvoicing.Countries.Italy;

/// <summary>
/// The Italian profiles this package carries.
/// </summary>
/// <remarks>
/// <para>
/// These are the Peppol profiles. Italy receives Peppol BIS across borders and for public procurement, and
/// the Italian rules inside the Peppol rule set apply to those documents.
/// </para>
/// <para>
/// <b>FatturaPA is not here.</b> The format the SDI exchanges domestically is its own XML tree rather than a
/// profile of EN 16931, and every invoice must carry a qualified electronic signature — which this library
/// does not produce, by design. It is a project rather than a profile; see <c>docs/roadmap.md</c>.
/// </para>
/// </remarks>
public static class ItProfiles
{
    /// <summary>Peppol BIS Billing 3.0 in UBL.</summary>
    public static Profile PeppolBillingUbl => PeppolProfiles.BillingUbl;

    /// <summary>Peppol BIS Billing 3.0 in CII.</summary>
    public static Profile PeppolBillingCii => PeppolProfiles.BillingCii;

    /// <summary>Every profile this package registers.</summary>
    public static IReadOnlyList<Profile> All { get; } = [PeppolBillingUbl, PeppolBillingCii];
}
