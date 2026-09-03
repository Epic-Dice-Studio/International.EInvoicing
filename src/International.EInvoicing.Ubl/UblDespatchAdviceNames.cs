using System.Xml.Linq;

namespace International.EInvoicing.Ubl;

/// <summary>The names a UBL <c>DespatchAdvice</c> is built from.</summary>
public static class UblDespatchAdviceNames
{
    /// <summary>The document namespace of a <c>DespatchAdvice</c>.</summary>
    public static XNamespace DespatchAdvice { get; } =
        "urn:oasis:names:specification:ubl:schema:xsd:DespatchAdvice-2";

    /// <summary>The root element's local name.</summary>
    public const string RootElement = "DespatchAdvice";
}
