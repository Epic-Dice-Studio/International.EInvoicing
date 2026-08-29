using System.Globalization;
using System.Text;
using System.Text.Json;

namespace International.EInvoicing.BuildTools;

/// Renders docs/coverage.json into the support matrix of README.md.
internal static class CoverageTable
{
    private const string StartMarker = "<!-- coverage:start -->";
    private const string EndMarker = "<!-- coverage:end -->";

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static int Run(string repositoryRoot, bool checkOnly)
    {
        string coveragePath = Path.Combine(repositoryRoot, "docs", "coverage.json");
        string readmePath = Path.Combine(repositoryRoot, "README.md");

        CoverageDocument coverage =
            JsonSerializer.Deserialize<CoverageDocument>(File.ReadAllText(coveragePath), JsonOptions)
            ?? throw new InvalidOperationException($"{coveragePath} is empty or malformed.");

        string readme = File.ReadAllText(readmePath);
        string updated = ReplaceBetweenMarkers(readme, Render(coverage));

        if (readme == updated)
        {
            Console.WriteLine("README.md support matrix is up to date.");
            return 0;
        }

        if (checkOnly)
        {
            Console.Error.WriteLine(
                "README.md support matrix is stale. Run: dotnet run --project build/Tools -- coverage");
            return 1;
        }

        File.WriteAllText(readmePath, updated);
        Console.WriteLine("README.md support matrix updated.");
        return 0;
    }

    private static string ReplaceBetweenMarkers(string content, string replacement)
    {
        int start = content.IndexOf(StartMarker, StringComparison.Ordinal);
        int end = content.IndexOf(EndMarker, StringComparison.Ordinal);
        if (start < 0 || end < 0 || end < start)
        {
            throw new InvalidOperationException($"README.md must contain {StartMarker} and {EndMarker}.");
        }

        return content[..(start + StartMarker.Length)] + "\n" + replacement + content[end..];
    }

    private static string Render(CoverageDocument coverage)
    {
        var builder = new StringBuilder();

        foreach (CoverageSection section in coverage.Sections)
        {
            builder.Append(CultureInfo.InvariantCulture, $"### {section.Title}\n\n");
            builder.Append("| | Read | Write | Validate | Package |\n");
            builder.Append("|---|---|---|---|---|\n");

            foreach (CoverageEntry entry in section.Entries)
            {
                string name = entry.Version is null or "—"
                    ? entry.Name
                    : $"{entry.Name} <sub>{entry.Version}</sub>";

                builder.Append(CultureInfo.InvariantCulture, $"| {name} ")
                    .Append(CultureInfo.InvariantCulture, $"| {Icon(coverage, entry.Read)} ")
                    .Append(CultureInfo.InvariantCulture, $"| {Icon(coverage, entry.Write)} ")
                    .Append(CultureInfo.InvariantCulture, $"| {Icon(coverage, entry.Validate)} ")
                    .Append(CultureInfo.InvariantCulture, $"| {Package(entry.Package)} |\n");
            }

            builder.Append('\n');

            foreach (CoverageEntry entry in section.Entries.Where(e => e.Notes is not null))
            {
                builder.Append(CultureInfo.InvariantCulture, $"> **{entry.Name}** — {entry.Notes}\n\n");
            }
        }

        builder.Append(CultureInfo.InvariantCulture, $"**Legend** — {string.Join(" · ", coverage.Statuses.Values)}\n\n");

        return builder.ToString();
    }

    private static string Icon(CoverageDocument coverage, string status) =>
        coverage.Statuses.TryGetValue(status, out string? label)
            ? label.Split(' ')[0]
            : throw new InvalidOperationException($"Unknown status '{status}' in docs/coverage.json.");

    private static string Package(string? package) =>
        package is null or "—" ? "—" : $"`{package}`";

    private sealed record CoverageDocument(
        Dictionary<string, string> Statuses,
        List<CoverageSection> Sections);

    private sealed record CoverageSection(string Title, List<CoverageEntry> Entries);

    private sealed record CoverageEntry(
        string Name,
        string? Version,
        string? Package,
        string Read,
        string Write,
        string Validate,
        string? Notes);
}
