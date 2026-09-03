using System.Xml.Linq;
using International.EInvoicing.Cii.Reading;
using International.EInvoicing.Diagnostics;
using International.EInvoicing.Model;

namespace International.EInvoicing.OrderX.Reading;

/// <summary>Walking an Order-X document, and keeping what nobody mapped.</summary>
internal static class OrderXNodes
{
    /// <summary>The child by that name, marked as mapped.</summary>
    public static XElement? In(CiiValueReader values, XElement? parent, XName name)
    {
        XElement? child = parent?.Element(name);
        values.Consume(child);
        return child;
    }

    /// <summary>Every child by that name, all marked as mapped.</summary>
    public static List<XElement> AllIn(CiiValueReader values, XElement? parent, XName name)
    {
        List<XElement> children = [.. parent?.Elements(name) ?? []];
        foreach (XElement child in children)
        {
            values.Consume(child);
        }

        return children;
    }

    /// <summary>
    /// Walks the whole document and gives every element the reader did not map to the node that contained it,
    /// remembering the mapped sibling it followed.
    /// </summary>
    /// <remarks>
    /// The order of a Cross Industry Order's elements is normative, so where an unmapped element goes back is
    /// part of not losing it: kept but misplaced is rejected by the schema just as surely as dropped.
    /// </remarks>
    public static void KeepEverythingElse(
        XElement source,
        InvoiceNode node,
        HashSet<XElement> mapped,
        IReadOnlyDictionary<XElement, InvoiceNode> owners,
        DiagnosticCollector diagnostics)
    {
        string? preceding = null;

        foreach (XElement element in source.Elements())
        {
            if (mapped.Contains(element))
            {
                preceding = element.Name.ToString();
                KeepEverythingElse(
                    element,
                    owners.TryGetValue(element, out InvoiceNode? owner) ? owner : node,
                    mapped,
                    owners,
                    diagnostics);
                continue;
            }

            node.Extensions.Add(new ExtensionElement(
                element.Name.NamespaceName,
                element.Name.LocalName,
                element.ToString(SaveOptions.DisableFormatting),
                CiiValueReader.LocationOf(element),
                preceding));

            diagnostics.Add(Diagnostic.Create(OrderXDiagnostics.UnmappedElement, element.Name.LocalName) with
            {
                Location = CiiValueReader.LocationOf(element),
                Found = element.Name.LocalName,
                AppliedFallback = "kept verbatim as extension data",
            });
        }
    }
}
