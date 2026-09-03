using International.EInvoicing.Cii;
using International.EInvoicing.Configuration;
using International.EInvoicing.Profiles;
using International.EInvoicing.Ubl;
using International.EInvoicing.Validation.Schematron;

namespace International.EInvoicing.Peppol;

/// <summary>Adds Peppol BIS Billing to a library instance.</summary>
public static class PeppolServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Peppol profiles, and the two syntaxes they are written in.
    /// </summary>
    /// <remarks>
    /// The rules are not here: Peppol publishes them under no licence, so they are fetched rather than
    /// packaged. <see cref="AddPeppolRulesFrom"/> puts them to work once you have them.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
    public static EInvoicingBuilder AddPeppol(this EInvoicingBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder
            .AddUbl()
            .AddCii()
            .AddProfiles(PeppolProfiles.All)
            .AddProfiles(PeppolPintProfiles.All)
            .AddProfiles(PeppolPostAwardProfiles.All);
    }

    /// <summary>
    /// Adds every Peppol rule set found in a directory.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Point it at the folder <c>build/fetch-specs.sh peppol</c> filled — <c>specs/peppol/rules</c> — or
    /// wherever you keep your copy. It loads the four files Peppol publishes, its own rules and its copy of
    /// the EN 16931 ones, for whichever of them are there.
    /// </para>
    /// <para>
    /// Both apply to a Peppol document. Running only Peppol's own rules gives a false pass, which is why
    /// this loads what it finds rather than asking you to name files one by one.
    /// </para>
    /// </remarks>
    /// <param name="builder">The library being assembled.</param>
    /// <param name="directory">Where the <c>.sch</c> files are.</param>
    /// <param name="version">
    /// The Peppol release the files came from, so a report can be reproduced later. Peppol releases
    /// quarterly, and the version is part of the answer.
    /// </param>
    /// <returns>The builder, so registration can continue.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException"><paramref name="directory"/> is empty.</exception>
    /// <exception cref="DirectoryNotFoundException">There is no such directory.</exception>
    /// <exception cref="FileNotFoundException">The directory holds none of the four rule sets.</exception>
    public static EInvoicingBuilder AddPeppolRulesFrom(
        this EInvoicingBuilder builder,
        string directory,
        string version = "3.0")
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);

        if (!Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException(
                $"No Peppol rule sets at '{directory}'. They declare no licence upstream, so this library "
                + "does not ship them: run build/fetch-specs.sh peppol, or point this at your own copy.");
        }

        var added = 0;

        foreach (string fileName in PeppolProfiles.RuleSetFileNames)
        {
            string path = Path.Combine(directory, fileName);

            if (!File.Exists(path))
            {
                continue;
            }

            DocumentSyntax syntax = fileName.Contains("UBL", StringComparison.Ordinal)
                ? DocumentSyntax.Ubl
                : DocumentSyntax.Cii;

            builder.AddRulesFromFile(syntax, path, Path.GetFileNameWithoutExtension(fileName), version);
            added++;
        }

        if (added == 0)
        {
            throw new FileNotFoundException(
                $"'{directory}' holds none of the Peppol rule sets ({string.Join(", ", PeppolProfiles.RuleSetFileNames)}). "
                + "Run build/fetch-specs.sh peppol.",
                Path.Combine(directory, PeppolProfiles.RuleSetFileNames[0]));
        }

        return builder;
    }

    /// <summary>
    /// Adds the rule sets for the Peppol post-award documents that are not invoices — the Invoice Response,
    /// the Message Level Response and the Despatch Advice.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Point it at the folder <c>build/fetch-specs.sh poacc</c> filled — <c>specs/peppol/poacc/rules</c>.
    /// OpenPEPPOL generates the structural half of these rule sets when it builds them and publishes only
    /// the compiled form, so what is registered here is compiled XSLT: the assertions are recovered from it,
    /// and they run and report like any other rule set.
    /// </para>
    /// <para>
    /// They are not registered by <see cref="AddPeppol"/> for the same reason the Billing rules are not:
    /// OpenPEPPOL declares no licence permitting redistribution, so they are fetched rather than shipped.
    /// </para>
    /// </remarks>
    /// <param name="builder">The library being assembled.</param>
    /// <param name="directory">Where the compiled rule sets are.</param>
    /// <param name="version">The Peppol release the files came from, so a report can be reproduced later.</param>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException"><paramref name="directory"/> is empty.</exception>
    /// <exception cref="DirectoryNotFoundException">There is no such directory.</exception>
    /// <exception cref="FileNotFoundException">The directory holds none of the rule sets.</exception>
    public static EInvoicingBuilder AddPeppolPostAwardRulesFrom(
        this EInvoicingBuilder builder,
        string directory,
        string version = "3.0")
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);

        if (!Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException(
                $"No Peppol post-award rule sets at '{directory}'. They declare no licence upstream, so this "
                + "library does not ship them: run build/fetch-specs.sh poacc, or point this at your own copy.");
        }

        var added = 0;

        foreach ((string fileName, Profile profile) in PeppolPostAwardProfiles.RuleSets)
        {
            string path = Path.Combine(directory, fileName);

            if (!File.Exists(path))
            {
                continue;
            }

            // Each rule set governs its own transaction. Both are an ApplicationResponse, so a rule set let
            // loose on the other's documents reports failures that are not in them.
            string identifier = profile.Id.Value;

            builder.AddRulesFromFile(
                DocumentSyntax.Ubl,
                path,
                Path.GetFileNameWithoutExtension(fileName),
                version,
                declared => string.Equals(declared.Value, identifier, StringComparison.Ordinal));
            added++;
        }

        if (added == 0)
        {
            throw new FileNotFoundException(
                $"'{directory}' holds none of the post-award rule sets "
                + $"({string.Join(", ", PeppolPostAwardProfiles.RuleSets.Keys)}). Run build/fetch-specs.sh poacc.",
                Path.Combine(directory, PeppolPostAwardProfiles.RuleSets.Keys.First()));
        }

        return builder;
    }

    /// <summary>
    /// Adds the Peppol PINT rule sets found in a directory of fetched artefacts.
    /// </summary>
    /// <remarks>
    /// <para>
    /// PINT validates in two layers and both apply: the base rules every jurisdiction shares, and the ones
    /// its own specialisation adds. Each is registered against the profiles it governs, so a BIS Billing
    /// document is not judged by them and a PINT document is not judged by EN 16931.
    /// </para>
    /// <para>
    /// OpenPEPPOL publishes these as pre-compiled XSLT under no redistribution licence, so they are fetched
    /// rather than shipped: <c>build/fetch-specs.sh pint</c>, then point this at
    /// <c>specs/peppol/pint/schematron</c>.
    /// </para>
    /// </remarks>
    /// <param name="builder">The library being assembled.</param>
    /// <param name="directory">The <c>schematron</c> directory the fetch script writes.</param>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException"><paramref name="directory"/> is empty.</exception>
    /// <exception cref="DirectoryNotFoundException">The directory does not exist.</exception>
    public static EInvoicingBuilder AddPeppolPintRulesFrom(this EInvoicingBuilder builder, string directory)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);

        if (!Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException(
                $"No PINT rule sets at '{directory}'. OpenPEPPOL publishes them under no redistribution "
                + "licence, so this library does not ship them: run build/fetch-specs.sh pint, or point this "
                + "at your own copy of the schematron directory.");
        }

        foreach (Profile profile in PeppolPintProfiles.All)
        {
            foreach (SchematronRuleSet rules in PeppolPintRules.For(profile, directory))
            {
                string identifier = profile.Id.Value;

                builder.AddRules(
                    profile.Syntax,
                    rules,
                    specification => string.Equals(specification.Value, identifier, StringComparison.Ordinal));
            }
        }

        return builder;
    }
}
