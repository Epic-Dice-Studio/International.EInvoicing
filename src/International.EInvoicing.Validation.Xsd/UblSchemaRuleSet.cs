using System.Xml.Schema;
using International.EInvoicing.Profiles;
using International.EInvoicing.Validation;

namespace International.EInvoicing.Validation.Xsd;

/// <summary>
/// The OASIS UBL 2.1 schemas, as a rule set.
/// </summary>
/// <remarks>
/// <para>
/// Schematron judges what a document <em>says</em>; the schema judges what shape it is in. The two do not
/// overlap: element order and cardinality are normative in UBL and no business rule looks at them, so a
/// document can satisfy all 955 EN 16931 assertions and still be something a receiver's parser rejects
/// outright. This library shipped exactly that defect — two bank accounts inside one <c>cac:PaymentMeans</c>,
/// where UBL allows one — and every rule set it owns said the invoice was fine.
/// </para>
/// <para>
/// The schemas are embedded, so this needs nothing fetched and no network. Resolution between them is
/// internal: no <c>xs:import</c> reaches outside the set, which is what keeps an untrusted document from
/// pointing the validator anywhere.
/// </para>
/// </remarks>
public sealed class UblSchemaRuleSet : IDocumentRuleSet
{
    private static readonly Lazy<XmlSchemaSet> Schemas =
        new(() => EmbeddedSchemas.Load("ubl/"), LazyThreadSafetyMode.ExecutionAndPublication);

    /// <inheritdoc />
    public string Name => "OASIS UBL 2.1 (schema)";

    /// <inheritdoc />
    public string Version => "2.1";

    /// <inheritdoc />
    /// <remarks>Every UBL document, whatever it declares in BT-24: a schema is about the syntax, not the profile.</remarks>
    public bool AppliesTo(DocumentSyntax syntax, ProfileIdentifier specification) => syntax == DocumentSyntax.Ubl;

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException"><paramref name="document"/> is <c>null</c>.</exception>
    public ValidationReport Validate(string document)
    {
        ArgumentNullException.ThrowIfNull(document);

        return SchemaCheck.Run(document, Schemas.Value, Name, Version);
    }
}
