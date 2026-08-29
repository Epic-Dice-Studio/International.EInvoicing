namespace International.EInvoicing.Model;

/// <summary>
/// Base of every node in the canonical model. Carries the extension data that lets a node keep whatever the
/// model does not describe, so reading a document never loses anything.
/// </summary>
public abstract class InvoiceNode
{
    /// <summary>Elements the reader could not map, kept verbatim and written back unchanged.</summary>
    public ExtensionData Extensions { get; } = [];
}
