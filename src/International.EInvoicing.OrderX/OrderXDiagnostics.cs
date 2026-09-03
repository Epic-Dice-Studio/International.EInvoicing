using International.EInvoicing.Diagnostics;

namespace International.EInvoicing.OrderX;

/// <summary>Diagnostics specific to reading and writing Order-X.</summary>
public static class OrderXDiagnostics
{
    /// <summary>The document is not well-formed XML, so nothing could be read from it.</summary>
    public static DiagnosticDescriptor MalformedDocument { get; } = new(
        "EIV5004",
        DiagnosticCategory.Safety,
        DiagnosticSeverity.Fatal,
        "The document is not well-formed XML: {0}");

    /// <summary>An element was not mapped to the canonical model and was kept as extension data.</summary>
    public static DiagnosticDescriptor UnmappedElement { get; } = new(
        "EIV2021",
        DiagnosticCategory.UnmappedElement,
        DiagnosticSeverity.Info,
        "Element '{0}' is not part of the canonical model and was kept as extension data.");

    /// <summary>
    /// The document's type code is not one of the three Order-X carries, so which document it is had to be
    /// assumed.
    /// </summary>
    /// <remarks>
    /// All three Order-X documents share a root element, so the type code is the only thing that tells them
    /// apart. Reading on regardless is right — the content is still an order — but saying so is what stops a
    /// caller trusting a document kind nobody declared.
    /// </remarks>
    public static DiagnosticDescriptor UnknownDocumentType { get; } = new(
        "EIV2022",
        DiagnosticCategory.UnmappedElement,
        DiagnosticSeverity.Warning,
        "Type code '{0}' is not an Order-X order (220), order change (230) or order response (231).");
}
