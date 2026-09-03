using International.EInvoicing.Profiles;

namespace International.EInvoicing.Peppol;

/// <summary>
/// The Peppol post-award documents that are not invoices.
/// </summary>
/// <remarks>
/// An Invoice Response is what a receiver owes a sender: the invoice is in process, accepted, rejected,
/// under query, or paid. A Message Level Response is one layer below it and answers a different question —
/// whether the message arrived and could be parsed at all. Those two are the same document with different
/// code lists, which is why one reader and one writer serve both. A Despatch Advice is a different document
/// altogether: what actually left the warehouse, which is what an invoice is reconciled against.
/// </remarks>
public static class PeppolPostAwardProfiles
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

    /// <summary>Peppol BIS Despatch Advice 3.0 — what was actually sent, against what was ordered.</summary>
    public static Profile DespatchAdvice { get; } = new(
        new ProfileIdentifier("urn:fdc:peppol.eu:poacc:trns:despatch_advice:3"),
        "Peppol BIS Despatch Advice 3.0",
        DocumentSyntax.Ubl);

    /// <summary>Peppol BIS Ordering 3.0 — what the buyer asked for.</summary>
    public static Profile Order { get; } = new(
        new ProfileIdentifier("urn:fdc:peppol.eu:poacc:trns:order:3"),
        "Peppol BIS Order 3.0",
        DocumentSyntax.Ubl);

    /// <summary>Peppol BIS Ordering 3.0 — the seller's answer to the order.</summary>
    public static Profile OrderResponse { get; } = new(
        new ProfileIdentifier("urn:fdc:peppol.eu:poacc:trns:order_response:3"),
        "Peppol BIS Order Response 3.0",
        DocumentSyntax.Ubl);

    /// <summary>
    /// Peppol BIS Ordering 3.0 — the seller's answer, restating the whole order as agreed.
    /// </summary>
    /// <remarks>
    /// The same <c>OrderResponse</c> document as <see cref="OrderResponse"/> under two further profiles: the
    /// advanced response, which answers line by line with everything the order carried, and the order
    /// agreement, which is that answer treated as the contract. One document, three meanings.
    /// </remarks>
    public static Profile OrderResponseAdvanced { get; } = new(
        new ProfileIdentifier("urn:fdc:peppol.eu:poacc:trns:order_response_advanced:3"),
        "Peppol BIS Order Response (advanced) 3.0",
        DocumentSyntax.Ubl);

    /// <inheritdoc cref="OrderResponseAdvanced"/>
    public static Profile OrderAgreement { get; } = new(
        new ProfileIdentifier("urn:fdc:peppol.eu:poacc:trns:order_agreement:3"),
        "Peppol BIS Order Agreement 3.0",
        DocumentSyntax.Ubl);

    /// <summary>Peppol BIS Ordering 3.0 — the buyer withdrawing an order.</summary>
    public static Profile OrderCancellation { get; } = new(
        new ProfileIdentifier("urn:fdc:peppol.eu:poacc:trns:order_cancellation:3"),
        "Peppol BIS Order Cancellation 3.0",
        DocumentSyntax.Ubl);

    /// <summary>Peppol BIS Ordering 3.0 — the buyer amending an order already sent.</summary>
    public static Profile OrderChange { get; } = new(
        new ProfileIdentifier("urn:fdc:peppol.eu:poacc:trns:order_change:3"),
        "Peppol BIS Order Change 3.0",
        DocumentSyntax.Ubl);

    /// <summary>Every post-award profile this package registers.</summary>
    public static IReadOnlyList<Profile> All { get; } =
    [
        InvoiceResponse,
        MessageLevelResponse,
        DespatchAdvice,
        Order,
        OrderResponse,
        OrderResponseAdvanced,
        OrderAgreement,
        OrderCancellation,
        OrderChange,
    ];

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
        ["PEPPOLBIS-T16.xslt"] = DespatchAdvice,
        ["PEPPOLBIS-T01.xslt"] = Order,
        ["PEPPOLBIS-T76.xslt"] = OrderResponse,
        ["PEPPOLBIS-T110.xslt"] = OrderResponseAdvanced,
        ["PEPPOLBIS-T116.xslt"] = OrderAgreement,
        ["PEPPOLBIS-T115.xslt"] = OrderCancellation,
        ["PEPPOLBIS-T114.xslt"] = OrderChange,
    };
}
