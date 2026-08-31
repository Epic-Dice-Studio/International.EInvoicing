using International.EInvoicing.Validation;
using International.EInvoicing.Validation.Schematron;

namespace International.EInvoicing.Peppol.TaxData;

/// <summary>
/// Judges a tax data document by OpenPeppol's own rules, for one jurisdiction.
/// </summary>
/// <remarks>
/// The tax data document is not an invoice, so it does not travel through the invoice validation pipeline:
/// nothing about it is EN 16931, its root is <c>pxs:TaxData</c>, and the profile registry has no entry that
/// could match it. It gets its own validator, taking the artefacts where the fetch script puts them.
/// </remarks>
public sealed class PeppolTaxDataValidator
{
    private readonly SchematronRuleSet _rules;

    private PeppolTaxDataValidator(SchematronRuleSet rules) => _rules = rules;

    /// <summary>The version of the rules this was loaded from.</summary>
    public string Version => _rules.Version ?? string.Empty;

    /// <summary>
    /// Loads the rules from a directory of fetched artefacts.
    /// </summary>
    /// <param name="directory">
    /// The version directory the fetch script writes, such as
    /// <c>specs/national/peppol-taxdata/schematron/tdd/sk/1.0.0</c>.
    /// </param>
    /// <exception cref="ArgumentException"><paramref name="directory"/> is empty.</exception>
    /// <exception cref="DirectoryNotFoundException">The directory does not exist.</exception>
    /// <exception cref="FileNotFoundException">The directory holds no tax data rule set.</exception>
    public static PeppolTaxDataValidator LoadFrom(string directory) =>
        LoadFrom(directory, PeppolTaxDataJurisdiction.Slovakia);

    /// <summary>The same, naming the jurisdiction the rules belong to.</summary>
    /// <param name="directory">The version directory the fetch script writes.</param>
    /// <param name="jurisdiction">Whose rules these are — it names them in the report.</param>
    /// <exception cref="ArgumentNullException"><paramref name="jurisdiction"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException"><paramref name="directory"/> is empty.</exception>
    /// <exception cref="DirectoryNotFoundException">The directory does not exist.</exception>
    /// <exception cref="FileNotFoundException">The directory holds no tax data rule set.</exception>
    public static PeppolTaxDataValidator LoadFrom(string directory, PeppolTaxDataJurisdiction jurisdiction)
    {
        ArgumentNullException.ThrowIfNull(jurisdiction);
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);

        if (!Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException(
                $"No tax data rules at '{directory}'. They are not redistributable, so this library "
                + "does not ship them: run build/fetch-specs.sh national, or point this at your own copy.");
        }

        string path = Directory.EnumerateFiles(directory, "*TDD.xslt").Order(StringComparer.Ordinal).LastOrDefault()
            ?? throw new FileNotFoundException($"No tax data rule set (*TDD.xslt) in '{directory}'.");

        return new PeppolTaxDataValidator(CompiledSchematron.Read(
            File.ReadAllText(path),
            jurisdiction.Name,
            Path.GetFileName(directory.TrimEnd(Path.DirectorySeparatorChar))));
    }

    /// <summary>What the published rules say about a tax data document.</summary>
    /// <exception cref="ArgumentException"><paramref name="document"/> is empty.</exception>
    public ValidationReport Validate(string document)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(document);

        return new SchematronValidator().Validate(document, _rules);
    }
}
