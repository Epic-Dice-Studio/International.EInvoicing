using International.EInvoicing.Configuration;
using International.EInvoicing.Peppol;

namespace International.EInvoicing.Countries.Iceland;

/// <summary>Registers the profiles Iceland uses.</summary>
public static class IcelandServiceCollectionExtensions
{
    /// <summary>
    /// Adds what Iceland needs: Peppol BIS Billing, in both syntaxes.
    /// </summary>
    /// <remarks>
    /// The Peppol rules are fetched rather than shipped — they declare no licence — and the Icelandic rules
    /// travel inside them, so <c>AddPeppolRulesFrom(directory)</c> brings both.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
    public static EInvoicingBuilder AddIceland(this EInvoicingBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddPeppol();
    }
}
