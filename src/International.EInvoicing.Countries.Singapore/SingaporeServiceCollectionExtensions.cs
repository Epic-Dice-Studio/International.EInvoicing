using International.EInvoicing.Configuration;
using International.EInvoicing.Peppol;

namespace International.EInvoicing.Countries.Singapore;

/// <summary>Registers the profiles Singapore uses.</summary>
public static class SingaporeServiceCollectionExtensions
{
    /// <summary>
    /// Adds what Singapore needs: the Peppol profiles, PINT included.
    /// </summary>
    /// <remarks>
    /// The Singaporean rules are fetched rather than shipped — OpenPEPPOL publishes them under no
    /// redistribution licence — so add them with <c>AddPeppolPintRulesFrom(directory)</c> once
    /// <c>build/fetch-specs.sh pint</c> has run.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
    public static EInvoicingBuilder AddSingapore(this EInvoicingBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddPeppol();
    }
}
