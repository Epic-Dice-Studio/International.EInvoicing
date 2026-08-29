using System.Collections;

namespace International.EInvoicing.Model;

/// <summary>
/// Everything a node carried that the model does not describe: elements from an unsupported profile, a
/// partner's private extension, a syntax we mapped only partially.
/// </summary>
/// <remarks>
/// This is the safety net behind the promise that reading a document loses nothing. Whatever ends up here is
/// still written back, and is still reachable by callers who know what to do with it.
/// </remarks>
public sealed class ExtensionData : IReadOnlyList<ExtensionElement>
{
    private readonly List<ExtensionElement> _elements = [];

    /// <summary>How many elements were kept.</summary>
    public int Count => _elements.Count;

    /// <summary>Whether anything was kept.</summary>
    public bool IsEmpty => _elements.Count == 0;

    /// <summary>The element at <paramref name="index"/>.</summary>
    public ExtensionElement this[int index] => _elements[index];

    /// <summary>Keeps an element the reader could not map.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="element"/> is <c>null</c>.</exception>
    public void Add(ExtensionElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        _elements.Add(element);
    }

    /// <summary>The kept elements with the given namespace and local name.</summary>
    public IEnumerable<ExtensionElement> Named(string namespaceUri, string localName) =>
        _elements.Where(e =>
            string.Equals(e.NamespaceUri, namespaceUri, StringComparison.Ordinal)
            && string.Equals(e.LocalName, localName, StringComparison.Ordinal));

    /// <inheritdoc />
    public IEnumerator<ExtensionElement> GetEnumerator() => _elements.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
