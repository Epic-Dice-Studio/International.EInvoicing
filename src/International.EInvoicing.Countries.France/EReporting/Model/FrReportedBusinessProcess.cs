using International.EInvoicing.Model;
using International.EInvoicing.Values;

namespace International.EInvoicing.Countries.France.EReporting.Model;

/// <summary>Which invoicing framework the reported invoice belongs to, and which profile the report follows.</summary>
public sealed class FrReportedBusinessProcess : InvoiceNode
{
    /// <summary>The invoicing framework — <c>B1</c>, <c>S1</c>, <c>M1</c> and the rest.</summary>
    public IdentifierField Identifier { get; set; }

    /// <summary>The profile. For e-reporting, always <see cref="FrEReportCodes.ProfileIdentifier"/>.</summary>
    public IdentifierField ProfileIdentifier { get; set; }
}
