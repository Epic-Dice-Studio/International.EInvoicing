using International.EInvoicing.Peppol;
using International.EInvoicing.Profiles;

namespace International.EInvoicing.Countries.Denmark;

/// <summary>
/// The Danish profiles.
/// </summary>
/// <remarks>
/// <para>
/// Denmark exchanges Peppol BIS Billing over NemHandel, with Danish rules that travel inside the Peppol rule
/// set. So these are the Peppol profiles.
/// </para>
/// <para>
/// Two things this does not carry. **OIOUBL 2.1**, the national format still in use domestically, is its own
/// syntax rather than a profile of EN 16931, and would be its own reader and writer. And **NemHandel BIS 4**,
/// which the Danish Business Authority committed to in March 2026 as the single domestic format by 2029, is
/// built on Peppol BIS 4 and EN 16931-1:2026 — neither of which is published yet. See
/// <c>docs/roadmap.md</c>.
/// </para>
/// </remarks>
public static class DkProfiles
{
    /// <summary>Peppol BIS Billing 3.0 in UBL, the syntax Denmark exchanges in.</summary>
    public static Profile PeppolBillingUbl => PeppolProfiles.BillingUbl;

    /// <summary>Peppol BIS Billing 3.0 in CII.</summary>
    public static Profile PeppolBillingCii => PeppolProfiles.BillingCii;

    /// <summary>Every profile Denmark uses.</summary>
    public static IReadOnlyList<Profile> All { get; } = [PeppolBillingUbl, PeppolBillingCii];
}
