using International.EInvoicing.Configuration;
using International.EInvoicing.Profiles;
using International.EInvoicing.Validation.Schematron;

namespace International.EInvoicing.OrderX;

/// <summary>Registers the Order-X rule sets, once you have them.</summary>
public static class OrderXBuilderExtensions
{
    /// <summary>
    /// Adds the Order-X rule sets found in a directory of fetched artefacts, one per profile.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Point it at the folder <c>build/fetch-specs.sh order-x</c> fills — <c>specs/order-x/schematron</c> —
    /// which holds one directory per profile. Each is registered against the profile it governs, because a
    /// profile's rules restrict the one above it: judging a BASIC document by the COMFORT rules reports
    /// failures that are not in it.
    /// </para>
    /// <para>
    /// Fetched rather than shipped, for the same reason the schemas are: FNFE-MPE and FeRD publish Order-X
    /// behind a registration and permit no redistribution.
    /// </para>
    /// </remarks>
    /// <param name="builder">The library being assembled.</param>
    /// <param name="directory">The <c>schematron</c> directory the fetch script writes.</param>
    /// <param name="version">The Order-X release the files came from, so a report can be reproduced later.</param>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException"><paramref name="directory"/> is empty.</exception>
    /// <exception cref="DirectoryNotFoundException">There is no such directory.</exception>
    /// <exception cref="FileNotFoundException">It holds none of the three rule sets.</exception>
    public static EInvoicingBuilder AddOrderXRulesFrom(
        this EInvoicingBuilder builder,
        string directory,
        string version = "1.0")
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);

        if (!Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException(
                $"No Order-X rule sets at '{directory}'. FNFE-MPE and FeRD publish them behind a "
                + "registration, so this library does not ship them: run build/fetch-specs.sh order-x, or "
                + "point this at your own copy.");
        }

        var added = 0;

        foreach (Profile profile in OrderXProfiles.All)
        {
            string path = Path.Combine(directory, NameOf(profile));

            if (!Directory.Exists(path))
            {
                continue;
            }

            string identifier = profile.Id.Value;

            foreach (string file in Directory.EnumerateFiles(path, "*.sch").Order(StringComparer.Ordinal))
            {
                builder.AddRulesFromFile(
                    DocumentSyntax.OrderX,
                    file,
                    profile.Name,
                    version,
                    declared => string.Equals(declared.Value, identifier, StringComparison.Ordinal));
                added++;
            }
        }

        if (added == 0)
        {
            throw new FileNotFoundException(
                $"'{directory}' holds none of the Order-X rule sets (basic, comfort, extended). "
                + "Run build/fetch-specs.sh order-x.",
                Path.Combine(directory, "comfort"));
        }

        return builder;
    }

    /// <summary>The directory a profile's artefacts sit in, which is its name lowercased.</summary>
    private static string NameOf(Profile profile) =>
        profile.Id.Value[(profile.Id.Value.LastIndexOf(':') + 1)..];
}
