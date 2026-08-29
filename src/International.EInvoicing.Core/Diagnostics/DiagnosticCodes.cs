namespace International.EInvoicing.Diagnostics;

/// <summary>
/// Every diagnostic this library can emit. Each code has a page in <c>docs/diagnostics/</c>, and CI fails
/// when one does not.
/// </summary>
public static class DiagnosticCodes
{
    /// <summary>A value could not be interpreted as its declared type; the raw text is preserved.</summary>
    public static DiagnosticDescriptor InvalidValue { get; } = new(
        "EIV2001",
        DiagnosticCategory.InvalidValue,
        DiagnosticSeverity.Warning,
        "The value '{0}' could not be read as {1}.");

    /// <summary>A date uses a legal format code this library does not turn into a typed value.</summary>
    public static DiagnosticDescriptor UnsupportedDateFormat { get; } = new(
        "EIV2002",
        DiagnosticCategory.InvalidValue,
        DiagnosticSeverity.Info,
        "Date format code '{0}' is valid but not converted to a typed value.");
}
