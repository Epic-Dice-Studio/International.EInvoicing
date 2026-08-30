using International.EInvoicing.Configuration;
using International.EInvoicing.Profiles;
using International.EInvoicing.Validation.Schematron;

namespace International.EInvoicing.Validation.XRechnung;

/// <summary>Adds the German rules to a library instance.</summary>
public static class XRechnungBuilderExtensions
{
    /// <summary>
    /// Checks documents that declare an XRechnung profile against the German rules.
    /// </summary>
    /// <remarks>
    /// XRechnung restricts EN 16931 rather than replacing it, so this is added alongside
    /// <c>AddEn16931Rules()</c>, not instead of it. A document declaring something else is left to the rule
    /// sets that do govern it.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
    public static EInvoicingBuilder AddXRechnungRules(this EInvoicingBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder
            .AddRules(DocumentSyntax.Ubl, XRechnungRules.For(DocumentSyntax.Ubl), IsXRechnung)
            .AddRules(DocumentSyntax.Cii, XRechnungRules.For(DocumentSyntax.Cii), IsXRechnung);
    }

    private static bool IsXRechnung(ProfileIdentifier specification) =>
        specification.Value?.Contains("xrechnung", StringComparison.OrdinalIgnoreCase) == true;
}
