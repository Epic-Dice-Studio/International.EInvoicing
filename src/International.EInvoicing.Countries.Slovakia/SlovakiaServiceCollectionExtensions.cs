using International.EInvoicing.Configuration;
using International.EInvoicing.Peppol;
using International.EInvoicing.Profiles;
using International.EInvoicing.Validation.Schematron;

namespace International.EInvoicing.Countries.Slovakia;

/// <summary>Registers what Slovakia needs.</summary>
public static class SlovakiaServiceCollectionExtensions
{
    /// <summary>Adds the Peppol BIS Billing profiles the Slovak mandate exchanges.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
    public static EInvoicingBuilder AddSlovakia(this EInvoicingBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.AddPeppol();
    }

    /// <summary>
    /// Adds the invoice rules that judge a Slovak invoice, from a directory of fetched artefacts.
    /// </summary>
    /// <remarks>
    /// OpenPeppol publishes them beside the tax data rules, as pre-compiled XSLT, which this library reads —
    /// and which is not redistributable, so it is fetched: <c>build/fetch-specs.sh national</c> writes them to
    /// <c>specs/national/peppol-taxdata/schematron/tdd/sk</c>. The tax data document itself is judged by
    /// <see cref="Validation.SkTaxDataValidator"/>, which takes the same directory.
    /// </remarks>
    /// <param name="builder">The library being assembled.</param>
    /// <param name="directory">The version directory the fetch script writes.</param>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException"><paramref name="directory"/> is empty.</exception>
    /// <exception cref="DirectoryNotFoundException">The directory does not exist.</exception>
    public static EInvoicingBuilder AddSlovakRulesFrom(this EInvoicingBuilder builder, string directory)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);

        if (!Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException(
                $"No Slovak rule sets at '{directory}'. They are not redistributable, so this library does "
                + "not ship them: run build/fetch-specs.sh national, or point this at your own copy.");
        }

        string identifier = PeppolProfiles.BillingUbl.Id.Value;

        foreach (string path in Directory.EnumerateFiles(directory, "PEPPOL-EN16931-UBL.xslt").Order(StringComparer.Ordinal))
        {
            builder.AddRules(
                DocumentSyntax.Ubl,
                CompiledSchematron.Read(File.ReadAllText(path), "Peppol BIS Billing 3.0 (UBL)", Version(directory)),
                specification => string.Equals(specification.Value, identifier, StringComparison.Ordinal));
        }

        return builder;
    }

    private static string Version(string directory) => Path.GetFileName(directory.TrimEnd(Path.DirectorySeparatorChar));
}
