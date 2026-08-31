using International.EInvoicing.Peppol;
using International.EInvoicing.Profiles;

namespace International.EInvoicing.Countries.Belgium;

/// <summary>
/// The Belgian profiles.
/// </summary>
/// <remarks>
/// The 2026 mandate is built on Peppol BIS Billing 3.0 rather than on a Belgian format, so these are the
/// Peppol profiles: what Belgium adds is national rules on top, not a different document.
/// </remarks>
public static class BeProfiles
{
    /// <summary>
    /// <b>UBL.BE</b>, the Belgian conformant extension that <c>GLOBALUBL.BE</c> judges.
    /// </summary>
    /// <remarks>
    /// A document declaring plain Peppol BIS is refused by the Belgian rule set — <c>PEPPOL-EN16931-R004</c>
    /// there requires this identifier — so the two are not interchangeable, and this one is read from the
    /// rule set rather than transcribed.
    /// </remarks>
    public static Profile UblBe { get; } = new(
        new ProfileIdentifier("urn:cen.eu:en16931:2017#conformant#urn:UBL.BE:1.0.0.20180214"),
        "UBL.BE",
        DocumentSyntax.Ubl,
        KnownProfiles.En16931Ubl.Id);

    /// <summary>Peppol BIS Billing 3.0 in UBL, the syntax Belgium exchanges in.</summary>
    public static Profile PeppolBillingUbl => PeppolProfiles.BillingUbl;

    /// <summary>Peppol BIS Billing 3.0 in CII.</summary>
    public static Profile PeppolBillingCii => PeppolProfiles.BillingCii;

    /// <summary>Every profile Belgium uses.</summary>
    public static IReadOnlyList<Profile> All { get; } = [UblBe, PeppolBillingUbl, PeppolBillingCii];
}
