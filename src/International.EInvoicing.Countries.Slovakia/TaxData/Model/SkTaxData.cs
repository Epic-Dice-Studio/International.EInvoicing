using International.EInvoicing.Model;

namespace International.EInvoicing.Countries.Slovakia.TaxData.Model;

/// <summary>
/// A Slovak tax data document — what the financial administration is told about an invoice.
/// </summary>
/// <remarks>
/// <para>
/// Slovakia's mandate has two halves. The invoice travels between the parties as Peppol BIS Billing; this is
/// the other half, sent to the financial administration within fifteen minutes of it. The transmission is
/// transport, and out of scope here; the document it carries is not, and OpenPeppol publishes 88 assertions
/// that judge it.
/// </para>
/// <para>
/// The document it reports is an ordinary <see cref="EInvoice"/>. That is not a convenience: every business
/// term the reported document may carry is an EN 16931 term, and the rules are written as "MUST NOT contain
/// elements other than" — a projection of the invoice, not a vocabulary of its own. So the invoice is held
/// here as it is, and the writer emits the part that is allowed to travel.
/// </para>
/// </remarks>
public sealed class SkTaxData : InvoiceNode
{
    /// <summary>The specification identifier every tax data document declares (TDT-001).</summary>
    public const string CustomizationId = "urn:peppol:taxdata:sk-1";

    /// <summary>The business process every tax data document declares (TDT-002).</summary>
    public const string ProfileId = "urn:peppol:taxreporting";

    /// <summary>This document's own identifier (TDT-003), which is not the invoice's.</summary>
    public string Uuid { get; set; } = string.Empty;

    /// <summary>When it was issued (TDT-004 and TDT-005), with the offset the rules require on the time.</summary>
    public DateTimeOffset IssuedAt { get; set; }

    /// <summary>What is being reported (TDT-007). See <see cref="SkTaxDataCodes.TaxDataTypes"/>.</summary>
    public string TaxDataTypeCode { get; set; } = string.Empty;

    /// <summary>How far the transaction reaches (TDT-006). See <see cref="SkTaxDataCodes.DocumentScopes"/>.</summary>
    public string DocumentScope { get; set; } = string.Empty;

    /// <summary>Which corner is reporting (TDT-012). See <see cref="SkTaxDataCodes.ReporterRoles"/>.</summary>
    public string ReporterRole { get; set; } = string.Empty;

    /// <summary>The authority being reported to (TDG-04).</summary>
    public SkTaxAuthority Authority { get; set; } = new();

    /// <summary>Who is reporting (TDG-05).</summary>
    public SkTaxDataEndpoint ReportingParty { get; set; } = new();

    /// <summary>Who receives the report (TDG-06).</summary>
    public SkTaxDataEndpoint ReceivingParty { get; set; } = new();

    /// <summary>The reporter's representative (TDG-07), when one is involved.</summary>
    public SkTaxDataEndpoint? ReportersRepresentative { get; set; }

    /// <summary>The invoice this reports (TDG-01 and TDG-02).</summary>
    public EInvoice? ReportedDocument { get; set; }

    /// <summary>The reported document's own identifier (TDT-017), which the rules require beside BT-1.</summary>
    public string ReportedDocumentUuid { get; set; } = string.Empty;
}
