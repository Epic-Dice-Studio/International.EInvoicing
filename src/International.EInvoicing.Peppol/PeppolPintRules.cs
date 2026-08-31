using System.Collections.Concurrent;
using International.EInvoicing.Profiles;
using International.EInvoicing.Validation.Schematron;

namespace International.EInvoicing.Peppol;

/// <summary>
/// The Peppol PINT rule sets, loaded from artefacts a caller fetched.
/// </summary>
/// <remarks>
/// <para>
/// PINT validates in two layers, and both apply: the <b>base</b> rules every jurisdiction shares, and the
/// <b>jurisdiction</b> rules its own specialisation adds. Running only the first gives a false pass, which is
/// why <see cref="For"/> returns both.
/// </para>
/// <para>
/// OpenPEPPOL publishes them under no licence that permits redistribution, and as pre-compiled XSLT rather
/// than source Schematron. Neither stops them running: <c>build/fetch-specs.sh pint</c> puts them on disk,
/// and <see cref="CompiledSchematron"/> reads the compiled form. See
/// <c>docs/standards/peppol-pint.md</c>.
/// </para>
/// </remarks>
public static class PeppolPintRules
{
    private static readonly ConcurrentDictionary<string, SchematronRuleSet> Loaded = new(StringComparer.Ordinal);

    /// <summary>The folder each jurisdiction's artefacts live in, as the publisher names them.</summary>
    public static IReadOnlyDictionary<string, string> Folders { get; } =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [PeppolPintProfiles.BillingAe.Id.Value] = "pint-ae",
            [PeppolPintProfiles.SelfBillingAe.Id.Value] = "pint-ae",
            [PeppolPintProfiles.BillingAuNz.Id.Value] = "pint-aunz",
            [PeppolPintProfiles.BillingEu.Id.Value] = "pint-eu",
            [PeppolPintProfiles.BillingJp.Id.Value] = "pint-jp",
            [PeppolPintProfiles.BillingMy.Id.Value] = "pint-my",
            [PeppolPintProfiles.BillingOm.Id.Value] = "pint-om",
            [PeppolPintProfiles.BillingSg.Id.Value] = "pint-sg",
        };

    /// <summary>
    /// Every PINT rule set that applies to a profile, given a directory of fetched artefacts.
    /// </summary>
    /// <remarks>
    /// The directory is the one <c>build/fetch-specs.sh pint</c> writes — <c>specs/peppol/pint/schematron</c>
    /// — or any copy of it. The newest version present for the jurisdiction is used, since the publisher
    /// keeps older ones beside it.
    /// </remarks>
    /// <param name="profile">The profile a document declares.</param>
    /// <param name="directory">Where the artefacts are.</param>
    /// <returns>The rule sets, base first. Empty when nothing for that profile is present.</returns>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    public static IReadOnlyList<SchematronRuleSet> For(Profile profile, string directory)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(directory);

        if (!Folders.TryGetValue(profile.Id.Value, out string? folder))
        {
            return [];
        }

        string root = Path.Combine(directory, folder);

        if (!Directory.Exists(root))
        {
            return [];
        }

        List<SchematronRuleSet> rules = [];

        foreach (string path in NewestOf(root))
        {
            rules.Add(Loaded.GetOrAdd(
                path,
                key => CompiledSchematron.Read(
                    File.ReadAllText(key),
                    $"{profile.Name} — {Layer(key)}",
                    Version(key))));
        }

        return rules;
    }

    /// <summary>
    /// The artefacts of the newest version present, base rules first.
    /// </summary>
    /// <remarks>
    /// A jurisdiction folder holds one directory per published version, and some of them hold a further
    /// directory per document kind (billing, self-billing). Both shapes are walked, and only the newest
    /// version is loaded — an older one beside it is history, not a second opinion.
    /// </remarks>
    private static IEnumerable<string> NewestOf(string root)
    {
        string? newest = Directory
            .EnumerateDirectories(root)
            .OrderBy(path => Path.GetFileName(path), VersionOrder.Instance)
            .LastOrDefault();

        if (newest is null)
        {
            return [];
        }

        return Directory
            .EnumerateFiles(newest, "*.xslt", SearchOption.AllDirectories)
            .Where(path => !path.Contains("selfbilling", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path.Contains("jurisdiction", StringComparison.Ordinal))
            .ThenBy(path => path, StringComparer.Ordinal);
    }

    private static string Layer(string path) =>
        path.Contains("jurisdiction", StringComparison.Ordinal) ? "jurisdiction rules" : "base rules";

    private static string Version(string path)
    {
        for (DirectoryInfo? directory = new FileInfo(path).Directory;
            directory is not null;
            directory = directory.Parent)
        {
            if (directory.Name.Length > 0 && char.IsAsciiDigit(directory.Name[0]))
            {
                return directory.Name;
            }
        }

        return "unknown";
    }

    /// <summary>Orders version folders the way versions order, not the way strings do.</summary>
    private sealed class VersionOrder : IComparer<string>
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
