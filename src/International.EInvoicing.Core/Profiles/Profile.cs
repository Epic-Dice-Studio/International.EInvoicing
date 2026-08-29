namespace International.EInvoicing.Profiles;

/// <summary>
/// A specification a document can claim to follow: a CIUS, an extension, or a private agreement between two
/// partners.
/// </summary>
/// <param name="Id">The identifier documents declare in BT-24.</param>
/// <param name="Name">A human-readable name, used in diagnostics and reports.</param>
/// <param name="Syntax">The syntax this profile applies to.</param>
/// <param name="Parent">
/// The profile this one restricts. It is what resolution falls back to when this profile is not implemented,
/// so a document is read as much as it can be rather than not at all.
/// </param>
public sealed record Profile(
    ProfileIdentifier Id,
    string Name,
    DocumentSyntax Syntax,
    ProfileIdentifier? Parent = null)
{
    /// <inheritdoc />
    public override string ToString() => $"{Name} ({Id})";
}
