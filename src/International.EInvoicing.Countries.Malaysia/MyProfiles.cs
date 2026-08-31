using International.EInvoicing.Peppol;
using International.EInvoicing.Profiles;

namespace International.EInvoicing.Countries.Malaysia;

/// <summary>
/// The Malaysian profiles.
/// </summary>
/// <remarks>
/// Malaysia's <b>MyInvois</b> exchanges Peppol PINT with a specialisation of its own,
/// <c>urn:peppol:pint:billing-1@my-1</c> — not BIS Billing. Submitting to the tax authority is a separate
/// national API, which is transport and out of scope here; the document is PINT, and the document is what
/// this library does.
/// </remarks>
public static class MyProfiles
{
    /// <summary>Peppol PINT Billing, Malaysian specialisation.</summary>
    public static Profile PintBilling => PeppolPintProfiles.BillingMy;

    /// <summary>Every profile Malaysia uses.</summary>
    public static IReadOnlyList<Profile> All { get; } = [PintBilling];
}
