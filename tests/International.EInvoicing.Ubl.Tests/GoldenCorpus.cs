namespace International.EInvoicing.Ubl.Tests;

/// <summary>
/// The official UBL documents this library is tested against. They come from the KoSIT XRechnung test suite
/// under <c>specs/</c>: files we invented would only prove we agree with ourselves.
/// </summary>
/// <remarks>
/// The repository is located by walking up from the test binary, not from <c>[CallerFilePath]</c>: CI builds
/// with <c>ContinuousIntegrationBuild</c>, which maps source paths to <c>/_/</c> for determinism, so a
/// compile-time path is not a real path there.
/// </remarks>
internal static class GoldenCorpus
{
    private static readonly string CorpusDirectory = Path.Combine(
        RepositoryRoot(),
        "specs",
        "xrechnung",
        "testsuite",
        "src",
        "test");

    public static IReadOnlyList<string> UblInvoicePaths { get; } =
    [
        .. Directory
            .EnumerateFiles(CorpusDirectory, "*_ubl.xml", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.Ordinal),
    ];

    public static IEnumerable<object[]> UblInvoiceCases =>
        UblInvoicePaths.Select(path => new object[] { Path.GetFileName(path) });

    public static string Read(string fileName) =>
        File.ReadAllText(UblInvoicePaths.Single(path => Path.GetFileName(path) == fileName));

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
