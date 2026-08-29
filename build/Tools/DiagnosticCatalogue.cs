using System.Text.RegularExpressions;

namespace International.EInvoicing.BuildTools;

/// Fails when a diagnostic code emitted in src/ has no page in docs/diagnostics/.
internal static partial class DiagnosticCatalogue
{
    public static int Run(string repositoryRoot)
    {
        string sourceDirectory = Path.Combine(repositoryRoot, "src");
        string catalogueDirectory = Path.Combine(repositoryRoot, "docs", "diagnostics");

        HashSet<string> emitted = Directory
            .EnumerateFiles(sourceDirectory, "*.cs", SearchOption.AllDirectories)
            .SelectMany(file => CodePattern().Matches(File.ReadAllText(file)).Select(match => match.Value))
            .ToHashSet(StringComparer.Ordinal);

        HashSet<string> documented = Directory
            .EnumerateFiles(catalogueDirectory, "EIV*.md")
            .Select(path => Path.GetFileNameWithoutExtension(path))
            .ToHashSet(StringComparer.Ordinal);

        foreach (string code in documented.Except(emitted, StringComparer.Ordinal).Order())
        {
            Console.WriteLine($"note: {code} is documented but not emitted yet.");
        }

        string[] undocumented = emitted.Except(documented, StringComparer.Ordinal).Order().ToArray();
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
    }

    [GeneratedRegex(@"\bEIV[0-9]{4}\b")]
    private static partial Regex CodePattern();
}
