using International.EInvoicing.Configuration;
using International.EInvoicing.Profiles;
using International.EInvoicing.Validation.Schematron;

namespace International.EInvoicing.Countries.Netherlands;

/// <summary>
/// The NLCIUS rule sets, loaded from artefacts a caller fetched.
/// </summary>
/// <remarks>
/// The Dutch rules are published as pre-compiled XSLT, which this library reads as data — see
/// <c>docs/standards/peppol-pint.md</c> for how, and why that is not the same as rewriting them.
/// <c>build/fetch-specs.sh national</c> puts them on disk. They are not redistributable, so they are not
/// shipped.
/// </remarks>
public static class NlRules
{
    /// <summary>
    /// Adds the NLCIUS rules found in a directory of fetched artefacts.
    /// </summary>
    /// <param name="builder">The library being assembled.</param>
    /// <param name="directory">
    /// The <c>simplerinvoicing</c> directory the fetch script writes, or any copy of it.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException"><paramref name="directory"/> is empty.</exception>
    /// <exception cref="DirectoryNotFoundException">The directory does not exist.</exception>
    public static EInvoicingBuilder AddNlciusRulesFrom(this EInvoicingBuilder builder, string directory)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);

        if (!Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException(
                $"No NLCIUS rule sets at '{directory}'. They are not redistributable, so this library does "
                + "not ship them: run build/fetch-specs.sh national, or point this at your own copy.");
        }

        AddNewest(builder, directory, "simplerinvoicing", "si-ubl-2.0", DocumentSyntax.Ubl);
        AddNewest(builder, directory, "nlcius", "nlcius-cii-1.0", DocumentSyntax.Cii);

        return builder;
    }

    /// <summary>
    /// The newest artefact matching a prefix. The publisher keeps every past revision beside the current
    /// one, and an older revision is history rather than a second opinion.
    /// </summary>
    private static void AddNewest(
        EInvoicingBuilder builder,
        string directory,
        string folder,
        string prefix,
        DocumentSyntax syntax)
    {
        string root = Path.Combine(directory, folder);

        if (!Directory.Exists(root))
        {
            return;
        }

        string? newest = Directory
            .EnumerateFiles(root, $"{prefix}*.xslt")
            .OrderBy(path => Revision(Path.GetFileNameWithoutExtension(path), prefix))
            .LastOrDefault();

        if (newest is null)
        {
            return;
        }

        string identifier = NlProfiles.NlciusUbl.Id.Value;

        builder.AddRules(
            syntax,
            CompiledSchematron.Read(
                File.ReadAllText(newest),
                $"NLCIUS ({syntax})",
                Path.GetFileNameWithoutExtension(newest)[prefix.Length..].TrimStart('.')),
            specification => specification.Value.StartsWith(identifier, StringComparison.Ordinal));
    }

    /// <summary>The trailing revision number, ordered as a number rather than as text.</summary>
    private static int Revision(string fileName, string prefix)
    {
        string tail = fileName[prefix.Length..].TrimStart('.');

        return int.TryParse(tail, out int revision) ? revision : 0;
    }
}
