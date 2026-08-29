namespace International.EInvoicing.Profiles;

/// <summary>Turns the identifier a document declares into the profile the reader will actually use.</summary>
public interface IProfileResolver
{
    /// <summary>Resolves <paramref name="declared"/>, falling back as far as needed to read the document.</summary>
    ProfileResolution Resolve(ProfileIdentifier declared, DocumentSyntax syntax);
}
