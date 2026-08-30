using International.EInvoicing.Configuration;
using International.EInvoicing.Peppol;

namespace International.EInvoicing.Countries.Norway;

/// <summary>Registers the profiles Norway uses.</summary>
public static class NorwayServiceCollectionExtensions
{
    /// <summary>
    /// Adds what Norway needs: EHF 3.0 in both syntaxes, and the Peppol BIS Billing profiles it restricts.
    /// </summary>
    /// <remarks>
    /// The Peppol rules are fetched rather than shipped — they declare no licence — and the Norwegian rules
    /// travel inside them, so <c>AddPeppolRulesFrom(directory)</c> brings both.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
    public static EInvoicingBuilder AddNorway(this EInvoicingBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.AddPeppol().AddProfiles([NoProfiles.Ehf3Ubl, NoProfiles.Ehf3Cii]);
    }
}
