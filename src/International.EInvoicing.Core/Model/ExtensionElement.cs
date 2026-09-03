using International.EInvoicing.Diagnostics;

namespace International.EInvoicing.Model;

/// <summary>
/// An element the reader did not map to the model, kept verbatim so nothing a document contained is ever
/// lost. Writers re-emit <see cref="Xml"/> unchanged, after the sibling it followed.
/// </summary>
/// <remarks>
/// Keeping the content is only half of losing nothing. Element order is normative in UBL and in CII, so an
/// element written back in the wrong place is one a receiver's schema rejects — which is why
/// <see cref="PrecedingSibling"/> is recorded and not only the content.
/// </remarks>
/// <param name="NamespaceUri">The element's namespace, empty when it has none.</param>
/// <param name="LocalName">The element's local name.</param>
/// <param name="Xml">The element and its content, exactly as they appeared.</param>
/// <param name="Location">Where it was found.</param>
/// <param name="PrecedingSibling">
/// The qualified name of the mapped element this one followed, so a writer can put it back there.
/// <c>null</c> when it came first among its siblings.
/// </param>
public sealed record ExtensionElement(
    string NamespaceUri,
    string LocalName,
    string Xml,
    SourceLocation Location = default,
    string? PrecedingSibling = null)
{
    /// <summary>The qualified name, as <c>{namespace}local</c> when there is a namespace.</summary>
    public string QualifiedName =>
        NamespaceUri.Length == 0 ? LocalName : $"{{{NamespaceUri}}}{LocalName}";

    /// <inheritdoc />
    public override string ToString() => QualifiedName;
}
