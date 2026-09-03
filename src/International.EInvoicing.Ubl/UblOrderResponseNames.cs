using System.Xml.Linq;

namespace International.EInvoicing.Ubl;

/// <summary>The names a UBL <c>OrderResponse</c> is built from.</summary>
public static class UblOrderResponseNames
{
    /// <summary>The document namespace of an <c>OrderResponse</c>.</summary>
    public static XNamespace OrderResponse { get; } =
        "urn:oasis:names:specification:ubl:schema:xsd:OrderResponse-2";

    /// <summary>The root element's local name.</summary>
    public const string RootElement = "OrderResponse";
}
