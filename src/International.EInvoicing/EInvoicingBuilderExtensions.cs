using International.EInvoicing.Cdar;
using International.EInvoicing.Cii;
using International.EInvoicing.Configuration;
using International.EInvoicing.Documents;
using International.EInvoicing.FacturX;
using International.EInvoicing.FacturX.Pdf;
using International.EInvoicing.Profiles;
using International.EInvoicing.Ubl;
using International.EInvoicing.Validation;
using International.EInvoicing.Validation.En16931;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace International.EInvoicing;

/// <summary>The one-line way to switch everything on.</summary>
public static class EInvoicingBuilderExtensions
{
    /// <summary>
    /// Everything this package carries: UBL, CII, lifecycle messages, Factur-X, and the EN 16931 rules.
    /// </summary>
    /// <remarks>
    /// Start here and add what is missing — a country, a rule set you fetched, your own profile. Reach for
    /// the individual <c>AddUbl()</c>, <c>AddCii()</c> and the rest only when you want less than this.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
    public static EInvoicingBuilder AddDefaults(this EInvoicingBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder
            .AddUbl()
            .AddCii()
            .AddCdar()
            .AddFacturX()
            .AddEn16931Rules()
            .AddFacade();
    }

    /// <summary>
    /// Registers <see cref="EInvoicing"/> itself, so it can be injected.
    /// </summary>
    /// <remarks>
    /// Included by <see cref="AddDefaults"/>. Call it yourself when you assembled the pieces one by one and
    /// still want the short way in. Outside a container it does nothing — there is nothing to register into.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
    public static EInvoicingBuilder AddFacade(this EInvoicingBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.ConfigureServices(services =>
        {
            services.TryAddSingleton(provider => new DocumentHandlers(
                provider.GetServices<IDocumentReader<Model.EInvoice>>(),
                provider.GetServices<IDocumentWriter<Model.EInvoice>>(),
                provider.GetServices<IDocumentReader<Cdar.Model.LifecycleStatusMessage>>(),
                provider.GetServices<IDocumentWriter<Cdar.Model.LifecycleStatusMessage>>()));

            services.TryAddSingleton(provider => new EInvoicing(
                provider.GetRequiredService<Configuration.EInvoicingOptions>(),
                provider.GetRequiredService<IProfileResolver>(),
                provider.GetServices<IDocumentRuleSet>(),
                provider.GetRequiredService<DocumentHandlers>(),
                provider.GetService<IPdfAttachmentReader>()));
        });
    }
}
