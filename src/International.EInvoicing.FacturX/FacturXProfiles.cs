using International.EInvoicing.Profiles;

namespace International.EInvoicing.FacturX;

/// <summary>
/// The five Factur-X profiles, cumulative from the least to the most complete. ZUGFeRD publishes the same
/// standard under its own name; the identifiers are shared.
/// </summary>
/// <remarks>
/// MINIMUM and BASIC WL are deliberately <em>not</em> EN 16931 invoices: they carry header data and totals,
/// without the lines the norm requires. They exist for accounting hand-off, and their legal use is narrow.
/// Choosing a profile is a business decision, so nothing here picks one for the caller.
/// </remarks>
public static class FacturXProfiles
{
    /// <summary>Header data and totals only. Not a complete EN 16931 invoice.</summary>
    public static Profile Minimum => KnownProfiles.FacturXMinimum;

    /// <summary>Adds the VAT breakdown, still without invoice lines. Not a complete EN 16931 invoice.</summary>
    public static Profile BasicWithoutLines => KnownProfiles.FacturXBasicWl;

    /// <summary>Adds invoice lines. The common minimum for exchange.</summary>
    public static Profile Basic => KnownProfiles.FacturXBasic;

    /// <summary>Full EN 16931 conformance, also known as COMFORT.</summary>
    public static Profile En16931 => KnownProfiles.En16931Cii;

    /// <summary>Adds elements beyond EN 16931, for bilateral or sector-specific needs.</summary>
    public static Profile Extended => KnownProfiles.FacturXExtended;

    /// <summary>Every Factur-X profile, from the least to the most complete.</summary>
    public static IReadOnlyList<Profile> All { get; } = [Minimum, BasicWithoutLines, Basic, En16931, Extended];

    /// <summary>
    /// The name Factur-X's metadata gives a profile, which is not its identifier.
    /// </summary>
    /// <remarks>
    /// The XMP says <c>BASIC WL</c> where BT-24 says
    /// <c>urn:factur-x.eu:1p0:basicwl</c>: same profile, two vocabularies, and the container is judged
    /// against the document by comparing them.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="profile"/> is <c>null</c>.</exception>
    public static string ConformanceLevelOf(Profile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        if (profile == Minimum)
        {
            return "MINIMUM";
        }

        if (profile == BasicWithoutLines)
        {
            return "BASIC WL";
        }

        if (profile == Basic)
        {
            return "BASIC";
        }

        return profile == Extended ? "EXTENDED" : "EN 16931";
    }

    /// <summary>Whether a profile carries everything EN 16931 requires of an invoice.</summary>
    public static bool IsEn16931Compliant(Profile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        return profile != Minimum && profile != BasicWithoutLines;
    }
}
