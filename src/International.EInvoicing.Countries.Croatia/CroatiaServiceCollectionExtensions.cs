using International.EInvoicing.Configuration;
using International.EInvoicing.Peppol;

namespace International.EInvoicing.Countries.Croatia;

/// <summary>Registers the profiles Croatia uses.</summary>
public static class CroatiaServiceCollectionExtensions
{
    /// <summary>
    /// Adds what Croatia needs today: Peppol BIS Billing, in both syntaxes.
    /// </summary>
    /// <remarks>
    /// Not HR-FISK 2.0, Croatia's own CIUS: its published specification identifier is not in any artefact
    /// this repository carries. Register it yourself and it wins over anything built in.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
    public static EInvoicingBuilder AddCroatia(this EInvoicingBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddPeppol();
    }
}
