using System.Xml.Linq;

namespace International.EInvoicing.Ubl;

/// <summary>The names a UBL <c>Order</c> is built from.</summary>
public static class UblOrderNames
{
    /// <summary>The document namespace of an <c>Order</c>.</summary>
    public static XNamespace Order { get; } = "urn:oasis:names:specification:ubl:schema:xsd:Order-2";

    /// <summary>The root element's local name.</summary>
    public const string RootElement = "Order";
}
