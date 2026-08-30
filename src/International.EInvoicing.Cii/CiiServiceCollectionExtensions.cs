using International.EInvoicing.Cii.Reading;
using International.EInvoicing.Cii.Writing;
using International.EInvoicing.Configuration;
using International.EInvoicing.Documents;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace International.EInvoicing.Cii;

/// <summary>Registers CII reading and writing.</summary>
public static class CiiServiceCollectionExtensions
{
    /// <summary>
    /// Adds the CII reader and writer, and registers the profiles this library implements for CII. A profile
    /// you register yourself still wins over these.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
    public static EInvoicingBuilder AddCii(this EInvoicingBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder
            .ConfigureServices(services => services.AddCiiServices())
            .AddProfiles(KnownProfiles.All.Where(profile => profile.Syntax == DocumentSyntax.Cii));
    }

    /// <summary>Registers the CII reader and writer in the container.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <c>null</c>.</exception>
    public static IServiceCollection AddCiiServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Registered by interface first, so a reader or writer of your own sits alongside these and the
        // facade prefers whichever was registered last. The concrete types resolve to the same instances.
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDocumentReader<EInvoice>, CiiInvoiceReader>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDocumentWriter<EInvoice>, CiiInvoiceWriter>());

        services.TryAddSingleton(provider =>
            provider.GetServices<IDocumentReader<EInvoice>>().OfType<CiiInvoiceReader>().First());
        services.TryAddSingleton(provider =>
            provider.GetServices<IDocumentWriter<EInvoice>>().OfType<CiiInvoiceWriter>().First());

        return services;
    }
}
