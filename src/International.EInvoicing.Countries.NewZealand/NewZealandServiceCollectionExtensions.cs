using International.EInvoicing.Configuration;
using International.EInvoicing.Peppol;

namespace International.EInvoicing.Countries.NewZealand;

/// <summary>Registers the profiles New Zealand uses.</summary>
public static class NewZealandServiceCollectionExtensions
{
    /// <summary>
    /// Adds what New Zealand needs: the Peppol profiles, PINT included.
    /// </summary>
    /// <remarks>
    /// The A-NZ jurisdiction rules are published as pre-compiled XSLT rather than source Schematron, so they
    /// do not run here — see <c>docs/standards/peppol-pint.md</c>.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
    public static EInvoicingBuilder AddNewZealand(this EInvoicingBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddPeppol();
    }
}
