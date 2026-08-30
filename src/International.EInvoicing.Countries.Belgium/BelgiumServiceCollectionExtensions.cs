using International.EInvoicing.Configuration;

namespace International.EInvoicing.Countries.Belgium;

/// <summary>Registers the profiles Belgium uses.</summary>
public static class BelgiumServiceCollectionExtensions
{
    /// <summary>Adds the Peppol BIS profiles the Belgian mandate is built on.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
    public static EInvoicingBuilder AddBelgium(this EInvoicingBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddProfiles(BeProfiles.All);
    }
}
