using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using International.EInvoicing.Profiles;
using International.EInvoicing.Validation;
using International.EInvoicing.Xml;

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
    private static readonly Lazy<XmlSchemaSet> Schemas = new(Load, LazyThreadSafetyMode.ExecutionAndPublication);

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

        var messages = new List<ValidationMessage>();

        XDocument parsed;
        try
        {
            using XmlReader reader = SecureXml.CreateReader(document, DocumentLimits.Unlimited);
            parsed = XDocument.Load(reader, LoadOptions.SetLineInfo);
        }
        catch (XmlException exception)
        {
            // A document that will not parse has no shape to judge, and saying so is the honest answer.
            return new ValidationReport(
                [Message("XSD-PARSE", exception.Message, exception.LineNumber, exception.LinePosition)],
                [new RuleSetOutcome("OASIS UBL 2.1 (schema)", "2.1", Ran: false, "the document is not XML")]);
        }

        parsed.Validate(
            Schemas.Value,
            (_, arguments) => messages.Add(Message(
                Identifier(arguments.Exception),
                arguments.Message,
                arguments.Exception?.LineNumber ?? 0,
                arguments.Exception?.LinePosition ?? 0)),
            addSchemaInfo: false);

        return new ValidationReport(messages, [new RuleSetOutcome(Name, Version, Ran: true)]);
    }

    /// <summary>
    /// What to call a schema failure, since a schema has no rule identifiers of its own.
    /// </summary>
    /// <remarks>
    /// A caller filtering a report by identifier needs something stable to filter on, and "the element order
    /// is wrong" and "that element is not allowed here" are different problems to whoever has to fix them.
    /// </remarks>
    private static string Identifier(XmlSchemaException? exception) =>
        exception?.Message.Contains("has invalid child element", StringComparison.Ordinal) == true
            ? "XSD-SEQUENCE"
            : "XSD";

    private static ValidationMessage Message(string identifier, string message, int line, int position) =>
        new(identifier, RuleSeverity.Error, message)
        {
            RuleSet = "OASIS UBL 2.1 (schema)",
            Location = line > 0
                ? string.Create(CultureInfo.InvariantCulture, $"line {line}, position {position}")
                : null,
        };

    private static XmlSchemaSet Load()
    {
        var set = new XmlSchemaSet
        {
            // Nothing is fetched: every import resolves inside the embedded set, and anything else is refused.
            XmlResolver = new EmbeddedSchemaResolver(),
        };

        Assembly assembly = typeof(UblSchemaRuleSet).Assembly;

        // Every schema, not only the document ones: the set then needs to resolve nothing, and an untrusted
        // document cannot point the validator at anything, because there is nothing left to point at.
        foreach (string name in assembly.GetManifestResourceNames()
            .Where(name => name.EndsWith(".xsd", StringComparison.OrdinalIgnoreCase))
            .Order(StringComparer.Ordinal))
        {
            using Stream stream = assembly.GetManifestResourceStream(name)!;
            // DTD parsing is allowed here and nowhere else: the W3C's xmldsig schema, which UBL imports for
            // cac:Signature, carries an internal subset of entity declarations. These files are embedded in
            // this assembly rather than arriving from anywhere, and the resolver is null, so nothing external
            // is fetched. The documents being *validated* are still read through SecureXml, which refuses DTDs.
            using XmlReader reader = XmlReader.Create(
                stream,
                new XmlReaderSettings { XmlResolver = null, DtdProcessing = DtdProcessing.Parse },
                EmbeddedSchemaResolver.BaseUri + Path.GetFileName(name));

            set.Add(null, reader);
        }

        set.Compile();
        return set;
    }

    /// <summary>Resolves every schema import to the copy embedded in this assembly, and nothing else.</summary>
    private sealed class EmbeddedSchemaResolver : XmlResolver
    {
        public const string BaseUri = "einvoicing-ubl:///";

        private static readonly ConcurrentDictionary<string, string?> Resources = new(StringComparer.Ordinal);

        public override object? GetEntity(Uri absoluteUri, string? role, Type? typeOfObjectToReturn)
        {
            ArgumentNullException.ThrowIfNull(absoluteUri);

            string file = Path.GetFileName(absoluteUri.AbsolutePath);
            string? resource = Resources.GetOrAdd(file, Find);

            return resource is null ? null : typeof(UblSchemaRuleSet).Assembly.GetManifestResourceStream(resource);
        }

        public override Uri ResolveUri(Uri? baseUri, string? relativeUri) =>
            new(BaseUri + Path.GetFileName(relativeUri ?? string.Empty), UriKind.Absolute);

        private static string? Find(string file) =>
            typeof(UblSchemaRuleSet).Assembly.GetManifestResourceNames()
                .FirstOrDefault(name => name.EndsWith(file, StringComparison.OrdinalIgnoreCase));
    }
}
