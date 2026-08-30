using International.EInvoicing.Cdar.Reading;
using International.EInvoicing.Cdar.Writing;
using International.EInvoicing.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace International.EInvoicing.Cdar;

/// <summary>Registers lifecycle message reading and writing.</summary>
public static class CdarServiceCollectionExtensions
{
    /// <summary>Adds the lifecycle profiles this library knows about.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
    public static EInvoicingBuilder AddCdar(this EInvoicingBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder
            .ConfigureServices(services => services.AddCdarServices())
            .AddProfiles(CdarProfiles.All);
    }

    /// <summary>Registers the lifecycle reader and writer in the container.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <c>null</c>.</exception>
    public static IServiceCollection AddCdarServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<CdarReader>();
        services.TryAddSingleton<CdarWriter>();
        return services;
    }
}
