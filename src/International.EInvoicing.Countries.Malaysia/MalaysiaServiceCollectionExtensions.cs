using International.EInvoicing.Configuration;
using International.EInvoicing.Peppol;

namespace International.EInvoicing.Countries.Malaysia;

/// <summary>Registers the profiles Malaysia uses.</summary>
public static class MalaysiaServiceCollectionExtensions
{
    /// <summary>
    /// Adds what Malaysia needs: the Peppol profiles, PINT included.
    /// </summary>
    /// <remarks>
    /// The Malaysian rules are fetched rather than shipped — <c>build/fetch-specs.sh pint</c> — and added
    /// with <c>AddPeppolPintRulesFrom(directory)</c>.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
    public static EInvoicingBuilder AddMalaysia(this EInvoicingBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddPeppol();
    }
}
