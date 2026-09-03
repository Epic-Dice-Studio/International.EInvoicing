using International.EInvoicing.Configuration;
using International.EInvoicing.OrderX.Reading;
using International.EInvoicing.Profiles;

namespace International.EInvoicing.OrderX.Tests;

/// <summary>
/// The published Order-X documents, and where they are.
/// </summary>
/// <remarks>
/// FNFE-MPE and FeRD publish Order-X behind a registration, so nothing here is committed:
/// <c>build/fetch-specs.sh order-x</c> fills the folder, and every test that needs it skips when it is
/// absent, which is also how CI runs.
/// </remarks>
internal static class OrderXCorpus
{
    /// <summary>The one published reference document: an order, in the COMFORT profile, fully populated.</summary>
    public const string ReferenceOrder = "ORDER-X_EX01_ORDER_FULL_DATA-COMFORTorder-x.xml";

    public static string Root => Path.Combine(RepositoryRoot(), "specs", "order-x");

    public static string? Find(string name)
    {
        string path = Path.Combine(Root, "examples", name);
        return File.Exists(path) ? path : null;
    }

    public static OrderXOrderReader Reader() =>
        new(new EInvoicingOptions(), new ProfileResolver(new ProfileRegistry(OrderXProfiles.All)));

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
