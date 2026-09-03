using International.EInvoicing.Configuration;
using International.EInvoicing.Documents;
using International.EInvoicing.Model;
using International.EInvoicing.OrderX.Reading;
using International.EInvoicing.OrderX.Writing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace International.EInvoicing.OrderX;

/// <summary>Registers Order-X reading and writing.</summary>
public static class OrderXServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Order-X reader and writer, and the three profiles. The rules and schemas are not here:
    /// FNFE-MPE and FeRD publish them behind a registration, so they are fetched rather than packaged.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
    public static EInvoicingBuilder AddOrderX(this EInvoicingBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder
            .ConfigureServices(services => services.AddOrderXServices())
            .AddProfiles(OrderXProfiles.All);
    }

    /// <summary>Registers the Order-X reader and writer in the container.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <c>null</c>.</exception>
    public static IServiceCollection AddOrderXServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // An order and an order change are the same document with a different type code, so one reader and
        // one writer serve both — the arrangement an invoice and a credit note already have.
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDocumentReader<Order>, OrderXOrderReader>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDocumentWriter<Order>, OrderXOrderWriter>());

        services.TryAddSingleton(provider =>
            provider.GetServices<IDocumentReader<Order>>().OfType<OrderXOrderReader>().First());
        services.TryAddSingleton(provider =>
            provider.GetServices<IDocumentWriter<Order>>().OfType<OrderXOrderWriter>().First());

        return services;
    }
}
