using International.EInvoicing.Model;
using International.EInvoicing.Values;

namespace International.EInvoicing.Countries.France.EReporting.Model;

/// <summary>A VAT identifier, and what kind of identifier it is.</summary>
public sealed class FrReportedTaxRegistration : InvoiceNode
{
    /// <summary>The VAT number.</summary>
    public IdentifierField Identifier { get; set; }
}
