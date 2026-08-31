using International.EInvoicing.Profiles;

namespace International.EInvoicing.Countries.Portugal;

/// <summary>
/// The Portuguese profiles.
/// </summary>
/// <remarks>
/// <b>CIUS-PT</b> is Portugal's national CIUS of EN 16931, published by the eSPap. Its identifier ends in a
/// version, and the published rule set accepts any version there — so this names the one the current
/// artefact validates, which the publisher states in the artefact's own file name.
/// </remarks>
public static class PtProfiles
{
    /// <summary>The identifier without its version, for a caller pinning a different one.</summary>
    public const string Prefix = "urn:cen.eu:en16931:2017#compliant#urn:feap.gov.pt:CIUS-PT:";

    /// <summary>CIUS-PT 2.1.1 in UBL.</summary>
    public static Profile CiusPtUbl { get; } = new(
        new ProfileIdentifier(Prefix + "2.1.1"),
        "CIUS-PT",
        DocumentSyntax.Ubl,
        KnownProfiles.En16931Ubl.Id);

    /// <summary>Every profile Portugal uses.</summary>
    public static IReadOnlyList<Profile> All { get; } = [CiusPtUbl];
}
