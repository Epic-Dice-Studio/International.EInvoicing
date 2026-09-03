using System.Xml.Linq;

namespace International.EInvoicing.Ubl;

/// <summary>The names a UBL <c>OrderCancellation</c> is built from.</summary>
public static class UblOrderCancellationNames
{
    /// <summary>The document namespace of an <c>OrderCancellation</c>.</summary>
    public static XNamespace OrderCancellation { get; } =
        "urn:oasis:names:specification:ubl:schema:xsd:OrderCancellation-2";

    /// <summary>The root element's local name.</summary>
    public const string RootElement = "OrderCancellation";
}
