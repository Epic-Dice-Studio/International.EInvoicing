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
    private static readonly ProfileIdentifier NlciusId =
        new("urn:cen.eu:en16931:2017#compliant#urn:fdc:nen.nl:nlcius:v1.0");

    /// <summary>NLCIUS, the Dutch national CIUS of EN 16931, in UBL — what SI-UBL 2.0 carries.</summary>
    public static Profile NlciusUbl { get; } =
        new(NlciusId, "NLCIUS", DocumentSyntax.Ubl, KnownProfiles.En16931Ubl.Id);

    /// <summary>NLCIUS in CII.</summary>
    public static Profile NlciusCii { get; } =
        new(NlciusId, "NLCIUS", DocumentSyntax.Cii, KnownProfiles.En16931Cii.Id);

    /// <summary>
    /// The NLCIUS G-account extension, for an invoice naming a blocked-funds account.
    /// </summary>
    /// <remarks>
    /// A <em>conformant</em> extension rather than a CIUS: it adds beyond EN 16931 instead of restricting it,
    /// which is why its identifier says <c>#conformant#</c>.
    /// </remarks>
    public static Profile NlciusGAccountUbl { get; } = new(
        new ProfileIdentifier(
            "urn:cen.eu:en16931:2017#compliant#urn:fdc:nen.nl:nlcius:v1.0"
            + "#conformant#urn:fdc:nen.nl:gaccount:v1.0"),
        "NLCIUS with G-account",
        DocumentSyntax.Ubl,
        NlciusId);

    /// <summary>Peppol BIS Billing 3.0 in UBL, the syntax the Netherlands exchanges in.</summary>
    public static Profile PeppolBillingUbl => PeppolProfiles.BillingUbl;

    /// <summary>Peppol BIS Billing 3.0 in CII.</summary>
    public static Profile PeppolBillingCii => PeppolProfiles.BillingCii;

    /// <summary>Every profile this package registers.</summary>
    public static IReadOnlyList<Profile> All { get; } =
        [NlciusUbl, NlciusCii, NlciusGAccountUbl, PeppolBillingUbl, PeppolBillingCii];
}
