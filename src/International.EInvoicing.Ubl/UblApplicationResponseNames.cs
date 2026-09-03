using System.Xml.Linq;

namespace International.EInvoicing.Ubl;

/// <summary>The names a UBL <c>ApplicationResponse</c> is built from.</summary>
public static class UblApplicationResponseNames
{
    /// <summary>The document namespace of an <c>ApplicationResponse</c>.</summary>
    public static XNamespace ApplicationResponse { get; } =
        "urn:oasis:names:specification:ubl:schema:xsd:ApplicationResponse-2";

    /// <summary>The root element's local name.</summary>
    public const string RootElement = "ApplicationResponse";

    /// <summary>
    /// The code list that marks a status as what the sender wants done rather than why the status was set.
    /// </summary>
    /// <remarks>
    /// UBL repeats <c>cac:Status</c> instead of nesting a reason and an action, so the <c>listID</c> is the
    /// only thing that tells them apart. A status with no list is read as a reason, which is what every
    /// document in Peppol's own corpus means by it.
    /// </remarks>
    public const string ActionCodeList = "OPStatusAction";
}
