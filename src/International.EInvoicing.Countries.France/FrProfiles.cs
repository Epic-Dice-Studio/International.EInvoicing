using International.EInvoicing.Profiles;

namespace International.EInvoicing.Countries.France;

/// <summary>The French profiles of the 2026 reform.</summary>
public static class FrProfiles
{
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

    /// <summary>Every French profile this library knows about.</summary>
    public static IReadOnlyList<Profile> All { get; } = [LifecycleStatusToPartner, LifecycleStatusToPublicPortal];
}
