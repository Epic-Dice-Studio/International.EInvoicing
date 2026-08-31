using International.EInvoicing.Configuration;
using International.EInvoicing.Model;
using International.EInvoicing.Peppol;
using International.EInvoicing.Profiles;
using International.EInvoicing.Validation.Schematron;

namespace International.EInvoicing.Countries.Croatia;

/// <summary>Registers the profiles Croatia uses.</summary>
public static class CroatiaServiceCollectionExtensions
{
    /// <summary>
    /// Adds what Croatia exchanges: Peppol BIS Billing in both syntaxes, and CIUS-HR with its extension.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
    public static EInvoicingBuilder AddCroatia(this EInvoicingBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.AddPeppol().AddProfiles(HrProfiles.All);
    }

    /// <summary>
    /// Adds the CIUS-HR rules found in a directory of fetched artefacts.
    /// </summary>
    /// <remarks>
    /// Published as pre-compiled XSLT, which this library reads as data, and not redistributable — so they
    /// are fetched: <c>build/fetch-specs.sh national</c> writes them to
    /// <c>specs/national/eracun/schematron</c>. The newest version found there is the one registered.
    /// </remarks>
    /// <param name="builder">The library being assembled.</param>
    /// <param name="directory">The <c>schematron</c> directory the fetch script writes.</param>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException"><paramref name="directory"/> is empty.</exception>
    /// <exception cref="DirectoryNotFoundException">The directory does not exist.</exception>
    public static EInvoicingBuilder AddCroatianRulesFrom(this EInvoicingBuilder builder, string directory)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);

        if (!Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException(
                $"No CIUS-HR rule sets at '{directory}'. They are not redistributable, so this library does "
                + "not ship them: run build/fetch-specs.sh national, or point this at your own copy.");
        }

        string? newest = Directory
            .EnumerateDirectories(directory)
            .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .LastOrDefault();

        if (newest is null)
        {
            return builder;
        }

        string identifier = HrProfiles.CiusHrUbl.Id.Value;

        foreach (string path in Directory.EnumerateFiles(newest, "*.xslt").Order(StringComparer.Ordinal))
        {
            builder.AddRules(
                DocumentSyntax.Ubl,
                CompiledSchematron.Read(
                    File.ReadAllText(path),
                    $"CIUS-HR ({Path.GetFileNameWithoutExtension(path)})",
                    Path.GetFileName(newest)),
                specification => string.Equals(specification.Value, identifier, StringComparison.Ordinal));
        }

        return builder;
    }

    /// <summary>
    /// Writes the time of issue and the operator who issued it into every Croatian invoice.
    /// </summary>
    /// <remarks>
    /// HR-BT-2, HR-BT-4 and HR-BT-5 are demanded by <c>HR-BR-2</c>, <c>HR-BR-37</c> and <c>HR-BR-9</c>, and
    /// EN 16931 defines none of them, so they are written into the document rather than held in the model.
    /// One operator for the whole library is the usual case — the operator is the installation, not the
    /// invoice; take the overload with a delegate when it is not.
    /// </remarks>
    /// <param name="builder">The library being assembled.</param>
    /// <param name="issuer">Who issued the invoices this library writes.</param>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    public static EInvoicingBuilder AddCroatianOperator(this EInvoicingBuilder builder, HrOperator issuer)
    {
        ArgumentNullException.ThrowIfNull(issuer);

        return builder.AddCroatianOperator(_ => issuer, TimeProvider.System);
    }

    /// <summary>
    /// The same, where which operator issued an invoice depends on the invoice.
    /// </summary>
    /// <param name="builder">The library being assembled.</param>
    /// <param name="issuerFor">
    /// Who issued this invoice. Returning <c>null</c> leaves the document alone, which is what a library
    /// writing both Croatian and foreign invoices wants.
    /// </param>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    public static EInvoicingBuilder AddCroatianOperator(
        this EInvoicingBuilder builder,
        Func<EInvoice, HrOperator?> issuerFor) =>
        builder.AddCroatianOperator(issuerFor, TimeProvider.System);

    /// <summary>The same, taking the time of issue from a clock of your own.</summary>
    /// <param name="builder">The library being assembled.</param>
    /// <param name="issuerFor">Who issued this invoice, or <c>null</c> to leave the document alone.</param>
    /// <param name="clock">Where the time of issue comes from.</param>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    public static EInvoicingBuilder AddCroatianOperator(
        this EInvoicingBuilder builder,
        Func<EInvoice, HrOperator?> issuerFor,
        TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(issuerFor);
        ArgumentNullException.ThrowIfNull(clock);

        return builder.AddWriteStep(new HrOperatorStep(issuerFor, clock));
    }
}
