using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

// Fails when a diagnostic code emitted in src/ has no page in docs/diagnostics/.
//   dotnet run build/CheckDiagnostics.cs

string root = FindRepositoryRoot();
string sourceDirectory = Path.Combine(root, "src");
string catalogueDirectory = Path.Combine(root, "docs", "diagnostics");

var codePattern = new Regex(@"\bEIV[0-9]{4}\b", RegexOptions.Compiled);

HashSet<string> emitted = Directory
    .EnumerateFiles(sourceDirectory, "*.cs", SearchOption.AllDirectories)
    .SelectMany(file => codePattern.Matches(File.ReadAllText(file)).Select(match => match.Value))
    .ToHashSet(StringComparer.Ordinal);

HashSet<string> documented = Directory
    .EnumerateFiles(catalogueDirectory, "EIV*.md")
    .Select(Path.GetFileNameWithoutExtension)
    .Where(name => name is not null)
    .ToHashSet(StringComparer.Ordinal)!;

string[] undocumented = emitted.Except(documented, StringComparer.Ordinal).Order().ToArray();
string[] orphaned = documented.Except(emitted, StringComparer.Ordinal).Order().ToArray();

foreach (string code in orphaned)
{
    Console.WriteLine($"note: {code} is documented but not emitted yet.");
}

if (undocumented.Length == 0)
{
    Console.WriteLine($"All {emitted.Count} diagnostic code(s) emitted in src/ are documented.");
    return 0;
}

Console.Error.WriteLine("Diagnostic codes without a catalogue page in docs/diagnostics/:");
foreach (string code in undocumented)
{
    Console.Error.WriteLine($"  {code}  ->  docs/diagnostics/{code}.md");
}

return 1;

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
