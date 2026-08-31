using International.EInvoicing.Configuration;
using International.EInvoicing.Profiles;
using International.EInvoicing.Ubl;
using International.EInvoicing.Validation.Schematron;

namespace International.EInvoicing.Countries.Romania;

/// <summary>Registers what Romania needs.</summary>
public static class RomaniaServiceCollectionExtensions
{
    /// <summary>Adds the CIUS-RO profile.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
    public static EInvoicingBuilder AddRomania(this EInvoicingBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.AddUbl().AddProfiles(RoProfiles.All);
    }

    /// <summary>
    /// Adds the CIUS-RO rules found in a directory of fetched artefacts.
    /// </summary>
    /// <remarks>
    /// Published as pre-compiled XSLT, which this library reads as data, and not redistributable — so they
    /// are fetched: <c>build/fetch-specs.sh national</c> writes them to
    /// <c>specs/national/cius-ro/schematron</c>.
    /// </remarks>
    /// <param name="builder">The library being assembled.</param>
    /// <param name="directory">The <c>schematron</c> directory the fetch script writes.</param>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException"><paramref name="directory"/> is empty.</exception>
    /// <exception cref="DirectoryNotFoundException">The directory does not exist.</exception>
    public static EInvoicingBuilder AddRomanianRulesFrom(this EInvoicingBuilder builder, string directory)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);

        if (!Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException(
                $"No CIUS-RO rule sets at '{directory}'. They are not redistributable, so this library does "
                + "not ship them: run build/fetch-specs.sh national, or point this at your own copy.");
        }

        string? newest = Directory
            .EnumerateDirectories(directory)
            .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .LastOrDefault();

        if (newest is null)
        {
            return builder;
        }

        string identifier = RoProfiles.CiusRoUbl.Id.Value;

        // The folder holds the invoice rules and a credit-note set beside them; both judge CIUS-RO.
        foreach (string path in Directory.EnumerateFiles(newest, "*.xslt").Order(StringComparer.Ordinal))
        {
            builder.AddRules(
                DocumentSyntax.Ubl,
                CompiledSchematron.Read(
                    File.ReadAllText(path),
                    $"CIUS-RO ({Path.GetFileNameWithoutExtension(path)})",
                    Path.GetFileName(newest)),
                specification => string.Equals(specification.Value, identifier, StringComparison.Ordinal));
        }

        return builder;
    }
}
