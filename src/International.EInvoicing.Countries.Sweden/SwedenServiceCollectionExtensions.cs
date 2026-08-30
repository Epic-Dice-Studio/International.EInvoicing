using International.EInvoicing.Configuration;
using International.EInvoicing.Peppol;

namespace International.EInvoicing.Countries.Sweden;

/// <summary>Registers the profiles Sweden uses.</summary>
public static class SwedenServiceCollectionExtensions
{
    /// <summary>
    /// Adds what Sweden needs: Peppol BIS Billing, in both syntaxes.
    /// </summary>
    /// <remarks>
    /// The Peppol rules are fetched rather than shipped — they declare no licence — and the Swedish rules
    /// travel inside them, so <c>AddPeppolRulesFrom(directory)</c> brings both.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
    public static EInvoicingBuilder AddSweden(this EInvoicingBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddPeppol();
    }
}
