using International.EInvoicing.Configuration;
using International.EInvoicing.Profiles;
using International.EInvoicing.Ubl.Reading;
using International.EInvoicing.Ubl.Writing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace International.EInvoicing.Ubl;

/// <summary>Registers UBL reading and writing.</summary>
public static class UblServiceCollectionExtensions
{
    /// <summary>
    /// Adds the UBL reader and writer, and registers the profiles this library implements for UBL. A profile
    /// you register yourself still wins over these.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
    public static EInvoicingBuilder AddUbl(this EInvoicingBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.AddProfiles(KnownProfiles.All.Where(profile => profile.Syntax == DocumentSyntax.Ubl));
    }

    /// <summary>Registers the UBL reader and writer in the container.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <c>null</c>.</exception>
    public static IServiceCollection AddUblServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<UblInvoiceReader>();
        services.TryAddSingleton<UblInvoiceWriter>();
        return services;
    }
}
