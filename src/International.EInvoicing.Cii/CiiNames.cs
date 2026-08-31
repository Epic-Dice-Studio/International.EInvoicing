using System.Xml.Linq;

namespace International.EInvoicing.Cii;

/// <summary>
/// The CII namespaces and the prefixes conventionally bound to them. Prefixes are a convention, not a
/// requirement: elements are always addressed by namespace and local name.
/// </summary>
public static class CiiNames
{
    /// <summary>The Cross Industry Invoice document namespace.</summary>
    public static XNamespace Rsm { get; } =
        "urn:un:unece:uncefact:data:standard:CrossIndustryInvoice:100";

    /// <summary>Reusable aggregate business information entities — where nearly everything lives.</summary>
    public static XNamespace Ram { get; } =
        "urn:un:unece:uncefact:data:standard:ReusableAggregateBusinessInformationEntity:100";

    /// <summary>Unqualified data types: the types the field system mirrors.</summary>
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

/// <summary>
/// How CII distinguishes payment accounts. EN 16931 has one term for the account identifier (BT-84); CII
/// splits it into two elements, so the distinction is carried in the identifier's scheme.
/// </summary>
public static class CreditTransferSchemes
{
    /// <summary>An account identified by something other than an IBAN.</summary>
    public const string Proprietary = "proprietary";

}
