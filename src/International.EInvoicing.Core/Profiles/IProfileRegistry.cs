namespace International.EInvoicing.Profiles;

/// <summary>
/// The profiles this instance of the library actually implements. Registering a profile from your own code
/// is a supported scenario: it takes precedence over anything the library ships.
/// </summary>
public interface IProfileRegistry
{
    /// <summary>Every registered profile.</summary>
    IReadOnlyCollection<Profile> All { get; }

    /// <summary>Finds a registered profile by identifier, and by syntax when one is known.</summary>
    Profile? Find(ProfileIdentifier id, DocumentSyntax syntax);

    /// <summary>Whether a profile is registered for this identifier and syntax.</summary>
    bool IsSupported(ProfileIdentifier id, DocumentSyntax syntax);
}
