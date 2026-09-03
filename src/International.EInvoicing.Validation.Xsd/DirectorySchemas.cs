using System.Xml;
using System.Xml.Schema;

namespace International.EInvoicing.Validation.Xsd;

/// <summary>
/// Loads a set of schemas out of a directory, and resolves their imports to each other.
/// </summary>
/// <remarks>
/// The counterpart of <see cref="EmbeddedSchemas"/> for schemas this library may not redistribute. Nothing
/// is fetched: every <c>xs:import</c> resolves to a file in the same directory, and anything else is
/// refused, so a schema pointing outward has nowhere to point.
/// </remarks>
internal static class DirectorySchemas
{
    public static XmlSchemaSet Load(string directory)
    {
        var set = new XmlSchemaSet { XmlResolver = new Resolver(directory) };

        foreach (string path in Directory.EnumerateFiles(directory, "*.xsd").Order(StringComparer.Ordinal))
        {
            using FileStream stream = File.OpenRead(path);
            using XmlReader reader = XmlReader.Create(
                stream,
                new XmlReaderSettings { XmlResolver = null, DtdProcessing = DtdProcessing.Prohibit },
                new Uri(path).AbsoluteUri);

            set.Add(null, reader);
        }

        set.Compile();
        return set;
    }

    private sealed class Resolver(string directory) : XmlResolver
    {
        public override object? GetEntity(Uri absoluteUri, string? role, Type? typeOfObjectToReturn)
        {
            ArgumentNullException.ThrowIfNull(absoluteUri);

            string path = Path.Combine(directory, Path.GetFileName(absoluteUri.AbsolutePath));
            return File.Exists(path) ? File.OpenRead(path) : null;
        }

        public override Uri ResolveUri(Uri? baseUri, string? relativeUri) =>
            new(Path.Combine(directory, Path.GetFileName(relativeUri ?? string.Empty)));
    }
}
