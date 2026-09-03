using International.EInvoicing.Diagnostics;

namespace International.EInvoicing.Model;

/// <summary>
/// An element the reader did not map to the model, kept verbatim so nothing a document contained is ever
/// lost. Writers re-emit <see cref="Xml"/> unchanged, inside the element it sat in and after the sibling it
/// followed.
/// </summary>
/// <remarks>
/// <para>
/// Keeping the content is only half of losing nothing. Element order is normative in UBL, in CII and in
/// CDAR, so an element written back in the wrong place is one a receiver's schema rejects — which is why
/// where it sat is recorded and not only what it said.
/// </para>
/// <para>
/// It takes both halves of the address. A model node does not always match one XML element: a CII invoice
/// fills itself from the document context, the exchanged document and three header sections, and
/// <c>ram:ID</c> appears in most of them. The sibling alone would put an extension after the first
/// <c>ram:ID</c> written anywhere in the node; the parent says which one.
/// </para>
/// </remarks>
/// <param name="NamespaceUri">The element's namespace, empty when it has none.</param>
/// <param name="LocalName">The element's local name.</param>
/// <param name="Xml">The element and its content, exactly as they appeared.</param>
/// <param name="Location">Where it was found.</param>
/// <param name="PrecedingSibling">
/// The qualified name of the mapped element this one followed, so a writer can put it back there.
/// <c>null</c> when it came first among its siblings.
/// </param>
/// <param name="ParentName">
/// The qualified name of the element this one sat in. <c>null</c> when the reader did not record it, in
/// which case a writer matches on the sibling alone.
/// </param>
public sealed record ExtensionElement(
    string NamespaceUri,
    string LocalName,
    string Xml,
    SourceLocation Location = default,
    string? PrecedingSibling = null,
    string? ParentName = null)
{
    /// <summary>The qualified name, as <c>{namespace}local</c> when there is a namespace.</summary>
    public string QualifiedName =>
        NamespaceUri.Length == 0 ? LocalName : $"{{{NamespaceUri}}}{LocalName}";

    /// <inheritdoc />
    public override string ToString() => QualifiedName;
}
