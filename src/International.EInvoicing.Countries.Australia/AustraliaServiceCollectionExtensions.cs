using International.EInvoicing.Configuration;
using International.EInvoicing.Peppol;

namespace International.EInvoicing.Countries.Australia;

/// <summary>Registers the profiles Australia uses.</summary>
public static class AustraliaServiceCollectionExtensions
{
    /// <summary>
    /// Adds what Australia needs: the Peppol profiles, PINT included.
    /// </summary>
    /// <remarks>
    /// The A-NZ jurisdiction rules are published as pre-compiled XSLT rather than source Schematron, so they
    /// do not run here — see <c>docs/standards/peppol-pint.md</c>. A document declaring the A-NZ profile is
    /// reported as unchecked rather than passed.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
    public static EInvoicingBuilder AddAustralia(this EInvoicingBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddPeppol();
    }
}
