using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace International.EInvoicing.Profiles;

/// <summary>
/// Which edition of EN 16931 a document declares.
/// </summary>
/// <remarks>
/// <para>
/// The standard has been revised. CEN published <b>EN 16931-1:2026</b> in May 2026 and formally withdrew the
/// 2017 edition, which stays compliant only for a migration period. The revision is a ViDA revision — new
/// terms for the 2030 digital reporting requirements, invoice coding, early-payment discounts, late-payment
/// charges, wider handling of exempt supplies — and it is not backward compatible.
/// </para>
/// <para>
/// This library implements the 2017 edition, which is what every artefact and every CIUS published so far is
/// written against. What this type adds is honesty: a document declaring another edition is recognised as
/// <em>an EN 16931 invoice of an edition we do not implement</em> rather than as an unknown profile, and it is
/// read as far as the 2017 model reaches, with everything else kept in extension data. Register a profile of
/// your own for a newer edition and it wins, as with any other profile.
/// </para>
/// <para>
/// The edition is carried in the year segment of the specification identifier (BT-24) —
/// <c>urn:cen.eu:en16931:2017</c>, and whatever the identifier for the 2026 edition turns out to be. That
/// segment is read here rather than matched against a fixed list, because the published identifier for the
/// 2026 edition is not something this library can assert today.
/// </para>
/// </remarks>
/// <param name="Year">The year the edition is named after.</param>
public readonly record struct En16931Edition(int Year)
{
    private const string Prefix = "urn:cen.eu:en16931:";

    /// <summary>EN 16931-1:2017, the original edition. Withdrawn in May 2026, compliant while migrating.</summary>
    public static En16931Edition Original => new(2017);

    /// <summary>EN 16931-1:2026, the ViDA revision, published May 2026.</summary>
    /// <remarks>
    /// Named so a caller can ask about it. Whether this library implements it is
    /// <see cref="IsImplemented"/>, which today is <c>false</c>.
    /// </remarks>
    public static En16931Edition Revised => new(2026);

    /// <summary>The edition this library's model and shipped artefacts implement.</summary>
    public static En16931Edition Implemented => Original;

    /// <summary>The base specification identifier of this edition.</summary>
    public ProfileIdentifier Identifier =>
        new(Prefix + Year.ToString(CultureInfo.InvariantCulture));

    /// <summary>Whether this library implements this edition.</summary>
    public bool IsImplemented => Year == Implemented.Year;

    /// <summary>
    /// The edition a specification identifier declares, or <c>null</c> when it does not name one.
    /// </summary>
    /// <remarks>
    /// A CIUS carries its base edition in front of its own identifier — <c>urn:cen.eu:en16931:2017#compliant#…</c>
    /// — so this reads the prefix and stops at the first <c>#</c>.
    /// </remarks>
    public static En16931Edition? Of(ProfileIdentifier declared)
    {
        if (!declared.IsDeclared || !declared.Value.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return null;
        }

        ReadOnlySpan<char> rest = declared.Value.AsSpan(Prefix.Length);
        int end = rest.IndexOf('#');
        ReadOnlySpan<char> year = end < 0 ? rest : rest[..end];

        return int.TryParse(year, NumberStyles.None, CultureInfo.InvariantCulture, out int value)
            ? new En16931Edition(value)
            : null;
    }

    /// <summary>The edition a specification identifier declares, when it names one.</summary>
    public static bool TryGet(ProfileIdentifier declared, [NotNullWhen(true)] out En16931Edition? edition)
    {
        edition = Of(declared);
        return edition is not null;
    }

    /// <inheritdoc />
    public override string ToString() => $"EN 16931-1:{Year.ToString(CultureInfo.InvariantCulture)}";
}
