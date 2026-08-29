using International.EInvoicing.Profiles;

namespace International.EInvoicing.Cdar;

/// <summary>Lifecycle message profiles this library knows about.</summary>
public static class CdarProfiles
{
    /// <summary>
    /// The French lifecycle statuses — <em>statuts de cycle de vie</em> — profiled by the DGFiP over
    /// UN/CEFACT CDAR. Registering it is what turns the status codes from opaque values into a national
    /// vocabulary; without it a French message still reads, with its codes uninterpreted.
    /// </summary>
    public static Profile FrenchLifecycleStatus { get; } = new(
        new ProfileIdentifier("urn.cpro.gouv.fr:1p0:CDV:invoice"),
        "French lifecycle status (CDV)",
        DocumentSyntax.Cdar);

    /// <summary>Every lifecycle profile this library knows about.</summary>
    public static IReadOnlyList<Profile> All { get; } = [FrenchLifecycleStatus];
}
