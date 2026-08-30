using International.EInvoicing.Profiles;

namespace International.EInvoicing.Countries.Germany;

/// <summary>The German profiles.</summary>
/// <remarks>
/// XRechnung uses one identifier across both syntaxes, so each profile is registered twice — a German
/// receiver may send you either UBL or CII, and supporting only one means rejecting valid invoices.
/// </remarks>
public static class DeProfiles
{
    /// <summary>XRechnung 3.x, the German CIUS, in UBL.</summary>
    public static Profile XRechnungUbl => KnownProfiles.XRechnung3CiusUbl;

    /// <summary>XRechnung 3.x in CII.</summary>
    public static Profile XRechnungCii => KnownProfiles.XRechnung3CiusCii;

    /// <summary>The XRechnung Extension in UBL, which adds elements beyond EN 16931.</summary>
    public static Profile XRechnungExtensionUbl => KnownProfiles.XRechnung3ExtensionUbl;

    /// <summary>The XRechnung Extension in CII.</summary>
    public static Profile XRechnungExtensionCii => KnownProfiles.XRechnung3ExtensionCii;

    /// <summary>Every German profile.</summary>
    public static IReadOnlyList<Profile> All { get; } =
        [XRechnungUbl, XRechnungCii, XRechnungExtensionUbl, XRechnungExtensionCii];
}
