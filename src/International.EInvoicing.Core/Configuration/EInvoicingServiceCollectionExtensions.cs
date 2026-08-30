using International.EInvoicing.Profiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace International.EInvoicing.Configuration;

/// <summary>Registers the library in a dependency injection container.</summary>
public static class EInvoicingServiceCollectionExtensions
{
    /// <summary>
    /// Adds the library to <paramref name="services"/>. Format and country packages add themselves through
    /// <paramref name="configure"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <c>null</c>.</exception>
    public static IServiceCollection AddEInvoicing(
        this IServiceCollection services,
        Action<EInvoicingBuilder>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var builder = new EInvoicingBuilder(services);
        configure?.Invoke(builder);

        ProfileRegistry registry = builder.BuildRegistry();

        services.TryAddSingleton(builder.BuildOptions());
        services.TryAddSingleton<IProfileRegistry>(registry);
        services.TryAddSingleton<IProfileResolver>(sp =>
            new ProfileResolver(sp.GetRequiredService<IProfileRegistry>()));

        return services;
    }
}
