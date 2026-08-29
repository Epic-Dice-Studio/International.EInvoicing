using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace International.EInvoicing.Validation.Schematron;

/// <summary>Registers the Schematron engine.</summary>
public static class SchematronServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Schematron validator. Register your own implementation before this one to keep it: this does
    /// not replace what is already there.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <c>null</c>.</exception>
    public static IServiceCollection AddSchematronValidation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<SchematronValidator>();
        return services;
    }
}
