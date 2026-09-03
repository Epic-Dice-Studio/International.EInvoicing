namespace International.EInvoicing.Peppol;

/// <summary>
/// What a Peppol Invoice Response can say about an invoice: the UNCL 4343 subset Peppol allows.
/// </summary>
/// <remarks>
/// The full UNCL 4343 list is long and mostly about transport bookings. Peppol admits seven codes, and the
/// difference between two of them decides whether a supplier chases a payment: <c>AP</c> is the buyer's
/// final approval, <c>PD</c> says the money has been sent. A receiver that treats <c>IP</c> as acceptance
/// has told the supplier something the buyer did not say.
/// </remarks>
public static class PeppolResponseCodes
{
    /// <summary>The message arrived, is readable, and has been submitted for processing.</summary>
    public const string Acknowledged = "AB";

    /// <summary>The invoice is being processed. Nothing is promised yet.</summary>
    public const string InProcess = "IP";

    /// <summary>Something is unclear, and the buyer is asking. The invoice is not rejected.</summary>
    public const string UnderQuery = "UQ";

    /// <summary>Accepted, but subject to the condition the accompanying clarification states.</summary>
    public const string ConditionallyAccepted = "CA";

    /// <summary>Rejected: the buyer will not process this invoice any further.</summary>
    public const string Rejected = "RE";

    /// <summary>Approved. The next step is payment.</summary>
    public const string Approved = "AP";

    /// <summary>Paid — the payment has been initiated.</summary>
    public const string Paid = "PD";

    /// <summary>Every code the Invoice Response admits.</summary>
    public static IReadOnlyList<string> All { get; } =
    [
        Acknowledged,
        InProcess,
        UnderQuery,
        ConditionallyAccepted,
        Rejected,
        Approved,
        Paid,
    ];

    /// <summary>
    /// The codes that oblige the sender to say more, by <c>PEPPOL-T111-R001</c>.
    /// </summary>
    /// <remarks>
    /// Rejecting, querying or conditionally accepting an invoice without saying why leaves the supplier
    /// nothing to act on, so the rule makes a clarification mandatory for exactly these three.
    /// </remarks>
    public static IReadOnlyList<string> RequiringClarification { get; } =
    [
        ConditionallyAccepted,
        UnderQuery,
        Rejected,
    ];
}
