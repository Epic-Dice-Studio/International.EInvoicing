using International.EInvoicing.Configuration;

namespace International.EInvoicing.Countries.Germany;

/// <summary>Registers the German profiles.</summary>
public static class GermanyServiceCollectionExtensions
{
    /// <summary>Adds the XRechnung profiles, for both syntaxes.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
    public static EInvoicingBuilder AddGermany(this EInvoicingBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddProfiles(DeProfiles.All);
    }
}
