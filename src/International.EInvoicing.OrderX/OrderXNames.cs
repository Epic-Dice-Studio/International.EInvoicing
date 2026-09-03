using System.Xml.Linq;

namespace International.EInvoicing.OrderX;

/// <summary>
/// The Order-X namespaces. Order-X is CII, but not the Cross Industry Invoice: it is the Cross Industry
/// Order, a different UN/CEFACT message on a later version of the same data types.
/// </summary>
/// <remarks>
/// This is the whole reason Order-X is not read by the invoice reader with a different root name. The
/// document namespace differs, and so does the version of <c>ram</c>, <c>udt</c> and <c>qdt</c> — 128 rather
/// than 100 — so every element in the document is a different <see cref="XName"/>.
/// </remarks>
public static class OrderXNames
{
    /// <summary>The Cross Industry Order document namespace.</summary>
    public static XNamespace Rsm { get; } =
        "urn:un:unece:uncefact:data:SCRDMCCBDACIOMessageStructure:100";

    /// <summary>Reusable aggregate business information entities, version 128.</summary>
    public static XNamespace Ram { get; } =
        "urn:un:unece:uncefact:data:standard:ReusableAggregateBusinessInformationEntity:128";

    /// <summary>Unqualified data types, version 128.</summary>
    public static XNamespace Udt { get; } =
        "urn:un:unece:uncefact:data:standard:UnqualifiedDataType:128";

    /// <summary>Qualified data types, version 128.</summary>
    public static XNamespace Qdt { get; } =
        "urn:un:unece:uncefact:data:standard:QualifiedDataType:128";

    /// <summary>The document element every Order-X document has, whichever of the three it is.</summary>
    public static XName Root { get; } = Rsm + "SCRDMCCBDACIOMessageStructure";

    internal const string RsmPrefix = "rsm";
    internal const string RamPrefix = "ram";
    internal const string UdtPrefix = "udt";
    internal const string QdtPrefix = "qdt";
}
