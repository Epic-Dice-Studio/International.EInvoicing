using International.EInvoicing.Diagnostics;

namespace International.EInvoicing.Profiles;

/// <summary>
/// Walks the fallback chain: the exact profile, then the profile it restricts, then the base EN 16931 profile
/// for the syntax, then generic reading. Every step past the first is reported, naming what was expected and
/// what was used instead — a document read with a fallback must never look like one read with its own profile.
/// </summary>
public sealed class ProfileResolver : IProfileResolver
{
    private readonly IProfileRegistry _registry;

    /// <summary>Creates a resolver over <paramref name="registry"/>.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="registry"/> is <c>null</c>.</exception>
    public ProfileResolver(IProfileRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        _registry = registry;
    }

    /// <summary>What this resolver knows about, for a caller asking what the library supports.</summary>
    public IProfileRegistry Registry => _registry;

    /// <inheritdoc />
    public ProfileResolution Resolve(ProfileIdentifier declared, DocumentSyntax syntax)
    {
        if (!declared.IsDeclared)
        {
            return new ProfileResolution(
                FallbackProfile(syntax),
                declared,
                ProfileResolutionOutcome.Undeclared,
                []);
        }

        if (_registry.Find(declared, syntax) is { } exact)
        {
            return new ProfileResolution(exact, declared, ProfileResolutionOutcome.Exact, []);
        }

        Profile? fallback = NearestSupportedAncestor(declared, syntax) ?? FallbackProfile(syntax);

        if (En16931Edition.Of(declared) is { IsImplemented: false } edition)
        {
            return new ProfileResolution(
                fallback,
                declared,
                ProfileResolutionOutcome.FellBackFromUnsupported,
                [ReportEdition(declared, edition, Describe(fallback, syntax))]);
        }

        bool isPublishedStandard = KnownProfiles.Find(declared, syntax) is not null;

        return new ProfileResolution(
            fallback,
            declared,
            isPublishedStandard
                ? ProfileResolutionOutcome.FellBackFromUnsupported
                : ProfileResolutionOutcome.FellBackFromUnknown,
            [ReportDowngrade(declared, isPublishedStandard, Describe(fallback, syntax))]);
    }

    /// <summary>
    /// A document written against another edition of the standard. It is an EN 16931 invoice, so say so:
    /// naming it "unknown" would send the reader looking for a profile registration they cannot make.
    /// </summary>
    private static Diagnostic ReportEdition(
        ProfileIdentifier declared,
        En16931Edition edition,
        string fallbackDescription) =>
        Diagnostic.Create(DiagnosticCodes.UnsupportedEdition, declared.Value) with
        {
            BusinessTerm = "BT-24",
            Expected = En16931Edition.Implemented.ToString(),
            Found = edition.ToString(),
            AppliedFallback = fallbackDescription
                + "; terms this edition does not have are kept in extension data",
        };

    private static Diagnostic ReportDowngrade(
        ProfileIdentifier declared,
        bool isPublishedStandard,
        string fallbackDescription)
    {
        DiagnosticDescriptor descriptor = isPublishedStandard
            ? DiagnosticCodes.UnsupportedProfile
            : DiagnosticCodes.UnknownProfile;

        string expected = isPublishedStandard
            ? "a registered implementation of this profile"
            : "a registered profile";

        return Diagnostic.Create(descriptor, declared.Value) with
        {
            BusinessTerm = "BT-24",
            Expected = expected,
            Found = declared.Value,
            AppliedFallback = fallbackDescription,
        };
    }

    private Profile? NearestSupportedAncestor(ProfileIdentifier declared, DocumentSyntax syntax)
    {
        ProfileIdentifier? parent = KnownProfiles.Find(declared, syntax)?.Parent;
        var seen = new HashSet<string>(StringComparer.Ordinal) { declared.Value };

        while (parent is { } candidate && seen.Add(candidate.Value))
        {
            if (_registry.Find(candidate, syntax) is { } supported)
            {
                return supported;
            }

            parent = KnownProfiles.Find(candidate, syntax)?.Parent;
        }

        return null;
    }

    private Profile? FallbackProfile(DocumentSyntax syntax)
    {
        ProfileIdentifier baseline = KnownProfiles.En16931Cii.Id;
        return _registry.Find(baseline, syntax);
    }

    private static string Describe(Profile? fallback, DocumentSyntax syntax) =>
        fallback is null
            ? $"generic {syntax} reading; no profile rules applied"
            : $"read as {fallback.Name}";
}
