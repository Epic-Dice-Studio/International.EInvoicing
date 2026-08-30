using International.EInvoicing.Model;

namespace International.EInvoicing.Countries.France.Lifecycle;

/// <summary>
/// The French elements the generic CDAR model does not describe, written as extension data so they travel
/// with the message and land in the right place.
/// </summary>
/// <remarks>
/// This is a national addition, not a gap in the model: the generic message has no notion of the profile
/// echoed inside a reference. Keeping it as extension data is what lets the CDAR package stay generic while
/// France gets what it needs.
/// </remarks>
internal static class FrExtensions
{
    private static readonly string Ram = International.EInvoicing.Cdar.CdarNames.Ram.NamespaceName;

    public static ExtensionElement ReferenceTypeCode(string profileIdentifier) =>
        new(Ram, "ReferenceTypeCode", $"<ram:ReferenceTypeCode xmlns:ram=\"{Ram}\">{profileIdentifier}</ram:ReferenceTypeCode>");

}
