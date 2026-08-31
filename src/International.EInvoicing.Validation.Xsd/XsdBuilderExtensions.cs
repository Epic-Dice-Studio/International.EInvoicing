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
}
