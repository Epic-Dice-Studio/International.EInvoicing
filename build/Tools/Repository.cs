namespace International.EInvoicing.BuildTools;

internal static class Repository
{
    public static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, ".git")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException("Repository root not found: no .git directory above the tool.");
    }
}
