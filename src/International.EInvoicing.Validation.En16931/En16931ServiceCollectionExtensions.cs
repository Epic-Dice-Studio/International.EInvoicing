using International.EInvoicing.Validation.Schematron;
using Microsoft.Extensions.DependencyInjection;

namespace International.EInvoicing.Validation.En16931;

/// <summary>Registers EN 16931 validation.</summary>
public static class En16931ServiceCollectionExtensions
{
    /// <summary>Adds the Schematron engine and the embedded EN 16931 rule sets.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <c>null</c>.</exception>
    public static IServiceCollection AddEn16931Validation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services.AddSchematronValidation();
    }
}
