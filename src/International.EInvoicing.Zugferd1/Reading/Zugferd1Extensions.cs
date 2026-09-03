using System.Xml.Linq;
using International.EInvoicing.Cii.Reading;
using International.EInvoicing.Diagnostics;
using International.EInvoicing.Model;

namespace International.EInvoicing.Zugferd1.Reading;

/// <summary>Keeping what the reader did not map, so that nothing a 2013 document carried is lost.</summary>
internal static class Zugferd1Extensions
{
    /// <summary>
    /// Walks the whole document and gives every element the reader did not map to the node that contained it,
    /// remembering where it sat.
    /// </summary>
    /// <remarks>
    /// This matters more here than anywhere else in the library. Reading an archive is the only reason to
    /// read ZUGFeRD 1.0 at all, and an archive is read to find out what a document said — including the
    /// parts EN 16931 never had a term for, such as the German <c>Bankleitzahl</c>.
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
                preceding,
                source.Name.ToString()));

            diagnostics.Add(Diagnostic.Create(Zugferd1Diagnostics.UnmappedElement, element.Name.LocalName) with
            {
                Location = CiiValueReader.LocationOf(element),
                Found = element.Name.LocalName,
                AppliedFallback = "kept verbatim as extension data",
            });
        }
    }
}
