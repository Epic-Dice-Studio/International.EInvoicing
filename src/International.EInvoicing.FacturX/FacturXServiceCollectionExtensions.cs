using International.EInvoicing.Configuration;
using International.EInvoicing.Profiles;

namespace International.EInvoicing.FacturX;

/// <summary>Registers the Factur-X and ZUGFeRD profiles.</summary>
public static class FacturXServiceCollectionExtensions
{
    /// <summary>
    /// Adds the five Factur-X profiles. Reading and writing the payload needs CII, so this also implies
    /// <c>AddCii()</c>.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
    public static EInvoicingBuilder AddFacturX(this EInvoicingBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder
            .AddProfiles(KnownProfiles.All.Where(profile => profile.Syntax == DocumentSyntax.Cii))
            .AddProfiles(FacturXProfiles.All);
    }
}
