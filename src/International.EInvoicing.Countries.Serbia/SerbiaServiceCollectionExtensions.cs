using International.EInvoicing.Configuration;
using International.EInvoicing.Profiles;
using International.EInvoicing.Ubl;
using International.EInvoicing.Validation.Schematron;

namespace International.EInvoicing.Countries.Serbia;

/// <summary>Registers what Serbia needs.</summary>
public static class SerbiaServiceCollectionExtensions
{
    /// <summary>Adds the SRBDT profile.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
    public static EInvoicingBuilder AddSerbia(this EInvoicingBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.AddUbl().AddProfiles(RsProfiles.All);
    }

    /// <summary>
    /// Adds the SRBDT rules found in a directory of fetched artefacts.
    /// </summary>
    /// <remarks>
    /// Published as pre-compiled XSLT, which this library reads as data, and not redistributable — so they
    /// are fetched: <c>build/fetch-specs.sh national</c> writes them to
    /// <c>specs/national/serbia/schematron</c>.
    /// </remarks>
    /// <param name="builder">The library being assembled.</param>
    /// <param name="directory">The <c>schematron</c> directory the fetch script writes.</param>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException"><paramref name="directory"/> is empty.</exception>
    /// <exception cref="DirectoryNotFoundException">The directory does not exist.</exception>
    public static EInvoicingBuilder AddSerbianRulesFrom(this EInvoicingBuilder builder, string directory)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);

        if (!Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException(
                $"No SRBDT rule sets at '{directory}'. They are not redistributable, so this library does "
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

        string identifier = RsProfiles.SrbdtUbl.Id.Value;

        // Serbia publishes one artefact covering the CIUS and its extension.
        foreach (string path in Directory.EnumerateFiles(newest, "*.xslt").Order(StringComparer.Ordinal))
        {
            builder.AddRules(
                DocumentSyntax.Ubl,
                CompiledSchematron.Read(
                    File.ReadAllText(path),
                    $"SRBDT ({Path.GetFileNameWithoutExtension(path)})",
                    Path.GetFileName(newest)),
                // The extension's identifier starts with the CIUS one, so both are judged by these rules.
                specification => specification.Value.StartsWith(identifier, StringComparison.Ordinal));
        }

        return builder;
    }
}
