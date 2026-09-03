namespace International.EInvoicing.CrossCheck.Tests;

/// <summary>The official XRechnung test suite, which is what the two engines are compared over.</summary>
internal static class Corpus
{
    public static IReadOnlyList<string> Documents()
    {
        string root = Path.Combine(RepositoryRoot(), "specs", "xrechnung", "testsuite", "src", "test");

        if (!Directory.Exists(root))
        {
            return [];
        }

        return
        [
            .. Directory.EnumerateFiles(root, "*_ubl.xml", SearchOption.AllDirectories)
                .Concat(Directory.EnumerateFiles(root, "*_uncefact.xml", SearchOption.AllDirectories))
                .Order(StringComparer.Ordinal),
        ];
    }

    public static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "International.EInvoicing.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("repository root not found");
    }
}
