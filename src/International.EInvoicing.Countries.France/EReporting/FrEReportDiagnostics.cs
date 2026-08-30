using International.EInvoicing.Diagnostics;

namespace International.EInvoicing.Countries.France.EReporting;

/// <summary>Diagnostics reported while reading an e-reporting transmission.</summary>
public static class FrEReportDiagnostics
{
    /// <summary>The document is not well-formed XML, so nothing could be read from it.</summary>
    public static DiagnosticDescriptor MalformedDocument { get; } = new(
        "EIV5001",
        DiagnosticCategory.Safety,
        DiagnosticSeverity.Fatal,
        "The document is not well-formed XML: {0}");

    /// <summary>An element was not mapped to the model and was kept as extension data.</summary>
    public static DiagnosticDescriptor UnmappedElement { get; } = new(
        "EIV2020",
        DiagnosticCategory.UnmappedElement,
        DiagnosticSeverity.Info,
        "Element '{0}' is not part of the e-reporting model and was kept as extension data.");
}
