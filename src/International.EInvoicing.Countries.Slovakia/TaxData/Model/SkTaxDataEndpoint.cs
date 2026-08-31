namespace International.EInvoicing.Countries.Slovakia.TaxData.Model;

/// <summary>
/// A party in a tax data document, which is an endpoint and nothing more.
/// </summary>
/// <remarks>
/// The reporting and receiving parties carry one element each — <c>cbc:EndpointID</c> with its scheme — and
/// <c>ibr-tdd-17</c> requires that scheme to be a Peppol participant identifier scheme, four digits.
/// </remarks>
public sealed class SkTaxDataEndpoint
{
    /// <summary>
    /// The scheme the receiving party is identified in: <c>0242</c>, the Peppol service provider identifier.
    /// </summary>
    /// <remarks>
    /// <c>ibr-tdd-20</c> does not merely require four digits there, as it does for the reporting party — it
    /// requires this one. The receiver of a tax data document is a service provider, not a taxpayer.
    /// </remarks>
    public const string ServiceProviderScheme = "0242";

    /// <summary>The participant identifier (TDT-013, TDT-014, TDT-015).</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Its scheme, four digits, as Peppol numbers them.</summary>
    public string SchemeId { get; set; } = string.Empty;
}
