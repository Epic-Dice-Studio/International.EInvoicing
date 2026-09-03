using System.Xml.Schema;
using International.EInvoicing.Profiles;
using International.EInvoicing.Validation;

namespace International.EInvoicing.Validation.Xsd;

/// <summary>
/// The UN/CEFACT Cross Industry Invoice schemas, as a rule set.
/// </summary>
/// <remarks>
/// <para>
/// The CII counterpart of <see cref="UblSchemaRuleSet"/>, and needed for the same reason: order and
/// cardinality are normative here too, and no business rule reads either.
/// </para>
/// <para>
/// <strong>Which revision matters, and the document will not tell you.</strong> D16B and D22B share their
/// namespaces, so either schema will happily attach to either document and the later one rejects values the
/// earlier one allows. EN 16931's CII syntax binding names <see cref="CiiSchemaVersion.D16B"/>, and so do
/// XRechnung, Factur-X and Peppol's CII profile — every CII profile this library implements. That is
/// therefore the default, and D22B is for a document you know to be written against it.
/// </para>
/// </remarks>
public sealed class CiiSchemaRuleSet : IDocumentRuleSet
{
    private static readonly Lazy<XmlSchemaSet> D16B =
        new(() => EmbeddedSchemas.Load("cii16b/"), LazyThreadSafetyMode.ExecutionAndPublication);

    private static readonly Lazy<XmlSchemaSet> D22B =
        new(() => EmbeddedSchemas.Load("cii22b/"), LazyThreadSafetyMode.ExecutionAndPublication);

    private readonly CiiSchemaVersion _version;

    /// <summary>Creates the rule set for the revision EN 16931 names.</summary>
    public CiiSchemaRuleSet()
        : this(CiiSchemaVersion.D16B)
    {
    }

    /// <summary>Creates the rule set for a chosen revision.</summary>
    /// <param name="version">Which revision of the schemas to judge by.</param>
    public CiiSchemaRuleSet(CiiSchemaVersion version) => _version = version;

    /// <inheritdoc />
    public string Name => $"UN/CEFACT CII {Version} (schema)";

    /// <inheritdoc />
    public string Version => _version == CiiSchemaVersion.D22B ? "D22B" : "D16B";

    /// <inheritdoc />
    public bool AppliesTo(DocumentSyntax syntax, ProfileIdentifier specification) => syntax == DocumentSyntax.Cii;

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException"><paramref name="document"/> is <c>null</c>.</exception>
    public ValidationReport Validate(string document)
    {
        ArgumentNullException.ThrowIfNull(document);

        XmlSchemaSet schemas = _version == CiiSchemaVersion.D22B ? D22B.Value : D16B.Value;

        return SchemaCheck.Run(document, schemas, Name, Version);
    }
}

/// <summary>
/// Which revision of the Cross Industry Invoice schemas to judge a document by.
/// </summary>
/// <remarks>
/// The document cannot tell you: both revisions use the same namespaces. What tells you is the profile —
/// every profile derived from EN 16931 is <see cref="D16B"/>.
/// </remarks>
public enum CiiSchemaVersion
{
    /// <summary>What EN 16931's CII syntax binding names, and what XRechnung, Factur-X and Peppol use.</summary>
    D16B,

    /// <summary>The 2022 revision, for a document you know to be written against it.</summary>
    D22B,
}
