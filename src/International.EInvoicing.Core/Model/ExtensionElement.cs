using International.EInvoicing.Diagnostics;

namespace International.EInvoicing.Model;

/// <summary>
/// An element the reader did not map to the model, kept verbatim so nothing a document contained is ever
/// lost. Writers re-emit <see cref="Xml"/> unchanged.
/// </summary>
/// <param name="NamespaceUri">The element's namespace, empty when it has none.</param>
/// <param name="LocalName">The element's local name.</param>
/// <param name="Xml">The element and its content, exactly as they appeared.</param>
/// <param name="Location">Where it was found.</param>
public sealed record ExtensionElement(
    string NamespaceUri,
    string LocalName,
    string Xml,
    SourceLocation Location = default)
{
    /// <summary>The qualified name, as <c>{namespace}local</c> when there is a namespace.</summary>
    public string QualifiedName =>
        NamespaceUri.Length == 0 ? LocalName : $"{{{NamespaceUri}}}{LocalName}";

    /// <inheritdoc />
    public override string ToString() => QualifiedName;
}
