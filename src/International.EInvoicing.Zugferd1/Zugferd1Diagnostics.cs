using International.EInvoicing.Diagnostics;

namespace International.EInvoicing.Zugferd1;

/// <summary>Diagnostics specific to reading ZUGFeRD 1.0.</summary>
public static class Zugferd1Diagnostics
{
    /// <summary>The document is not well-formed XML, so nothing could be read from it.</summary>
    public static DiagnosticDescriptor MalformedDocument { get; } = new(
        "EIV5005",
        DiagnosticCategory.Safety,
        DiagnosticSeverity.Fatal,
        "The document is not well-formed XML: {0}");

    /// <summary>An element was not mapped to the canonical model and was kept as extension data.</summary>
    public static DiagnosticDescriptor UnmappedElement { get; } = new(
        "EIV2023",
        DiagnosticCategory.UnmappedElement,
        DiagnosticSeverity.Info,
        "Element '{0}' is not part of the canonical model and was kept as extension data.");
}
