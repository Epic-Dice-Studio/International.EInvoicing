namespace International.EInvoicing.Countries.Netherlands.Identifiers;

/// <summary>
/// The two schemes a Dutch legal entity identifier may be declared under.
/// </summary>
/// <remarks>
/// <para>
/// <c>NL-R-003</c> and <c>NL-R-005</c> are fatal: when the supplier is Dutch, both parties' legal entity
/// identifiers must carry scheme <c>0106</c> (KvK) or <c>0190</c> (OIN). An invoice that names the company
/// correctly and omits the scheme is refused, which is the trap this exists to keep a caller out of.
/// </para>
/// <para>
/// The shape of the numbers themselves is not checked here. Peppol does not check it either, and a library
/// that is stricter than the network it writes for rejects invoices the recipient would have accepted.
/// </para>
/// </remarks>
public static class NlLegalIdentifier
{
    /// <summary>The Chamber of Commerce number — <em>KvK-nummer</em>.</summary>
    public const string Kvk = "0106";

    /// <summary>The organisation identification number used by Dutch public bodies — <em>OIN</em>.</summary>
    public const string Oin = "0190";

    /// <summary>Whether a scheme identifier is one the Dutch rules accept on a legal entity.</summary>
    public static bool IsAccepted(string? scheme) =>
        string.Equals(scheme, Kvk, StringComparison.Ordinal)
        || string.Equals(scheme, Oin, StringComparison.Ordinal);
}
