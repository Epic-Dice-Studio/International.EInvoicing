using International.EInvoicing.Configuration;

namespace International.EInvoicing.Validation.Xsd;

/// <summary>Registers schema validation.</summary>
public static class XsdBuilderExtensions
{
    /// <summary>
    /// Adds the OASIS UBL 2.1 schemas to what <c>Validate</c> runs.
    /// </summary>
    /// <remarks>
    /// It is a rule set like any other, so it appears in the report beside the business rules and says
    /// whether it ran. What it catches is what no business rule looks at: element order, cardinality, and
    /// values a type does not allow.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
    public static EInvoicingBuilder AddUblSchema(this EInvoicingBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.AddRules(new UblSchemaRuleSet());
    }

    /// <summary>Adds the UN/CEFACT CII D22B schemas to what <c>Validate</c> runs.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
    public static EInvoicingBuilder AddCiiSchema(this EInvoicingBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.AddRules(new CiiSchemaRuleSet());
    }

    /// <summary>
    /// Adds the Order-X schemas found in a directory of fetched artefacts, one rule set per profile.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Point it at the folder <c>build/fetch-specs.sh order-x</c> fills — <c>specs/order-x/schema</c> — which
    /// holds one directory per profile. Each is registered against the profile identifier it governs, so a
    /// BASIC document is judged by the BASIC schema rather than by the widest one that would accept it.
    /// </para>
    /// <para>
    /// Fetched rather than embedded because FNFE-MPE and FeRD publish Order-X behind a registration and
    /// permit no redistribution.
    /// </para>
    /// </remarks>
    /// <param name="builder">The library being assembled.</param>
    /// <param name="directory">The <c>schema</c> directory the fetch script writes.</param>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException"><paramref name="directory"/> is empty.</exception>
    /// <exception cref="DirectoryNotFoundException">The directory does not exist.</exception>
    /// <exception cref="FileNotFoundException">It holds none of the three profile directories.</exception>
    public static EInvoicingBuilder AddOrderXSchemaFrom(this EInvoicingBuilder builder, string directory)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);

        if (!Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException(
                $"No Order-X schemas at '{directory}'. Run build/fetch-specs.sh order-x, or point this at "
                + "your own copy of the schema directory.");
        }

        var added = 0;

        foreach (string profile in (string[])["basic", "comfort", "extended"])
        {
            string path = Path.Combine(directory, profile);

            if (!Directory.Exists(path))
            {
                continue;
            }

            string identifier = $"urn:order-x.eu:1p0:{profile}";

            builder.AddRules(new OrderXSchemaRuleSet(
                path,
                $"Order-X 1.0 {profile.ToUpperInvariant()} (schema)",
                declared => string.Equals(declared.Value, identifier, StringComparison.Ordinal)));
            added++;
        }

        if (added == 0)
        {
            throw new FileNotFoundException(
                $"'{directory}' holds none of the Order-X profile directories (basic, comfort, extended). "
                + "Run build/fetch-specs.sh order-x.",
                Path.Combine(directory, "comfort"));
        }

        return builder;
    }

    /// <summary>
    /// Adds both, which is what a library reading whatever arrives wants.
    /// </summary>
    /// <remarks>
    /// Each applies to its own syntax, so a document is judged by one of them and the other says it did not
    /// run — the report keeps "clean" and "never looked at" apart on purpose.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
    public static EInvoicingBuilder AddSchemas(this EInvoicingBuilder builder) =>
        builder.AddUblSchema().AddCiiSchema();
}
