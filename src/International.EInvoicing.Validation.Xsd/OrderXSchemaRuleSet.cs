using System.Xml.Schema;
using International.EInvoicing.Profiles;
using International.EInvoicing.Validation;

namespace International.EInvoicing.Validation.Xsd;

/// <summary>
/// The Order-X schemas of one profile, as a rule set.
/// </summary>
/// <remarks>
/// <para>
/// Not embedded, unlike the UBL and CII ones: FNFE-MPE and FeRD publish Order-X behind a registration and
/// permit no redistribution, so the files are fetched and this is pointed at them.
/// </para>
/// <para>
/// A profile's schema is a restriction of the one above it, so a document is judged by the schema of the
/// profile it declares. Judging a BASIC document by the EXTENDED schema would pass things BASIC forbids.
/// </para>
/// </remarks>
public sealed class OrderXSchemaRuleSet : IDocumentRuleSet
{
    private readonly Lazy<XmlSchemaSet> _schemas;
    private readonly Func<ProfileIdentifier, bool> _applies;

    /// <summary>Creates a rule set from a directory of Order-X schemas.</summary>
    /// <param name="directory">A profile's schema directory — <c>basic</c>, <c>comfort</c> or <c>extended</c>.</param>
    /// <param name="name">What to call it in a report.</param>
    /// <param name="applies">Which declared profile identifiers this schema judges.</param>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    /// <exception cref="ArgumentException"><paramref name="directory"/> is empty.</exception>
    /// <exception cref="DirectoryNotFoundException">There is no such directory.</exception>
    public OrderXSchemaRuleSet(string directory, string name, Func<ProfileIdentifier, bool> applies)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(applies);

        if (!Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException(
                $"No Order-X schemas at '{directory}'. FNFE-MPE and FeRD publish them behind a registration, "
                + "so this library does not ship them: run build/fetch-specs.sh order-x, or point this at "
                + "your own copy.");
        }

        Name = name;
        _applies = applies;
        _schemas = new Lazy<XmlSchemaSet>(
            () => DirectorySchemas.Load(directory),
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public string Version => "1.0";

    /// <inheritdoc />
    public bool AppliesTo(DocumentSyntax syntax, ProfileIdentifier specification) =>
        syntax == DocumentSyntax.OrderX && _applies(specification);

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException"><paramref name="document"/> is <c>null</c>.</exception>
    public ValidationReport Validate(string document)
    {
        ArgumentNullException.ThrowIfNull(document);

        return SchemaCheck.Run(document, _schemas.Value, Name, Version);
    }
}
