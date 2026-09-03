using System.Xml.Linq;
using International.EInvoicing.Diagnostics;
using International.EInvoicing.Model;

namespace International.EInvoicing.Ubl.Reading;

/// <summary>Keeping what a reader did not map, so that nothing a document carried is lost.</summary>
internal static class UblExtensions
{
    /// <summary>
    /// Walks the whole document and gives every element the reader did not map to the node that contained
    /// it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Doing this once at the end, rather than inside each mapping method, is what makes the guarantee
    /// total: an element nobody thought about is still kept, wherever it sits.
    /// </para>
    /// <para>
    /// Each one also remembers the mapped sibling it followed, so a writer can put it back there rather than
    /// at the end of the node. Element order is normative, so where it goes is part of not losing it.
    /// </para>
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

                // Descend with the node that owns this element, when one exists, so what it contains is kept
                // where it belongs and can be written back inside it.
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
                UblValueReader.LocationOf(element),
                preceding));

            diagnostics.Add(Diagnostic.Create(UblDiagnostics.UnmappedElement, element.Name.LocalName) with
            {
                Location = UblValueReader.LocationOf(element),
                Found = element.Name.LocalName,
                AppliedFallback = "kept verbatim as extension data",
            });
        }
    }
}
