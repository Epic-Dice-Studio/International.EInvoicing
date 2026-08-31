using International.EInvoicing.Model;
using International.EInvoicing.Peppol.TaxData;

namespace International.EInvoicing.Peppol.TaxData.Model;

/// <summary>
/// A tax data document — what a tax authority is told about an invoice.
/// </summary>
/// <remarks>
/// <para>
/// A reporting mandate has two halves. The invoice travels between the parties as Peppol BIS Billing; this is
/// the other half, sent to the tax authority — for Slovakia, within fifteen minutes of it. The transmission
/// is transport, and out of scope here; the document it carries is not, and OpenPeppol publishes a rule set
/// per jurisdiction that judges it.
/// </para>
/// <para>
/// The document it reports is an ordinary <see cref="EInvoice"/>. That is not a convenience: every business
/// term the reported document may carry is an EN 16931 term, and the rules are written as "MUST NOT contain
/// elements other than" — a projection of the invoice, not a vocabulary of its own. So the invoice is held
/// here as it is, and the writer emits the part that is allowed to travel.
/// </para>
/// </remarks>
public sealed class PeppolTaxData : InvoiceNode
{
    /// <summary>Whose rules this document is written to. Slovakia unless you say otherwise.</summary>
    public PeppolTaxDataJurisdiction Jurisdiction { get; set; } = PeppolTaxDataJurisdiction.Slovakia;

    /// <summary>This document's own identifier (TDT-003), which is not the invoice's.</summary>
    public string Uuid { get; set; } = string.Empty;

    /// <summary>When it was issued (TDT-004 and TDT-005), with the offset the rules require on the time.</summary>
    public DateTimeOffset IssuedAt { get; set; }

    /// <summary>What is being reported (TDT-007). See <see cref="PeppolTaxDataJurisdiction.TaxDataTypes"/>.</summary>
    public string TaxDataTypeCode { get; set; } = string.Empty;

    /// <summary>How far the transaction reaches (TDT-006). See <see cref="PeppolTaxDataJurisdiction.DocumentScopes"/>.</summary>
    public string DocumentScope { get; set; } = string.Empty;

    /// <summary>Which corner is reporting (TDT-012). See <see cref="PeppolTaxDataJurisdiction.ReporterRoles"/>.</summary>
    public string ReporterRole { get; set; } = string.Empty;

    /// <summary>The authority being reported to (TDG-04).</summary>
    public PeppolTaxAuthority Authority { get; set; } = new();

    /// <summary>Who is reporting (TDG-05).</summary>
    public PeppolTaxDataEndpoint ReportingParty { get; set; } = new();

    /// <summary>Who receives the report (TDG-06).</summary>
    public PeppolTaxDataEndpoint ReceivingParty { get; set; } = new();

    /// <summary>The reporter's representative (TDG-07), when one is involved.</summary>
    public PeppolTaxDataEndpoint? ReportersRepresentative { get; set; }

    /// <summary>The invoice this reports (TDG-01 and TDG-02).</summary>
    public EInvoice? ReportedDocument { get; set; }

    /// <summary>The reported document's own identifier (TDT-017), which the rules require beside BT-1.</summary>
    public string ReportedDocumentUuid { get; set; } = string.Empty;
}
