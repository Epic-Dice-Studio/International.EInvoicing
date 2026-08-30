using International.EInvoicing.Configuration;
using International.EInvoicing.Profiles;

namespace International.EInvoicing.Validation.Schematron;

/// <summary>Adds Schematron rules to a library instance.</summary>
public static class SchematronBuilderExtensions
{
    /// <summary>Adds a rule set that has already been loaded.</summary>
    /// <param name="builder">The library being assembled.</param>
    /// <param name="syntax">The syntax the rules are written against.</param>
    /// <param name="rules">The rules themselves.</param>
    /// <param name="appliesTo">Which declared profiles they govern. Omit for every document in that syntax.</param>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    public static EInvoicingBuilder AddRules(
        this EInvoicingBuilder builder,
        DocumentSyntax syntax,
        SchematronRuleSet rules,
        Func<ProfileIdentifier, bool>? appliesTo = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ConfigureServices(services => services.AddSchematronValidation());
        return builder.AddRules(new SchematronDocumentRuleSet(rules, syntax, appliesTo));
    }

    /// <summary>
    /// Adds a rule set from a <c>.sch</c> file on disk.
    /// </summary>
    /// <remarks>
    /// This is how the artefacts this library cannot ship are brought in. The Peppol and French rules declare
    /// no licence, so they are fetched rather than packaged — <c>build/fetch-specs.sh</c> — and this is the
    /// one line that puts them to work:
    /// <code>
    /// EInvoicing library = EInvoicing.Create(e => e
    ///     .AddDefaults()
    ///     .AddRulesFromFile(DocumentSyntax.Ubl, "artefacts/PEPPOL-EN16931-UBL.sch", "Peppol BIS Billing", "3.0"));
    /// </code>
    /// </remarks>
    /// <param name="builder">The library being assembled.</param>
    /// <param name="syntax">The syntax the rules are written against.</param>
    /// <param name="path">Where the file is.</param>
    /// <param name="name">What a report should call it.</param>
    /// <param name="version">Which version it is, so a report can be reproduced later.</param>
    /// <param name="appliesTo">Which declared profiles it governs. Omit for every document in that syntax.</param>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    /// <exception cref="FileNotFoundException">There is no file there.</exception>
    /// <exception cref="XPath.XPathException">An expression in the rule set could not be read.</exception>
    public static EInvoicingBuilder AddRulesFromFile(
        this EInvoicingBuilder builder,
        DocumentSyntax syntax,
        string path,
        string name,
        string version,
        Func<ProfileIdentifier, bool>? appliesTo = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"No rule set at '{path}'. Artefacts that may not be redistributed are fetched rather than "
                + "packaged; see build/fetch-specs.sh and docs/standards/.",
                path);
        }

        return builder.AddRules(syntax, SchematronRuleSet.Load(File.ReadAllText(path), name, version), appliesTo);
    }

    /// <summary>Adds a rule set from Schematron you have in hand — embedded in your own assembly, say.</summary>
    /// <param name="builder">The library being assembled.</param>
    /// <param name="syntax">The syntax the rules are written against.</param>
    /// <param name="schematron">The rule set's XML.</param>
    /// <param name="name">What a report should call it.</param>
    /// <param name="version">Which version it is.</param>
    /// <param name="appliesTo">Which declared profiles it governs. Omit for every document in that syntax.</param>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    /// <exception cref="XPath.XPathException">An expression in the rule set could not be read.</exception>
    public static EInvoicingBuilder AddRulesFrom(
        this EInvoicingBuilder builder,
        DocumentSyntax syntax,
        string schematron,
        string name,
        string version,
        Func<ProfileIdentifier, bool>? appliesTo = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.AddRules(syntax, SchematronRuleSet.Load(schematron, name, version), appliesTo);
    }
}
