using International.EInvoicing.Cdar;
using International.EInvoicing.Configuration;

namespace International.EInvoicing.Countries.France;

/// <summary>Registers the French profiles.</summary>
public static class FranceServiceCollectionExtensions
{
    /// <summary>
    /// Adds everything France needs: its invoice and lifecycle profiles, and the lifecycle reader and writer
    /// those messages are exchanged with.
    /// </summary>
    /// <remarks>
    /// The French rule sets are not here because they may not be redistributed. Fetch them once — see
    /// <c>docs/standards/country-fr.md</c> — and add them with <c>AddRulesFromFile(...)</c>.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
    public static EInvoicingBuilder AddFrance(this EInvoicingBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder
            .AddCdar()
            .AddProfiles(FrProfiles.All);
    }
}
