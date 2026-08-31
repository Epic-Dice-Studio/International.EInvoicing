using International.EInvoicing.Configuration;
using International.EInvoicing.Peppol;

namespace International.EInvoicing.Countries.Japan;

/// <summary>Registers the profiles Japan uses.</summary>
public static class JapanServiceCollectionExtensions
{
    /// <summary>
    /// Adds what Japan needs: the Peppol profiles, PINT included.
    /// </summary>
    /// <remarks>
    /// The Japanese rules are fetched rather than shipped — <c>build/fetch-specs.sh pint</c> — and added
    /// with <c>AddPeppolPintRulesFrom(directory)</c>.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
    public static EInvoicingBuilder AddJapan(this EInvoicingBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddPeppol();
    }
}
