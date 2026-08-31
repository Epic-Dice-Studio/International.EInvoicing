using International.EInvoicing.Peppol;
using International.EInvoicing.Profiles;

namespace International.EInvoicing.Countries.Slovakia;

/// <summary>
/// The Slovak profiles.
/// </summary>
/// <remarks>
/// Slovakia's B2B mandate starts on <b>1 January 2027</b> and exchanges Peppol BIS Billing 3.0 — there is no
/// Slovak CIUS of EN 16931 in any artefact published so far, and this library does not invent one. What is
/// Slovak is the second document: a <b>tax data document</b>, sent to the financial administration, whose
/// identifier and rules OpenPeppol does publish.
/// </remarks>
public static class SkProfiles
{
    /// <summary>Peppol BIS Billing 3.0 in UBL, the syntax the mandate exchanges.</summary>
    public static Profile PeppolBillingUbl => PeppolProfiles.BillingUbl;

    /// <summary>Peppol BIS Billing 3.0 in CII.</summary>
    public static Profile PeppolBillingCii => PeppolProfiles.BillingCii;

    /// <summary>Every profile this package registers.</summary>
    public static IReadOnlyList<Profile> All { get; } = [PeppolBillingUbl, PeppolBillingCii];
}
