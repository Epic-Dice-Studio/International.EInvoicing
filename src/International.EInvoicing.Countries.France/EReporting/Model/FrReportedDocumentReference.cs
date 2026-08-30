using International.EInvoicing.Model;
using International.EInvoicing.Values;

namespace International.EInvoicing.Countries.France.EReporting.Model;

/// <summary>A reference to an earlier invoice, which a corrective invoice or a credit note must carry.</summary>
public sealed class FrReportedDocumentReference : InvoiceNode
{
    /// <summary>The earlier invoice's number.</summary>
    public IdentifierField Identifier { get; set; }

    /// <summary>When it was issued.</summary>
    public DateField IssueDate { get; set; }
}
