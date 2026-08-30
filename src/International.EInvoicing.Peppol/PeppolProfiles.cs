using International.EInvoicing.Profiles;

namespace International.EInvoicing.Peppol;

/// <summary>
/// The Peppol BIS Billing 3.0 profiles.
/// </summary>
/// <remarks>
/// Peppol adds no elements to EN 16931: it restricts it and constrains which identifier schemes may be used.
/// The work is therefore in rules and code lists rather than in the model, which is why one package serves
/// every country that exchanges over the Peppol network.
/// </remarks>
public static class PeppolProfiles
{
    /// <summary>Peppol BIS Billing 3.0 in UBL — the syntax the network actually carries.</summary>
    public static Profile BillingUbl => KnownProfiles.PeppolBisBilling3Ubl;

    /// <summary>Peppol BIS Billing 3.0 in CII.</summary>
    public static Profile BillingCii => KnownProfiles.PeppolBisBilling3Cii;

    /// <summary>Both, for registration.</summary>
    public static IReadOnlyList<Profile> All { get; } = [BillingUbl, BillingCii];

    /// <summary>The file names of the rule sets Peppol publishes, as they are named upstream.</summary>
    /// <remarks>
    /// Peppol ships two per syntax: its own rules, and its copy of the EN 16931 ones. Both apply — running
    /// only the first gives a false pass.
    /// </remarks>
    public static IReadOnlyList<string> RuleSetFileNames { get; } =
    [
        "PEPPOL-EN16931-UBL.sch",
        "PEPPOL-EN16931-CII.sch",
        "CEN-EN16931-UBL.sch",
        "CEN-EN16931-CII.sch",
    ];
}
