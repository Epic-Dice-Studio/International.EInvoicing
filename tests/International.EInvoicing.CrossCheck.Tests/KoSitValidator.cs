using System.Diagnostics;
using System.Xml.Linq;

namespace International.EInvoicing.CrossCheck.Tests;

/// <summary>
/// Runs the KoSIT validator — the reference implementation German authorities actually run — and reads back
/// what it said about each document.
/// </summary>
/// <remarks>
/// <para>
/// One process for the whole corpus rather than one per document: the JVM takes longer to start than the
/// validator takes to run, and eighty-six starts would make this too slow to keep.
/// </para>
/// <para>
/// Everything here is optional. No Java, no jar, no configuration, no corpus — the tests skip. The point is
/// a check CI can run, not a dependency the build acquires.
/// </para>
/// </remarks>
internal static class KoSitValidator
{
    private static readonly XNamespace Report = "http://www.xoev.de/de/validator/varl/1";

    /// <summary>Whether everything this needs is present.</summary>
    public static bool IsAvailable => Java() is not null && Jar() is not null && Scenarios() is not null;

    /// <summary>Why it is not, so a skip says something useful.</summary>
    public static string WhyNot =>
        Java() is null
            ? "no Java on PATH or at JAVA_HOME — the KoSIT validator is a JVM program"
            : "run build/fetch-specs.sh kosit";

    /// <summary>
    /// Validates every document and answers, for each, the rule codes KoSIT fired and whether it accepted.
    /// </summary>
    /// <exception cref="InvalidOperationException">The validator could not be run at all.</exception>
    public static IReadOnlyDictionary<string, KoSitVerdict> Validate(IEnumerable<string> documents)
    {
        string output = Directory.CreateTempSubdirectory("kosit-").FullName;

        try
        {
            var arguments = new List<string>
            {
                "-jar", Jar()!,
                "-s", Scenarios()!,
                "-r", Path.GetDirectoryName(Scenarios())!,
                "-o", output,
            };

            arguments.AddRange(documents);

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo(Java()!)
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                },
            };

            foreach (string argument in arguments)
            {
                process.StartInfo.ArgumentList.Add(argument);
            }

            process.Start();
            string stdout = process.StandardOutput.ReadToEnd();
            string stderr = process.StandardError.ReadToEnd();
            process.WaitForExit();

            Dictionary<string, KoSitVerdict> verdicts = [];

            foreach (string report in Directory.EnumerateFiles(output, "*-report.xml"))
            {
                (string Name, KoSitVerdict Verdict) read = ReadReport(report);
                verdicts[read.Name] = read.Verdict;
            }

            return verdicts.Count > 0
                ? verdicts
                : throw new InvalidOperationException(
                    $"The KoSIT validator produced no reports.{Environment.NewLine}{stdout}{stderr}");
        }
        finally
        {
            Directory.Delete(output, recursive: true);
        }
    }

    private static (string Name, KoSitVerdict Verdict) ReadReport(string path)
    {
        XElement root = XDocument.Load(path).Root!;

        string name = Path.GetFileName(path)[..^"-report.xml".Length];
        bool accepted = root.Descendants(Report + "assessment").Elements(Report + "accept").Any();

        // Every rule the validator fired, at whatever level: an information-level rule it fires and this
        // library does not is the same kind of disagreement as a rejection, only quieter.
        List<XElement> messages = [.. root.Descendants(Report + "message")];

        HashSet<string> fired =
        [
            .. messages
                .Select(message => message.Attribute("code")?.Value)
                .Where(code => !string.IsNullOrEmpty(code))
                .Select(code => code!),
        ];

        HashSet<string> errors =
        [
            .. messages
                .Where(message => message.Attribute("level")?.Value == "error")
                .Select(message => message.Attribute("code")?.Value)
                .Where(code => !string.IsNullOrEmpty(code))
                .Select(code => code!),
        ];

        return (name, new KoSitVerdict(accepted, fired, errors));
    }

    private static string? Java()
    {
        if (Environment.GetEnvironmentVariable("JAVA_HOME") is { Length: > 0 } home)
        {
            string candidate = Path.Combine(home, "bin", OperatingSystem.IsWindows() ? "java.exe" : "java");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        string executable = OperatingSystem.IsWindows() ? "java.exe" : "java";

        return (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
            .Split(Path.PathSeparator)
            .Select(directory => Path.Combine(directory, executable))
            .FirstOrDefault(File.Exists);
    }

    private static string? Jar() =>
        Directory.Exists(Root())
            ? Directory.EnumerateFiles(Root(), "validator-*-standalone.jar").Order(StringComparer.Ordinal)
                .FirstOrDefault()
            : null;

    private static string? Scenarios()
    {
        string path = Path.Combine(Root(), "configuration", "scenarios.xml");
        return File.Exists(path) ? path : null;
    }

    private static string Root() => Path.Combine(Corpus.RepositoryRoot(), "specs", "kosit");
}

/// <summary>
/// What the KoSIT validator said about one document.
/// </summary>
/// <remarks>
/// <see cref="Accepted"/> is not "found nothing wrong". Acceptance is decided by the scenario's own
/// <c>acceptMatch</c>, and the XRechnung scenarios accept a document that broke EN 16931 rules — this
/// library's <c>IsValid</c> asks a different question, so the two are not comparable and only
/// <see cref="Errors"/> and <see cref="Fired"/> are.
/// </remarks>
/// <param name="Accepted">Whether the scenario's acceptance rule matched.</param>
/// <param name="Fired">Every rule it reported, at any level.</param>
/// <param name="Errors">Those it reported at error level.</param>
internal sealed record KoSitVerdict(bool Accepted, IReadOnlySet<string> Fired, IReadOnlySet<string> Errors);
