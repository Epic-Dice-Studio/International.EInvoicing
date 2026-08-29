using System.Xml.Linq;

namespace International.EInvoicing.Cdar;

/// <summary>
/// The CDAR namespaces. Only the document namespace differs from CII: the components, data types and
/// qualified types are the same UN/CEFACT modules.
/// </summary>
public static class CdarNames
{
    /// <summary>The Cross Domain Acknowledgement and Response document namespace.</summary>
    public static XNamespace Rsm { get; } =
        "urn:un:unece:uncefact:data:standard:CrossDomainAcknowledgementAndResponse:100";

    /// <summary>Reusable aggregate business information entities.</summary>
    public static XNamespace Ram { get; } =
        "urn:un:unece:uncefact:data:standard:ReusableAggregateBusinessInformationEntity:100";

    /// <summary>Unqualified data types.</summary>
    public static XNamespace Udt { get; } =
        "urn:un:unece:uncefact:data:standard:UnqualifiedDataType:100";

    /// <summary>Qualified data types.</summary>
    public static XNamespace Qdt { get; } =
        "urn:un:unece:uncefact:data:standard:QualifiedDataType:100";

    internal const string RsmPrefix = "rsm";
    internal const string RamPrefix = "ram";
    internal const string UdtPrefix = "udt";
    internal const string QdtPrefix = "qdt";
}
