using International.EInvoicing.Diagnostics;

namespace International.EInvoicing;

/// <summary>Diagnostics raised by the top-level entry point.</summary>
public static class EInvoicingDiagnostics
{
    /// <summary>The document is not one this library recognises.</summary>
    public static DiagnosticDescriptor UnrecognisedDocument { get; } = new(
        "EIV5010",
        DiagnosticCategory.Safety,
        DiagnosticSeverity.Fatal,
        "This is not a document this library recognises: neither UBL, nor CII, nor a lifecycle message.");
}
