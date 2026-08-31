using International.EInvoicing.Configuration;
using International.EInvoicing.Profiles;
using International.EInvoicing.Validation.Schematron;

namespace International.EInvoicing.FacturX;

/// <summary>
/// The Factur-X rule sets, loaded from artefacts a caller fetched.
/// </summary>
/// <remarks>
/// <para>
/// Factur-X publishes one rule set per profile, and they differ in kind rather than degree: MINIMUM and
/// BASIC WL are not EN 16931 invoices at all, so the EN 16931 rules do not judge them
/// (<c>AddEn16931Rules</c> knows that) and these do.
/// </para>
/// <para>
/// The artefacts are not redistributable, so they are fetched: <c>build/fetch-specs.sh national</c> writes
/// them to <c>specs/national/zugferd/schematron</c>. They are published as pre-compiled XSLT, which this
/// library reads as data — see <c>docs/standards/peppol-pint.md</c>.
/// </para>
/// </remarks>
public static class FacturXRules
{
    /// <summary>The artefact file name for each profile, as the publisher names them.</summary>
    public static IReadOnlyDictionary<string, string> FileNames { get; } =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["FACTUR-X_MINIMUM"] = FacturXProfiles.Minimum.Id.Value,
            ["FACTUR-X_BASIC-WL"] = FacturXProfiles.BasicWithoutLines.Id.Value,
            ["FACTUR-X_BASIC"] = FacturXProfiles.Basic.Id.Value,
            ["FACTUR-X_EN16931"] = FacturXProfiles.En16931.Id.Value,
            ["FACTUR-X_EXTENDED"] = FacturXProfiles.Extended.Id.Value,
        };

    /// <summary>
    /// Adds the Factur-X rule sets found in a directory of fetched artefacts, one per profile.
    /// </summary>
    /// <param name="builder">The library being assembled.</param>
    /// <param name="directory">The <c>schematron</c> directory the fetch script writes.</param>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException"><paramref name="directory"/> is empty.</exception>
    /// <exception cref="DirectoryNotFoundException">The directory does not exist.</exception>
    public static EInvoicingBuilder AddFacturXRulesFrom(this EInvoicingBuilder builder, string directory)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);

        if (!Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException(
                $"No Factur-X rule sets at '{directory}'. They are not redistributable, so this library does "
                + "not ship them: run build/fetch-specs.sh national, or point this at your own copy.");
        }

        string? newest = Directory
            .EnumerateDirectories(directory)
            .OrderBy(Path.GetFileName, VersionOrder.Instance)
            .LastOrDefault();

        if (newest is null)
        {
            return builder;
        }

        foreach ((string fileName, string identifier) in FileNames)
        {
            string path = Path.Combine(newest, fileName + ".xslt");

            if (!File.Exists(path))
            {
                continue;
            }

            builder.AddRules(
                DocumentSyntax.Cii,
                CompiledSchematron.Read(
                    File.ReadAllText(path),
                    $"Factur-X {fileName["FACTUR-X_".Length..]}",
                    Path.GetFileName(newest)),
                specification => string.Equals(specification.Value, identifier, StringComparison.Ordinal));
        }

        return builder;
    }

    /// <summary>Orders version folders the way versions order, not the way strings do.</summary>
    private sealed class VersionOrder : IComparer<string?>
    {
        public static VersionOrder Instance { get; } = new();

        public int Compare(string? left, string? right)
        {
            int[] first = Parts(left);
            int[] second = Parts(right);

            for (int index = 0; index < Math.Max(first.Length, second.Length); index++)
            {
                int a = index < first.Length ? first[index] : 0;
                int b = index < second.Length ? second[index] : 0;

                if (a != b)
                {
                    return a.CompareTo(b);
                }
            }

            return string.CompareOrdinal(left, right);
        }

        private static int[] Parts(string? version) =>
        [
            .. (version ?? string.Empty)
                .Split('.')
                .Select(part => int.TryParse(part, out int value) ? value : 0),
        ];
    }
}
