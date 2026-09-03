using International.EInvoicing.Configuration;
using International.EInvoicing.Documents;
using International.EInvoicing.Model;
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

        return builder
            .ConfigureServices(services => services.AddUblServices())
            .AddProfiles(KnownProfiles.All.Where(profile => profile.Syntax == DocumentSyntax.Ubl));
    }

    /// <summary>Registers the UBL reader and writer in the container.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <c>null</c>.</exception>
    public static IServiceCollection AddUblServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Registered by interface first, so a reader or writer of your own sits alongside these and the
        // facade prefers whichever was registered last. The concrete types resolve to the same instances.
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDocumentReader<EInvoice>, UblInvoiceReader>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDocumentWriter<EInvoice>, UblInvoiceWriter>());

        services.TryAddSingleton(provider =>
            provider.GetServices<IDocumentReader<EInvoice>>().OfType<UblInvoiceReader>().First());
        services.TryAddSingleton(provider =>
            provider.GetServices<IDocumentWriter<EInvoice>>().OfType<UblInvoiceWriter>().First());

        // A UBL ApplicationResponse says what happened to a document rather than what is owed, so it fills
        // the lifecycle model and registers beside the UN/CEFACT reader that fills the same one.
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IDocumentReader<LifecycleStatusMessage>, UblApplicationResponseReader>());
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IDocumentWriter<LifecycleStatusMessage>, UblApplicationResponseWriter>());

        services.TryAddSingleton(provider =>
            provider.GetServices<IDocumentReader<LifecycleStatusMessage>>().OfType<UblApplicationResponseReader>().First());
        services.TryAddSingleton(provider =>
            provider.GetServices<IDocumentWriter<LifecycleStatusMessage>>().OfType<UblApplicationResponseWriter>().First());

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IDocumentReader<DespatchAdvice>, UblDespatchAdviceReader>());
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IDocumentWriter<DespatchAdvice>, UblDespatchAdviceWriter>());

        services.TryAddSingleton(provider =>
            provider.GetServices<IDocumentReader<DespatchAdvice>>().OfType<UblDespatchAdviceReader>().First());
        services.TryAddSingleton(provider =>
            provider.GetServices<IDocumentWriter<DespatchAdvice>>().OfType<UblDespatchAdviceWriter>().First());

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDocumentReader<Order>, UblOrderReader>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDocumentWriter<Order>, UblOrderWriter>());

        services.TryAddSingleton(provider =>
            provider.GetServices<IDocumentReader<Order>>().OfType<UblOrderReader>().First());
        services.TryAddSingleton(provider =>
            provider.GetServices<IDocumentWriter<Order>>().OfType<UblOrderWriter>().First());

        return services;
    }
}
