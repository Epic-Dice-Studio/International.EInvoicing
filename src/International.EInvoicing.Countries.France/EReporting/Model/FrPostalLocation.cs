using International.EInvoicing.Model;
using International.EInvoicing.Values;

namespace International.EInvoicing.Countries.France.EReporting.Model;

/// <summary>A postal address, as e-reporting carries it.</summary>
public sealed class FrPostalLocation : InvoiceNode
{
    /// <summary>The first address line.</summary>
    public TextField LineOne { get; set; }

    /// <summary>The second address line.</summary>
    public TextField LineTwo { get; set; }

    /// <summary>The third address line.</summary>
    public TextField LineThree { get; set; }

    /// <summary>The town.</summary>
    public TextField CityName { get; set; }

    /// <summary>The postal code.</summary>
    public TextField PostalZone { get; set; }

    /// <summary>The region or department.</summary>
    public TextField CountrySubentity { get; set; }

    /// <summary>The country, ISO 3166-1 alpha-2.</summary>
    public CodeField CountryCode { get; set; }
}
