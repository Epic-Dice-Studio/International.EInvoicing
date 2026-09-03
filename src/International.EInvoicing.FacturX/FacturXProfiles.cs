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

    /// <summary>
    /// The German CIUS, carried in a hybrid PDF. ZUGFeRD calls this conformance level <c>XRECHNUNG</c>.
    /// </summary>
    /// <remarks>
    /// It is not a Factur-X profile of its own: it is XRechnung's CII binding travelling in a Factur-X
    /// container, which is how German senders deliver a hybrid XRechnung. The container must say so, because
    /// a receiver told <c>EN 16931</c> will not apply the German rules the document is written against.
    /// </remarks>
    public static Profile XRechnung => KnownProfiles.XRechnung3CiusCii;

    /// <summary>The XRechnung extension, which declares the same conformance level.</summary>
    public static Profile XRechnungExtension => KnownProfiles.XRechnung3ExtensionCii;

    /// <summary>Every Factur-X profile, from the least to the most complete.</summary>
    /// <remarks>
    /// The German ones are here because a container carrying them must be judged against what it claims, and
    /// they are last because they are not a rung on the MINIMUM-to-EXTENDED ladder.
    /// </remarks>
    public static IReadOnlyList<Profile> All { get; } =
        [Minimum, BasicWithoutLines, Basic, En16931, Extended, XRechnung, XRechnungExtension];

    /// <summary>
    /// The name Factur-X's metadata gives a profile, which is not its identifier.
    /// </summary>
    /// <remarks>
    /// The XMP says <c>BASIC WL</c> where BT-24 says
    /// <c>urn:factur-x.eu:1p0:basicwl</c>: same profile, two vocabularies, and the container is judged
    /// against the document by comparing them.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="profile"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">
    /// Factur-X publishes no conformance level for that profile. Answering <c>EN 16931</c> for anything
    /// unrecognised is how a container comes to claim a profile its payload is not written against, which is
    /// the disagreement <see cref="FacturXDiagnostics.MetadataDisagrees"/> exists to catch — so this refuses
    /// rather than guesses.
    /// </exception>
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

        if (profile == Extended)
        {
            return "EXTENDED";
        }

        if (profile == XRechnung || profile == XRechnungExtension)
        {
            return XRechnungLevel;
        }

        return profile == En16931
            ? "EN 16931"
            : throw new ArgumentException(
                $"Factur-X publishes no conformance level for '{profile.Id}'. A container can only claim one "
                + "of MINIMUM, BASIC WL, BASIC, EN 16931, EXTENDED or XRECHNUNG, and a container that claims "
                + "the wrong one is read as a different invoice by every receiver that trusts it.",
                nameof(profile));
    }

    /// <summary>The conformance level a hybrid XRechnung declares.</summary>
    public const string XRechnungLevel = "XRECHNUNG";

    /// <summary>Whether a profile carries everything EN 16931 requires of an invoice.</summary>
    public static bool IsEn16931Compliant(Profile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        return profile != Minimum && profile != BasicWithoutLines;
    }
}
