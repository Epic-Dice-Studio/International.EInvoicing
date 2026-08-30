using International.EInvoicing.Configuration;
using International.EInvoicing.Peppol;

namespace International.EInvoicing.Countries.Denmark;

/// <summary>Registers the profiles Denmark uses.</summary>
public static class DenmarkServiceCollectionExtensions
{
    /// <summary>
    /// Adds what Denmark needs: Peppol BIS Billing, in both syntaxes.
    /// </summary>
    /// <remarks>
    /// The Peppol rules are fetched rather than shipped — they declare no licence — and the Danish rules
    /// travel inside them, so <c>AddPeppolRulesFrom(directory)</c> brings both.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
    public static EInvoicingBuilder AddDenmark(this EInvoicingBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddPeppol();
    }
}
