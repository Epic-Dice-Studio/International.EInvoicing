using International.EInvoicing.Configuration;
using International.EInvoicing.Peppol;

namespace International.EInvoicing.Countries.Italy;

/// <summary>Registers the profiles Italy exchanges over Peppol.</summary>
public static class ItalyServiceCollectionExtensions
{
    /// <summary>
    /// Adds Peppol BIS Billing, which is what Italy exchanges over the network.
    /// </summary>
    /// <remarks>
    /// Not FatturaPA: the domestic format is its own syntax and requires a qualified signature. The Italian
    /// rules travel inside the Peppol rule set, so <c>AddPeppolRulesFrom(directory)</c> brings them.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
    public static EInvoicingBuilder AddItaly(this EInvoicingBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddPeppol();
    }
}
