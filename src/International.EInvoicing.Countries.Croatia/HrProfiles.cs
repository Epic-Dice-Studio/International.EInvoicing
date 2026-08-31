using International.EInvoicing.Peppol;
using International.EInvoicing.Profiles;

namespace International.EInvoicing.Countries.Croatia;

/// <summary>
/// The Croatian profiles.
/// </summary>
/// <remarks>
/// <para>
/// Croatia's <em>Fiskalizacija 2.0</em> mandate, live for domestic B2B since 1 January 2026, exchanges
/// UBL 2.1 over a five-corner Peppol-style network, so the Peppol profiles are here too.
/// </para>
/// <para>
/// <b>CIUS-HR</b> is Croatia's own specialisation, and it never travels alone: the published rule set tests
/// BT-24 for the CIUS <em>and</em> the conformant extension in one identifier, which is why there is one
/// profile here rather than two. It is read out of the artefact — a guessed identifier in BT-24 makes every
/// document written with it wrong, which is why this was left out until the artefact could be read.
/// </para>
/// <para>
/// A document carrying this identifier is judged by rules that require business terms EN 16931 does not
/// define — the time of issue, the operator's OIB, a second VAT breakdown — which live in Croatia's own UBL
/// extension. See <c>docs/standards/country-hr.md</c> for what this library supplies and what it does not.
/// </para>
/// </remarks>
public static class HrProfiles
{
    private static readonly ProfileIdentifier CiusHrId = new(
        "urn:cen.eu:en16931:2017#compliant#urn:mfin.gov.hr:cius-2025:1.0"
        + "#conformant#urn:mfin.gov.hr:ext-2025:1.0");

    /// <summary>Peppol BIS Billing 3.0 in UBL, the syntax Croatia exchanges in.</summary>
    public static Profile PeppolBillingUbl => PeppolProfiles.BillingUbl;

    /// <summary>Peppol BIS Billing 3.0 in CII.</summary>
    public static Profile PeppolBillingCii => PeppolProfiles.BillingCii;

    /// <summary>CIUS-HR with the Croatian extension — what <em>Fiskalizacija 2.0</em> exchanges.</summary>
    public static Profile CiusHrUbl { get; } =
        new(CiusHrId, "CIUS-HR 2025", DocumentSyntax.Ubl, KnownProfiles.En16931Ubl.Id);

    /// <summary>Every profile this package registers.</summary>
    public static IReadOnlyList<Profile> All { get; } = [PeppolBillingUbl, PeppolBillingCii, CiusHrUbl];
}
