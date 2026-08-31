using International.EInvoicing.Peppol;
using International.EInvoicing.Profiles;

namespace International.EInvoicing.Countries.Singapore;

/// <summary>
/// The Singaporean profiles.
/// </summary>
/// <remarks>
/// Singapore exchanges <b>InvoiceNow</b>, which runs on Peppol PINT with a specialisation of its own —
/// <c>urn:peppol:pint:billing-1@sg-1</c>, not BIS Billing. The two families differ in the profile identifier
/// and in the business process, and Singapore's rules are written in terms of <b>GST</b> throughout.
/// </remarks>
public static class SgProfiles
{
    /// <summary>Peppol PINT Billing, Singaporean specialisation — what InvoiceNow carries.</summary>
    public static Profile PintBilling => PeppolPintProfiles.BillingSg;

    /// <summary>Every profile Singapore uses.</summary>
    public static IReadOnlyList<Profile> All { get; } = [PintBilling];
}
