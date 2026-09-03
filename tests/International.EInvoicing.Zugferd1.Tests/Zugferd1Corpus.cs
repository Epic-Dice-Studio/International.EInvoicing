using International.EInvoicing.Configuration;
using International.EInvoicing.Profiles;
using International.EInvoicing.Zugferd1.Reading;

namespace International.EInvoicing.Zugferd1.Tests;

/// <summary>
/// The ZUGFeRD 1.0 reference documents, and where they are.
/// </summary>
/// <remarks>
/// FeRD's own 2013 package is no longer published; these are the four reference documents mustangproject
/// carries, fetched by <c>build/fetch-specs.sh zugferd1</c> and not committed. Every test that needs them
/// skips when the folder is empty, which is also how CI runs.
/// </remarks>
internal static class Zugferd1Corpus
{
    public static string Root => Path.Combine(RepositoryRoot(), "specs", "zugferd-1.0");

    public static IReadOnlyList<string> Documents()
    {
        string examples = Path.Combine(Root, "examples");

        return Directory.Exists(examples)
            ? [.. Directory.EnumerateFiles(examples, "*.xml").Order(StringComparer.Ordinal)]
            : [];
    }

    public static string? Find(string name)
    {
        string path = Path.Combine(Root, "examples", name);
        return File.Exists(path) ? path : null;
    }

    public static Zugferd1InvoiceReader Reader() =>
        new(new EInvoicingOptions(), new ProfileResolver(new ProfileRegistry(Zugferd1Profiles.All)));

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
