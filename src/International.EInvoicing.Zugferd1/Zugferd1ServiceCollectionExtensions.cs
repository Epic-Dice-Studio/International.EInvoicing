using International.EInvoicing.Configuration;
using International.EInvoicing.Documents;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;
using International.EInvoicing.Validation.Schematron;
using International.EInvoicing.Zugferd1.Reading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace International.EInvoicing.Zugferd1;

/// <summary>Registers ZUGFeRD 1.0 reading.</summary>
public static class Zugferd1ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the ZUGFeRD 1.0 reader and its four profiles. There is no writer, on purpose: FeRD replaced this
    /// format, and a library that made it easy to produce more of it would not be doing anyone a favour.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
    public static EInvoicingBuilder AddZugferd1(this EInvoicingBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder
            .ConfigureServices(services => services.AddZugferd1Services())
            .AddProfiles(Zugferd1Profiles.All);
    }

    /// <summary>Registers the ZUGFeRD 1.0 reader in the container.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <c>null</c>.</exception>
    public static IServiceCollection AddZugferd1Services(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IDocumentReader<EInvoice>, Zugferd1InvoiceReader>());

        services.TryAddSingleton(provider =>
            provider.GetServices<IDocumentReader<EInvoice>>().OfType<Zugferd1InvoiceReader>().First());

        return services;
    }

    /// <summary>
    /// Adds the ZUGFeRD 1.0 rule set found in a directory of fetched artefacts.
    /// </summary>
    /// <remarks>
    /// FeRD published one Schematron file covering all three profiles. It is fetched rather than shipped
    /// because FeRD no longer publishes the format: <c>build/fetch-specs.sh zugferd1</c>, then
    /// <c>specs/zugferd-1.0/schematron</c>.
    /// </remarks>
    /// <param name="builder">The library being assembled.</param>
    /// <param name="directory">The <c>schematron</c> directory the fetch script writes.</param>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException"><paramref name="directory"/> is empty.</exception>
    /// <exception cref="DirectoryNotFoundException">There is no such directory.</exception>
    /// <exception cref="FileNotFoundException">It holds no rule set.</exception>
    public static EInvoicingBuilder AddZugferd1RulesFrom(this EInvoicingBuilder builder, string directory)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);

        if (!Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException(
                $"No ZUGFeRD 1.0 rule set at '{directory}'. FeRD no longer publishes the format, so this "
                + "library does not ship it: run build/fetch-specs.sh zugferd1, or point this at your own copy.");
        }

        string[] files = [.. Directory.EnumerateFiles(directory, "*.sch").Order(StringComparer.Ordinal)];

        if (files.Length == 0)
        {
            throw new FileNotFoundException(
                $"'{directory}' holds no ZUGFeRD 1.0 rule set. Run build/fetch-specs.sh zugferd1.",
                Path.Combine(directory, "ZUGFeRD_1p0.sch"));
        }

        foreach (string file in files)
        {
            builder.AddRulesFromFile(DocumentSyntax.Zugferd1, file, "ZUGFeRD 1.0", "1.0");
        }

        return builder;
    }
}
