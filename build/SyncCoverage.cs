#:property PublishAot=false

using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

// Renders docs/coverage.json into the coverage table of README.md.
//   dotnet run build/SyncCoverage.cs            rewrites README.md
//   dotnet run build/SyncCoverage.cs -- --check  fails when README.md is stale (CI gate)

const string StartMarker = "<!-- coverage:start -->";
const string EndMarker = "<!-- coverage:end -->";

string root = FindRepositoryRoot();
string coveragePath = Path.Combine(root, "docs", "coverage.json");
string readmePath = Path.Combine(root, "README.md");
bool checkOnly = args.Contains("--check");

CoverageDocument coverage = JsonSerializer.Deserialize<CoverageDocument>(
    File.ReadAllText(coveragePath), Json.Options)
    ?? throw new InvalidOperationException($"{coveragePath} is empty or malformed.");

string readme = File.ReadAllText(readmePath);
string updated = ReplaceBetweenMarkers(readme, Render(coverage));

if (readme == updated)
{
    Console.WriteLine("README.md coverage table is up to date.");
    return 0;
}

if (checkOnly)
{
    Console.Error.WriteLine("README.md coverage table is stale. Run: dotnet run build/SyncCoverage.cs");
    return 1;
}

File.WriteAllText(readmePath, updated);
Console.WriteLine("README.md coverage table updated.");
return 0;

static string FindRepositoryRoot([CallerFilePath] string scriptPath = "")
{
    var directory = new DirectoryInfo(Path.GetDirectoryName(scriptPath)!);
    while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, ".git")))
    {
        directory = directory.Parent;
    }

    return directory?.FullName
        ?? throw new InvalidOperationException("Repository root not found: no .git directory above the script.");
}

static string ReplaceBetweenMarkers(string content, string replacement)
{
    int start = content.IndexOf(StartMarker, StringComparison.Ordinal);
    int end = content.IndexOf(EndMarker, StringComparison.Ordinal);
    if (start < 0 || end < 0 || end < start)
    {
        throw new InvalidOperationException($"README.md must contain {StartMarker} and {EndMarker}.");
    }

    return content[..(start + StartMarker.Length)] + Environment.NewLine
        + replacement
        + content[end..];
}

static string Render(CoverageDocument coverage)
{
    var builder = new StringBuilder();

    foreach (CoverageSection section in coverage.Sections)
    {
        builder.AppendLine($"### {section.Title}").AppendLine();
        builder.AppendLine("| | Read | Write | Validate | Package |");
        builder.AppendLine("|---|---|---|---|---|");

        foreach (CoverageEntry entry in section.Entries)
        {
            string name = entry.Version is null or "—" ? entry.Name : $"{entry.Name} <sub>{entry.Version}</sub>";
            builder.AppendLine(
                $"| {name} | {Label(coverage, entry.Read)} | {Label(coverage, entry.Write)} " +
                $"| {Label(coverage, entry.Validate)} | {Package(entry.Package)} |");
        }

        builder.AppendLine();

        foreach (CoverageEntry entry in section.Entries.Where(e => e.Notes is not null))
        {
            builder.AppendLine($"> **{entry.Name}** — {entry.Notes}").AppendLine();
        }
    }

    builder.AppendLine("**Legend** — " + string.Join(" · ", coverage.Statuses.Values));
    builder.AppendLine();

    return builder.ToString();
}

static string Label(CoverageDocument coverage, string status) =>
    coverage.Statuses.TryGetValue(status, out string? label)
        ? label.Split(' ')[0]
        : throw new InvalidOperationException($"Unknown status '{status}' in docs/coverage.json.");

static string Package(string? package) =>
    package is null or "—" ? "—" : $"`{package}`";

internal static class Json
{
    public static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };
}

internal sealed record CoverageDocument(Dictionary<string, string> Statuses, List<CoverageSection> Sections);

internal sealed record CoverageSection(string Title, List<CoverageEntry> Entries);

internal sealed record CoverageEntry(
    string Name,
    string? Version,
    string? Package,
    string Read,
    string Write,
    string Validate,
    string? Notes);
