using System.Xml.Linq;

namespace International.EInvoicing.Ubl;

/// <summary>The names a UBL <c>OrderChange</c> is built from.</summary>
public static class UblOrderChangeNames
{
    /// <summary>The document namespace of an <c>OrderChange</c>.</summary>
    public static XNamespace OrderChange { get; } =
        "urn:oasis:names:specification:ubl:schema:xsd:OrderChange-2";

    /// <summary>The root element's local name.</summary>
    public const string RootElement = "OrderChange";
}
