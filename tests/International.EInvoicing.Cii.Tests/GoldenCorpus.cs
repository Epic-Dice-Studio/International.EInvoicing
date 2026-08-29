namespace International.EInvoicing.Cii.Tests;

/// <summary>
/// The official CII documents this library is tested against, from the KoSIT XRechnung test suite under
/// <c>specs/</c>. The repository is located from the test binary, not from a compile-time path: CI builds
/// deterministically, which rewrites source paths.
/// </summary>
internal static class GoldenCorpus
{
    private static readonly string CorpusDirectory = Path.Combine(
        RepositoryRoot(),
        "specs",
        "xrechnung",
        "testsuite",
        "src",
        "test");

    public static IReadOnlyList<string> CiiInvoicePaths { get; } =
    [
        .. Directory
            .EnumerateFiles(CorpusDirectory, "*_uncefact.xml", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.Ordinal),
    ];

    public static IEnumerable<object[]> CiiInvoiceCases =>
        CiiInvoicePaths.Select(path => new object[] { Path.GetFileName(path) });

    public static string Read(string fileName) =>
        File.ReadAllText(CiiInvoicePaths.Single(path => Path.GetFileName(path) == fileName));

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, ".git")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException(
            $"Repository root not found above {AppContext.BaseDirectory}: the golden corpus lives in the "
            + "working tree, so these tests must run from a checkout.");
    }
}
