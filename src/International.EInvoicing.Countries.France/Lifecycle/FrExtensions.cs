using International.EInvoicing.Model;

namespace International.EInvoicing.Countries.France.Lifecycle;

/// <summary>
/// The French elements the generic CDAR model does not describe, written as extension data so they travel
/// with the message and land in the right place.
/// </summary>
/// <remarks>
/// These are national additions, not gaps in the model: the generic message has no notion of a dispute reason
/// list or of the profile echoed inside a reference. Keeping them as extension data is what lets the CDAR
/// package stay generic while France gets what it needs.
/// </remarks>
internal static class FrExtensions
{
    private static readonly string Ram = International.EInvoicing.Cdar.CdarNames.Ram.NamespaceName;

    public static ExtensionElement ReferenceTypeCode(string profileIdentifier) =>
        new(Ram, "ReferenceTypeCode", $"<ram:ReferenceTypeCode xmlns:ram=\"{Ram}\">{profileIdentifier}</ram:ReferenceTypeCode>");

    public static ExtensionElement DocumentStatus(string reasonCode, string reason) =>
        new(
            Ram,
            "SpecifiedDocumentStatus",
            $"<ram:SpecifiedDocumentStatus xmlns:ram=\"{Ram}\">"
            + $"<ram:ReasonCode>{Escape(reasonCode)}</ram:ReasonCode>"
            + $"<ram:Reason>{Escape(reason)}</ram:Reason>"
            + "</ram:SpecifiedDocumentStatus>");

    private static string Escape(string value) =>
        value.Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal);
}
