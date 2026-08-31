using International.EInvoicing.Configuration;
using International.EInvoicing.Peppol;

namespace International.EInvoicing.Countries.Belgium;

/// <summary>Registers the profiles Belgium uses.</summary>
public static class BelgiumServiceCollectionExtensions
{
    /// <summary>
    /// Adds what Belgium needs: Peppol BIS Billing, which the 2026 mandate is built on, in both syntaxes.
    /// </summary>
    /// <remarks>
    /// The Peppol rules are fetched rather than shipped — they declare no licence. Add them with
    /// <c>AddPeppolRulesFrom(directory)</c> once you have them.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
    public static EInvoicingBuilder AddBelgium(this EInvoicingBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddPeppol().AddProfiles(BeProfiles.All);
    }
}
