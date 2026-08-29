namespace International.EInvoicing.Diagnostics;

/// <summary>A starting point for a <see cref="DiagnosticPolicy"/>, before per-category or per-code overrides.</summary>
public enum DiagnosticPreset
{
    /// <summary>Descriptor defaults, unchanged. Reports everything the library noticed.</summary>
    Balanced,

    /// <summary>Drops what a caller reading only the EN 16931 core cannot act on: unmapped elements, unknown codes.</summary>
    Lenient,

    /// <summary>Anything the library could not fully interpret makes the document unusable.</summary>
    Strict,
}
