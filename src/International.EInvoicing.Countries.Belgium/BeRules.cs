using International.EInvoicing.Configuration;
using International.EInvoicing.Profiles;
using International.EInvoicing.Validation.Schematron;

namespace International.EInvoicing.Countries.Belgium;

/// <summary>
/// The Belgian rule set, loaded from artefacts a caller fetched.
/// </summary>
/// <remarks>
/// Belgium publishes <b>GLOBALUBL.BE</b>, which bundles the EN 16931 UBL rules with its own on top — so it
/// judges a Peppol BIS document whole. It is not redistributable: <c>build/fetch-specs.sh national</c>
/// writes it to <c>specs/national/ublbe/schematron</c>. Published as compiled XSLT, which this library
/// reads as data.
/// </remarks>
public static class BeRules
{
    /// <summary>
    /// Adds the Belgian rule set found in a directory of fetched artefacts.
    /// </summary>
    /// <param name="builder">The library being assembled.</param>
    /// <param name="directory">The <c>schematron</c> directory the fetch script writes.</param>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException"><paramref name="directory"/> is empty.</exception>
    /// <exception cref="DirectoryNotFoundException">The directory does not exist.</exception>
    public static EInvoicingBuilder AddBelgianRulesFrom(this EInvoicingBuilder builder, string directory)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);

        if (!Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException(
                $"No Belgian rule set at '{directory}'. It is not redistributable, so this library does not "
                + "ship it: run build/fetch-specs.sh national, or point this at your own copy.");
        }

        string? newest = Directory
            .EnumerateFiles(directory, "GLOBALUBL.BE*.xslt", SearchOption.AllDirectories)
            .OrderBy(path => Path.GetFileName(Path.GetDirectoryName(path)), VersionOrder.Instance)
            .LastOrDefault();

        if (newest is null)
        {
            return builder;
        }

        string identifier = BeProfiles.UblBe.Id.Value;

        return builder.AddRules(
            DocumentSyntax.Ubl,
            CompiledSchematron.Read(
                File.ReadAllText(newest),
                "GLOBALUBL.BE",
                Path.GetFileName(Path.GetDirectoryName(newest)) ?? "unknown"),
            specification => string.Equals(specification.Value, identifier, StringComparison.Ordinal),
            // GLOBALUBL.BE bundles the EN 16931 rules — 94% of them by identifier — and adapts several.
            // Registering the originals alongside re-imposes exactly what Belgium relaxed.
            supersedesBaseline: true);
    }

    /// <summary>Orders version folders — <c>v1.2.8</c> after <c>v1.2.7</c>, which text ordering gets right
    /// only by luck.</summary>
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
                .TrimStart('v')
                .Split('.')
                .Select(part => int.TryParse(part, out int value) ? value : 0),
        ];
    }
}
