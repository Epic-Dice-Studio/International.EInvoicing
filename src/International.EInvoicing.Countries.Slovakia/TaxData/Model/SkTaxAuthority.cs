namespace International.EInvoicing.Countries.Slovakia.TaxData.Model;

/// <summary>The authority a tax data document is reported to (TDG-04).</summary>
/// <remarks><c>ibr-tdd-12</c> allows it an identifier and a name, and nothing else.</remarks>
public sealed class SkTaxAuthority
{
    /// <summary>The authority's identifier (TDT-010), which the rules require.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>The authority's name (TDT-011).</summary>
    public string? Name { get; set; }
}
