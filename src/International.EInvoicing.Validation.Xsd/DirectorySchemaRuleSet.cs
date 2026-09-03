using System.Xml.Schema;
using International.EInvoicing.Profiles;
using International.EInvoicing.Validation;

namespace International.EInvoicing.Validation.Xsd;

/// <summary>
/// A set of schemas loaded from a directory, as a rule set.
/// </summary>
/// <remarks>
/// <para>
/// Not embedded, unlike the UBL and CII ones: some publishers permit no redistribution — FNFE-MPE and FeRD
/// put Order-X behind a registration, and no longer publish ZUGFeRD 1.0 at all — so the files are fetched
/// and this is pointed at them.
/// </para>
/// <para>
/// One instance judges one profile. A profile's schema is a restriction of the one above it, so a document
/// is judged by the schema of the profile it declares: judging a BASIC document by the EXTENDED schema would
/// pass things BASIC forbids.
/// </para>
/// </remarks>
public sealed class DirectorySchemaRuleSet : IDocumentRuleSet
{
    private readonly Lazy<XmlSchemaSet> _schemas;
    private readonly DocumentSyntax _syntax;
    private readonly Func<ProfileIdentifier, bool> _applies;

    /// <summary>Creates a rule set from a directory of schemas.</summary>
    /// <param name="directory">Where the <c>.xsd</c> files are.</param>
    /// <param name="name">What to call it in a report.</param>
    /// <param name="version">Which version it is, so a report can be reproduced later.</param>
    /// <param name="syntax">The syntax these schemas describe.</param>
    /// <param name="applies">Which declared profile identifiers this schema judges.</param>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    /// <exception cref="ArgumentException"><paramref name="directory"/> is empty.</exception>
    /// <exception cref="DirectoryNotFoundException">There is no such directory.</exception>
    public DirectorySchemaRuleSet(
        string directory,
        string name,
        string version,
        DocumentSyntax syntax,
        Func<ProfileIdentifier, bool> applies)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(version);
        ArgumentNullException.ThrowIfNull(applies);

        if (!Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException(
                $"No schemas at '{directory}'. This library does not ship them, because their publisher does "
                + "not permit it: run build/fetch-specs.sh, or point this at your own copy.");
        }

        Name = name;
        Version = version;
        _syntax = syntax;
        _applies = applies;
        _schemas = new Lazy<XmlSchemaSet>(
            () => DirectorySchemas.Load(directory),
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public string Version { get; }

    /// <inheritdoc />
    public bool AppliesTo(DocumentSyntax syntax, ProfileIdentifier specification) =>
        syntax == _syntax && _applies(specification);

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException"><paramref name="document"/> is <c>null</c>.</exception>
    public ValidationReport Validate(string document)
    {
        ArgumentNullException.ThrowIfNull(document);

        return SchemaCheck.Run(document, _schemas.Value, Name, Version);
    }
}
