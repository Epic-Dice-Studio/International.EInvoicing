using International.EInvoicing.Configuration;
using International.EInvoicing.Peppol;

namespace International.EInvoicing.Countries.Greece;

/// <summary>Registers the profiles Greece uses.</summary>
public static class GreeceServiceCollectionExtensions
{
    /// <summary>
    /// Adds what Greece needs: Peppol BIS Billing, in both syntaxes.
    /// </summary>
    /// <remarks>
    /// The Greek rules travel inside the Peppol rule set, so <c>AddPeppolRulesFrom(directory)</c> brings
    /// them along with everything else.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
    public static EInvoicingBuilder AddGreece(this EInvoicingBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddPeppol();
    }
}
