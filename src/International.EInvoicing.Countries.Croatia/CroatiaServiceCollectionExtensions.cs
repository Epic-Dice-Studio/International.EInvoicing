using International.EInvoicing.Configuration;
using International.EInvoicing.Peppol;
using International.EInvoicing.Profiles;
using International.EInvoicing.Validation.Schematron;

namespace International.EInvoicing.Countries.Croatia;

/// <summary>Registers the profiles Croatia uses.</summary>
public static class CroatiaServiceCollectionExtensions
{
    /// <summary>
    /// Adds what Croatia exchanges: Peppol BIS Billing in both syntaxes, and CIUS-HR with its extension.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
    public static EInvoicingBuilder AddCroatia(this EInvoicingBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.AddPeppol().AddProfiles(HrProfiles.All);
    }

    /// <summary>
    /// Adds the CIUS-HR rules found in a directory of fetched artefacts.
    /// </summary>
    /// <remarks>
    /// Published as pre-compiled XSLT, which this library reads as data, and not redistributable — so they
    /// are fetched: <c>build/fetch-specs.sh national</c> writes them to
    /// <c>specs/national/eracun/schematron</c>. The newest version found there is the one registered.
    /// </remarks>
    /// <param name="builder">The library being assembled.</param>
    /// <param name="directory">The <c>schematron</c> directory the fetch script writes.</param>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException"><paramref name="directory"/> is empty.</exception>
    /// <exception cref="DirectoryNotFoundException">The directory does not exist.</exception>
    public static EInvoicingBuilder AddCroatianRulesFrom(this EInvoicingBuilder builder, string directory)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);

        if (!Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException(
                $"No CIUS-HR rule sets at '{directory}'. They are not redistributable, so this library does "
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

        string identifier = HrProfiles.CiusHrUbl.Id.Value;

        foreach (string path in Directory.EnumerateFiles(newest, "*.xslt").Order(StringComparer.Ordinal))
        {
            builder.AddRules(
                DocumentSyntax.Ubl,
                CompiledSchematron.Read(
                    File.ReadAllText(path),
                    $"CIUS-HR ({Path.GetFileNameWithoutExtension(path)})",
                    Path.GetFileName(newest)),
                specification => string.Equals(specification.Value, identifier, StringComparison.Ordinal));
        }

        return builder;
    }
}
