using System.Collections.Concurrent;
using System.Reflection;
using System.Xml;
using System.Xml.Schema;

namespace International.EInvoicing.Validation.Xsd;

/// <summary>
/// Loads a set of schemas out of this assembly, and resolves their imports to each other.
/// </summary>
/// <remarks>
/// Nothing is fetched at any point: every <c>xs:import</c> resolves inside the embedded set, and anything
/// else is refused. An untrusted document therefore has nowhere to point the validator.
/// </remarks>
internal static class EmbeddedSchemas
{
    private const string BaseUri = "einvoicing-schema:///";

    private static readonly ConcurrentDictionary<string, string?> Resources = new(StringComparer.Ordinal);

    public static XmlSchemaSet Load(string prefix)
    {
        var set = new XmlSchemaSet { XmlResolver = new Resolver() };
        Assembly assembly = typeof(EmbeddedSchemas).Assembly;

        foreach (string name in assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith(prefix, StringComparison.Ordinal))
            .Where(name => name.EndsWith(".xsd", StringComparison.OrdinalIgnoreCase))
            .Order(StringComparer.Ordinal))
        {
            using Stream stream = assembly.GetManifestResourceStream(name)!;

            // DTD parsing is allowed for these files and nowhere else: the W3C's xmldsig schema, which UBL
            // imports for cac:Signature, carries an internal subset of entity declarations. They are embedded
            // rather than arriving from anywhere, and the resolver is null. Documents being *validated* go
            // through SecureXml, which refuses DTDs outright.
            using XmlReader reader = XmlReader.Create(
                stream,
                new XmlReaderSettings { XmlResolver = null, DtdProcessing = DtdProcessing.Parse },
                BaseUri + Path.GetFileName(name));

            set.Add(null, reader);
        }

        set.Compile();
        return set;
    }

    private sealed class Resolver : XmlResolver
    {
        public override object? GetEntity(Uri absoluteUri, string? role, Type? typeOfObjectToReturn)
        {
            ArgumentNullException.ThrowIfNull(absoluteUri);

            string? resource = Resources.GetOrAdd(Path.GetFileName(absoluteUri.AbsolutePath), Find);

            return resource is null ? null : typeof(EmbeddedSchemas).Assembly.GetManifestResourceStream(resource);
        }

        public override Uri ResolveUri(Uri? baseUri, string? relativeUri) =>
            new(BaseUri + Path.GetFileName(relativeUri ?? string.Empty), UriKind.Absolute);

        private static string? Find(string file) =>
            typeof(EmbeddedSchemas).Assembly.GetManifestResourceNames()
                .FirstOrDefault(name => name.EndsWith(file, StringComparison.OrdinalIgnoreCase));
    }
}
