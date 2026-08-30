using International.EInvoicing.Model;
using International.EInvoicing.Values;

namespace International.EInvoicing.Countries.France.EReporting.Model;

/// <summary>A party on a reported invoice: how it is identified, and where it is.</summary>
/// <remarks>
/// A buyer abroad may be known only by country, which is the point of reporting these transactions at all.
/// </remarks>
public sealed class FrReportedParty : InvoiceNode
{
    /// <summary>The company identifier and its scheme — <c>0002</c> a SIREN, <c>0223</c> a foreign registration.</summary>
    public IdentifierField CompanyIdentifier { get; set; }

    /// <summary>The VAT identifier. Required when the company is identified by SIREN or foreign registration.</summary>
    public FrReportedTaxRegistration? TaxRegistration { get; set; }

    /// <summary>The party's country, ISO 3166-1 alpha-2.</summary>
    public CodeField CountryCode { get; set; }
}
