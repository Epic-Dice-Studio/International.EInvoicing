using System.Xml.Linq;

namespace International.EInvoicing.Ubl;

/// <summary>
/// The UBL namespaces and the prefixes conventionally bound to them. Elements are always addressed by
/// namespace and local name: a document may bind any prefix it likes, and several <c>cbc:ID</c> elements mean
/// entirely different things depending on where they sit.
/// </summary>
public static class UblNames
{
    /// <summary>Common basic components — the namespace of leaf values.</summary>
    public static XNamespace Cbc { get; } =
        "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";

    /// <summary>Common aggregate components — the namespace of composite elements.</summary>
    public static XNamespace Cac { get; } =
        "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";

    /// <summary>The Invoice document namespace.</summary>
    public static XNamespace Invoice { get; } =
        "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2";

    /// <summary>The CreditNote document namespace.</summary>
    public static XNamespace CreditNote { get; } =
        "urn:oasis:names:specification:ubl:schema:xsd:CreditNote-2";

    /// <summary>UBL extensions, which carry syntax-level extension content.</summary>
    public static XNamespace Ext { get; } =
        "urn:oasis:names:specification:ubl:schema:xsd:CommonExtensionComponents-2";

    internal const string CbcPrefix = "cbc";
    internal const string CacPrefix = "cac";
}
