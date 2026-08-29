using International.EInvoicing.Configuration;

namespace International.EInvoicing.Countries.France;

/// <summary>Registers the French profiles.</summary>
public static class FranceServiceCollectionExtensions
{
    /// <summary>
    /// Adds the French profiles, so a lifecycle message declaring one resolves exactly rather than falling
    /// back to generic reading.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
    public static EInvoicingBuilder AddFrance(this EInvoicingBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddProfiles(FrProfiles.All);
    }
}
