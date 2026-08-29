using System.Text.RegularExpressions;

namespace International.EInvoicing.Validation.Schematron.Tests;

/// <summary>The published rule sets under <c>specs/</c>, which is what this engine is measured against.</summary>
internal static partial class Artefacts
{
    public static string RepositoryRoot { get; } = FindRepositoryRoot();

    public static string UblRules { get; } = Path.Combine(
        RepositoryRoot, "specs", "en16931", "ubl", "schematron", "preprocessed",
        "EN16931-UBL-validation-preprocessed.sch");

    public static string CiiRules { get; } = Path.Combine(
        RepositoryRoot, "specs", "en16931", "cii", "schematron", "preprocessed",
        "EN16931-CII-validation-preprocessed.sch");

    /// <summary>Every context and test expression the EN 16931 artefacts contain.</summary>
    public static IReadOnlyList<string> AllExpressions { get; } =
    [
        .. new[] { UblRules, CiiRules }
            .SelectMany(path => ExpressionPattern().Matches(File.ReadAllText(path)))
            .Select(match => System.Net.WebUtility.HtmlDecode(match.Groups[1].Value))
            .Where(expression => !string.IsNullOrWhiteSpace(expression)),
    ];

    [GeneratedRegex("(?:test|context)=\"([^\"]*)\"")]
    private static partial Regex ExpressionPattern();

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, ".git")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}
