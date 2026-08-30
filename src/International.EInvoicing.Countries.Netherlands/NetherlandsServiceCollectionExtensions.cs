using International.EInvoicing.Configuration;
using International.EInvoicing.Peppol;

namespace International.EInvoicing.Countries.Netherlands;

/// <summary>Registers the profiles Netherlands uses.</summary>
public static class NetherlandsServiceCollectionExtensions
{
    /// <summary>
    /// Adds what Netherlands needs: Peppol BIS Billing, in both syntaxes.
    /// </summary>
    /// <remarks>
    /// The Peppol rules are fetched rather than shipped — they declare no licence — and the Dutch rules
    /// travel inside them, so <c>AddPeppolRulesFrom(directory)</c> brings both.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
    public static EInvoicingBuilder AddNetherlands(this EInvoicingBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddPeppol();
    }
}
