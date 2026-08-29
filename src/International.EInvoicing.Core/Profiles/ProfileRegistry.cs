namespace International.EInvoicing.Profiles;

/// <summary>The default registry. Later registrations of the same identifier and syntax replace earlier ones.</summary>
public sealed class ProfileRegistry : IProfileRegistry
{
    private readonly Dictionary<(string Id, string Syntax), Profile> _profiles = [];

    /// <summary>Creates an empty registry.</summary>
    public ProfileRegistry()
    {
    }

    /// <summary>Creates a registry holding <paramref name="profiles"/>.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="profiles"/> is <c>null</c>.</exception>
    public ProfileRegistry(IEnumerable<Profile> profiles)
    {
        ArgumentNullException.ThrowIfNull(profiles);

        foreach (Profile profile in profiles)
        {
            Register(profile);
        }
    }

    /// <inheritdoc />
    public IReadOnlyCollection<Profile> All => _profiles.Values;

    /// <summary>Adds a profile, replacing any profile already registered for the same identifier and syntax.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="profile"/> is <c>null</c>.</exception>
    public void Register(Profile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        _profiles[Key(profile.Id, profile.Syntax)] = profile;
    }

    /// <inheritdoc />
    public Profile? Find(ProfileIdentifier id, DocumentSyntax syntax)
    {
        if (!id.IsDeclared)
        {
            return null;
        }

        if (syntax.IsKnown)
        {
            return _profiles.GetValueOrDefault(Key(id, syntax));
        }

        return _profiles.Values.FirstOrDefault(p => p.Id == id);
    }

    /// <inheritdoc />
    public bool IsSupported(ProfileIdentifier id, DocumentSyntax syntax) => Find(id, syntax) is not null;

    private static (string, string) Key(ProfileIdentifier id, DocumentSyntax syntax) =>
        (id.Value, syntax.Name ?? string.Empty);
}
