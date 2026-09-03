using System.Xml.Linq;
using International.EInvoicing.Model;
using International.EInvoicing.Ubl.Reading;
using International.EInvoicing.Ubl.Writing;
using International.EInvoicing.Values;

namespace International.EInvoicing.Ubl;

/// <summary>
/// An item classification, in UBL.
/// </summary>
/// <remarks>
/// UBL puts the code and its name in one element: the code is the content and the name is the <c>name</c>
/// attribute. Every UBL document that carries an item carries them the same way, so reading and writing them
/// lives here rather than three times over.
/// </remarks>
internal static class UblClassification
{
    public static ItemClassification Read(XElement? element, UblValueReader values) =>
        new()
        {
            Code = values.ReadCode(element),
            Name = element?.Attribute("name") is { } name ? new TextField(name.Value) : TextField.Unset,
        };

    public static void Write(UblDocument writer, ItemClassification classification)
    {
        writer.Code("ItemClassificationCode", classification.Code, ("name", classification.Name.Value));
    }
}
