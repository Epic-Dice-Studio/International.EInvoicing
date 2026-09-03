using International.EInvoicing.Profiles;

namespace International.EInvoicing.Peppol;

/// <summary>
/// The two Peppol responses that are not invoices, both carried as a UBL <c>ApplicationResponse</c>.
/// </summary>
/// <remarks>
/// An Invoice Response is what a receiver owes a sender: the invoice is in process, accepted, rejected,
/// under query, or paid. A Message Level Response is one layer below it and answers a different question —
/// whether the message arrived and could be parsed at all. They are the same document with different code
/// lists, which is why one reader and one writer serve both.
/// </remarks>
public static class PeppolResponseProfiles
{
    /// <summary>Peppol BIS Invoice Response 3.0 — what happened to the invoice.</summary>
    public static Profile InvoiceResponse { get; } = new(
        new ProfileIdentifier("urn:fdc:peppol.eu:poacc:trns:invoice_response:3"),
        "Peppol BIS Invoice Response 3.0",
        DocumentSyntax.Ubl);

    /// <summary>Peppol Message Level Response 3.0 — whether the message itself arrived and parsed.</summary>
    public static Profile MessageLevelResponse { get; } = new(
        new ProfileIdentifier("urn:fdc:peppol.eu:poacc:trns:mlr:3"),
        "Peppol Message Level Response 3.0",
        DocumentSyntax.Ubl);

    /// <summary>Both, for registration.</summary>
    public static IReadOnlyList<Profile> All { get; } = [InvoiceResponse, MessageLevelResponse];

    /// <summary>
    /// The compiled rule set Peppol publishes for each profile, named as it is upstream.
    /// </summary>
    /// <remarks>
    /// <para>
    /// OpenPEPPOL generates the structural half of each rule set at build time and publishes only the
    /// compiled XSLT, which is why these are <c>.xslt</c> where the Billing ones are <c>.sch</c>. The
    /// compiled reader recovers the assertions, so they run like any other rule set.
    /// </para>
    /// <para>
    /// Each is tied to the profile it governs. The two transactions share a root element and differ in what
    /// they mean, so a rule set let loose on both reports an Invoice Response as a malformed Message Level
    /// Response — twelve failures on OpenPEPPOL's own example, none of them a defect in the document.
    /// </para>
    /// </remarks>
    public static IReadOnlyDictionary<string, Profile> RuleSets { get; } = new Dictionary<string, Profile>(StringComparer.Ordinal)
    {
        ["PEPPOLBIS-T111.xslt"] = InvoiceResponse,
        ["PEPPOLBIS-T71.xslt"] = MessageLevelResponse,
    };
}
