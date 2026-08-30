using International.EInvoicing.Profiles;

namespace International.EInvoicing.Countries.France;

/// <summary>The French profiles of the 2026 reform.</summary>
public static class FrProfiles
{
    private static readonly ProfileIdentifier ExtendedCtcFrId =
        new("urn:cen.eu:en16931:2017#conformant#urn.cpro.gouv.fr:1p0:extended-ctc-fr");

    /// <summary>
    /// Lifecycle statuses exchanged with a trading partner through an approved platform.
    /// </summary>
    public static Profile LifecycleStatusToPartner { get; } = new(
        new ProfileIdentifier("urn.cpro.gouv.fr:1p0:CDV:invoice"),
        "French lifecycle status, to a partner",
        DocumentSyntax.Cdar);

    /// <summary>
    /// Lifecycle statuses reported to the public portal. A different profile from the partner one, not a
    /// variant of it: the message is addressed differently and carries a different context.
    /// </summary>
    public static Profile LifecycleStatusToPublicPortal { get; } = new(
        new ProfileIdentifier("urn.cpro.gouv.fr:1p0:CDV:einvoicingF2"),
        "French lifecycle status, to the public portal",
        DocumentSyntax.Cdar);

    /// <summary>
    /// The French invoice profile, in UBL.
    /// </summary>
    /// <remarks>
    /// It declares <c>#conformant#</c>, not <c>#compliant#</c>: it is an extension of EN 16931 rather than a
    /// restriction of it, so a French invoice may legitimately carry what the base rules reject — the same
    /// relationship the XRechnung Extension has. Validating one against EN 16931 alone can therefore report
    /// failures that are not failures.
    /// </remarks>
    public static Profile ExtendedCtcFrUbl { get; } = new(
        ExtendedCtcFrId,
        "Extended CTC FR",
        DocumentSyntax.Ubl,
        KnownProfiles.En16931Ubl.Id);

    /// <summary>The French invoice profile, in CII.</summary>
    public static Profile ExtendedCtcFrCii { get; } = new(
        ExtendedCtcFrId,
        "Extended CTC FR",
        DocumentSyntax.Cii,
        KnownProfiles.En16931Cii.Id);

    /// <summary>Every French profile this library knows about.</summary>
    public static IReadOnlyList<Profile> All { get; } =
    [
        ExtendedCtcFrUbl,
        ExtendedCtcFrCii,
        LifecycleStatusToPartner,
        LifecycleStatusToPublicPortal,
    ];
}
