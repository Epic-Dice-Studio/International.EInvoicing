using System.Runtime.CompilerServices;

namespace International.EInvoicing.Ubl.Tests;

/// <summary>
/// The official UBL documents this library is tested against. They come from the KoSIT XRechnung test suite
/// under <c>specs/</c>: files we invented would only prove we agree with ourselves.
/// </summary>
internal static class GoldenCorpus
{
    public static IEnumerable<string> UblInvoicePaths =>
        Directory.EnumerateFiles(
                Path.Combine(RepositoryRoot(), "specs", "xrechnung", "testsuite", "src", "test"),
                "*_ubl.xml",
                SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.Ordinal);

    public static IEnumerable<object[]> UblInvoiceCases =>
        UblInvoicePaths.Select(path => new object[] { Path.GetFileName(path) });

    public static string Read(string fileName) =>
        File.ReadAllText(UblInvoicePaths.Single(path => Path.GetFileName(path) == fileName));

    private static string RepositoryRoot([CallerFilePath] string sourcePath = "")
    {
        var directory = new DirectoryInfo(Path.GetDirectoryName(sourcePath)!);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, ".git")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}
