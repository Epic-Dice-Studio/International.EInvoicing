using International.EInvoicing.Diagnostics;

namespace International.EInvoicing.FacturX;

/// <summary>Diagnostics specific to hybrid Factur-X and ZUGFeRD invoices.</summary>
public static class FacturXDiagnostics
{
    /// <summary>The PDF carries no invoice payload, so there is nothing structured to read.</summary>
    public static DiagnosticDescriptor MissingPayload { get; } = new(
        "EIV4001",
        DiagnosticCategory.Container,
        DiagnosticSeverity.Fatal,
        "The PDF carries no embedded invoice: none of {0} was found.");

    /// <summary>The embedded payload is larger than the configured limit and was not decoded.</summary>
    public static DiagnosticDescriptor PayloadTooLarge { get; } = new(
        "EIV4002",
        DiagnosticCategory.Safety,
        DiagnosticSeverity.Fatal,
        "The embedded invoice is larger than the {0} byte limit and was not decoded.");

    /// <summary>
    /// The document declares a profile that is not a complete EN 16931 invoice. Reading succeeds; the caller
    /// is told because the legal usefulness of such a document is narrow.
    /// </summary>
    public static DiagnosticDescriptor ProfileIsNotAnEn16931Invoice { get; } = new(
        "EIV4010",
        DiagnosticCategory.UnsupportedProfile,
        DiagnosticSeverity.Warning,
        "Profile '{0}' carries header data and totals but not the lines EN 16931 requires of an invoice.");
}
