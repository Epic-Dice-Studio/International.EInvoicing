using System.Xml.Schema;
using International.EInvoicing.Profiles;
using International.EInvoicing.Validation;

namespace International.EInvoicing.Validation.Xsd;

/// <summary>
/// The UN/CEFACT Cross Industry Invoice D22B schemas, as a rule set.
/// </summary>
/// <remarks>
/// The CII counterpart of <see cref="UblSchemaRuleSet"/>, and needed for the same reason: order and
/// cardinality are normative here too, and no business rule reads either. Factur-X and ZUGFeRD documents are
/// CII, so this is what says whether one is a shape a German or French receiver can parse.
/// </remarks>
public sealed class CiiSchemaRuleSet : IDocumentRuleSet
{
    private static readonly Lazy<XmlSchemaSet> Schemas =
        new(() => EmbeddedSchemas.Load("cii/"), LazyThreadSafetyMode.ExecutionAndPublication);

    /// <inheritdoc />
    public string Name => "UN/CEFACT CII D22B (schema)";

    /// <inheritdoc />
    public string Version => "D22B";

    /// <inheritdoc />
    public bool AppliesTo(DocumentSyntax syntax, ProfileIdentifier specification) => syntax == DocumentSyntax.Cii;

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException"><paramref name="document"/> is <c>null</c>.</exception>
    public ValidationReport Validate(string document)
    {
        ArgumentNullException.ThrowIfNull(document);

        return SchemaCheck.Run(document, Schemas.Value, Name, Version);
    }
}
