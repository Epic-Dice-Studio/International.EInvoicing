using International.EInvoicing.Profiles;

namespace International.EInvoicing.Countries.Romania;

/// <summary>
/// The Romanian profiles.
/// </summary>
/// <remarks>
/// <b>CIUS-RO</b> is Romania's national CIUS of EN 16931, and what the <em>e-Factura</em> mandate exchanges.
/// Its identifier carries the CIUS version, which is not the version of the rule set that checks it — the
/// artefacts are published at 1.0.9 and say themselves that they are "CIUS-RO version 1.0.1 compatible".
/// The identifier below is read from the artefact, not transcribed.
/// </remarks>
public static class RoProfiles
{
    private static readonly ProfileIdentifier CiusRoId =
        new("urn:cen.eu:en16931:2017#compliant#urn:efactura.mfinante.ro:CIUS-RO:1.0.1");

    /// <summary>CIUS-RO in UBL — what e-Factura carries.</summary>
    public static Profile CiusRoUbl { get; } =
        new(CiusRoId, "CIUS-RO", DocumentSyntax.Ubl, KnownProfiles.En16931Ubl.Id);

    /// <summary>Every profile Romania uses.</summary>
    public static IReadOnlyList<Profile> All { get; } = [CiusRoUbl];
}
