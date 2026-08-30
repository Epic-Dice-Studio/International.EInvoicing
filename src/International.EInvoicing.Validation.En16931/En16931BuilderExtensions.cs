using International.EInvoicing.Configuration;
using International.EInvoicing.Profiles;
using International.EInvoicing.Validation.Schematron;

namespace International.EInvoicing.Validation.En16931;

/// <summary>Adds the EN 16931 rules to a library instance.</summary>
public static class En16931BuilderExtensions
{
    /// <summary>
    /// Checks every UBL and CII document against EN 16931.
    /// </summary>
    /// <remarks>
    /// These are the rules a CIUS restricts and an extension builds on, so they apply whatever a document
    /// declares. A national rule set is added alongside them, never instead of them.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
    public static EInvoicingBuilder AddEn16931Rules(this EInvoicingBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder
            .AddRules(DocumentSyntax.Ubl, En16931Rules.For(DocumentSyntax.Ubl))
            .AddRules(DocumentSyntax.Cii, En16931Rules.For(DocumentSyntax.Cii));
    }
}
