using International.EInvoicing.Configuration;
using International.EInvoicing.Peppol;

namespace International.EInvoicing.Countries.Netherlands;

/// <summary>Registers the profiles Netherlands uses.</summary>
public static class NetherlandsServiceCollectionExtensions
{
    /// <summary>
    /// Adds what the Netherlands needs: NLCIUS and its G-account extension, and Peppol BIS Billing.
    /// </summary>
    /// <remarks>
    /// Two sets of rules apply in the Netherlands and both are fetched rather than shipped: the Dutch rules
    /// inside the Peppol rule set (<c>AddPeppolRulesFrom</c>), which judge Peppol BIS documents, and the
    /// NLCIUS rules (<c>AddNlciusRulesFrom</c>), which judge NLCIUS ones.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
    public static EInvoicingBuilder AddNetherlands(this EInvoicingBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.AddPeppol().AddProfiles(NlProfiles.All);
    }
}
