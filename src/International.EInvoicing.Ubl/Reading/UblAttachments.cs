using System.Xml.Linq;
using International.EInvoicing.Model;
using International.EInvoicing.Xml;

namespace International.EInvoicing.Ubl.Reading;

/// <summary>
/// A document sent alongside another, attached or referenced.
/// </summary>
/// <remarks>
/// UBL states it the same way wherever it appears — <c>cac:AdditionalDocumentReference</c> on an order,
/// <c>cac:DocumentReference</c> on a despatch line — so it is read the same way, into the same model.
/// </remarks>
internal static class UblAttachments
{
    public static AdditionalDocument Read(
        XElement element,
        UblValueReader values,
        HashSet<XElement> mapped,
        Dictionary<XElement, InvoiceNode> owners,
        DocumentLimits limits)
    {
        var document = new AdditionalDocument
        {
            Identifier = values.ReadIdentifier(Take(element, UblNames.Cbc + "ID", mapped)),
            Description = values.ReadText(Take(element, UblNames.Cbc + "DocumentType", mapped)),
        };

        owners[element] = document;

        if (Take(element, UblNames.Cac + "Attachment", mapped) is { } attachment)
        {
            owners[attachment] = document;
            document.Attachment = values.ReadBinary(
                Take(attachment, UblNames.Cbc + "EmbeddedDocumentBinaryObject", mapped),
                limits);

            if (Take(attachment, UblNames.Cac + "ExternalReference", mapped) is { } external)
            {
                owners[external] = document;
                document.ExternalLocation = values.ReadText(Take(external, UblNames.Cbc + "URI", mapped));
            }
        }

        return document;
    }

    private static XElement? Take(XElement parent, XName name, HashSet<XElement> mapped)
    {
        XElement? element = parent.Element(name);
        if (element is not null)
        {
            mapped.Add(element);
        }

        return element;
    }
}
