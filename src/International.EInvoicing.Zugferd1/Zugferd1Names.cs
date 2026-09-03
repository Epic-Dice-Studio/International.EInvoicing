using System.Xml.Linq;

namespace International.EInvoicing.Zugferd1;

/// <summary>
/// The ZUGFeRD 1.0 namespaces and the names it gives things.
/// </summary>
/// <remarks>
/// <para>
/// ZUGFeRD 1.0 is CII, from before CII settled. The vocabulary is recognisably the same — <c>ram</c>,
/// <c>udt</c>, trade parties and monetary summations — but the version of the data types is 12 and 15 rather
/// than 100, the document namespace is FeRD's own, and the sections have longer names: a transaction is
/// <em>Specified</em>, a header agreement is <em>ApplicableSupplyChain</em> rather than
/// <em>ApplicableHeader</em>.
/// </para>
/// <para>
/// Which is why this is a reader of its own and not the CII reader with a flag: nearly every element in the
/// document is a different <see cref="XName"/>.
/// </para>
/// </remarks>
public static class Zugferd1Names
{
    /// <summary>The document namespace, which is FeRD's rather than UN/CEFACT's.</summary>
    public static XNamespace Rsm { get; } = "urn:ferd:CrossIndustryDocument:invoice:1p0";

    /// <summary>Reusable aggregate business information entities, version 12.</summary>
    public static XNamespace Ram { get; } =
        "urn:un:unece:uncefact:data:standard:ReusableAggregateBusinessInformationEntity:12";

    /// <summary>Unqualified data types, version 15.</summary>
    public static XNamespace Udt { get; } = "urn:un:unece:uncefact:data:standard:UnqualifiedDataType:15";

    /// <summary>Qualified data types, version 12.</summary>
    public static XNamespace Qdt { get; } = "urn:un:unece:uncefact:data:standard:QualifiedDataType:12";

    /// <summary>The document element.</summary>
    public static XName Root { get; } = Rsm + "CrossIndustryDocument";
}
