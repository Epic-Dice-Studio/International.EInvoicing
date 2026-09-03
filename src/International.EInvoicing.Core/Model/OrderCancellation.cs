using International.EInvoicing.Diagnostics;
using International.EInvoicing.Profiles;
using International.EInvoicing.Values;

namespace International.EInvoicing.Model;

/// <summary>
/// The buyer withdrawing an order, and saying why.
/// </summary>
/// <remarks>
/// The smallest document of the ordering family and the one with the least room for doubt: it names an
/// order and revokes it. The reason is not decoration — a cancellation the seller cannot explain to their
/// warehouse is one they will query rather than act on, which is why <see cref="Reason"/> is mandatory in
/// the transaction rather than optional.
/// </remarks>
public sealed class OrderCancellation : InvoiceNode
{
    /// <summary>The cancellation's own number.</summary>
    public IdentifierField Number { get; set; }

    /// <summary>When it was issued.</summary>
    public DateTimeField IssuedAt { get; set; }

    /// <summary>A free-text note about the cancellation.</summary>
    public TextField Note { get; set; }

    /// <summary>Why the order is being withdrawn.</summary>
    public TextField Reason { get; set; }

    /// <summary>Which order is being withdrawn.</summary>
    public IdentifierField OrderReference { get; set; }

    /// <summary>The originator's own document reference.</summary>
    public IdentifierField OriginatorReference { get; set; }

    /// <summary>The contract the order was placed under.</summary>
    public IdentifierField ContractReference { get; set; }

    /// <summary>What the document claims to conform to.</summary>
    public ProfileIdentifier SpecificationIdentifier { get; set; }

    /// <summary>The business process this document takes part in.</summary>
    public IdentifierField BusinessProcessType { get; set; }

    /// <summary>Who placed the order.</summary>
    public Party? Buyer { get; set; }

    /// <summary>Who it was placed with.</summary>
    public Party? Seller { get; set; }

    /// <summary>Who asked for the order, when a third party did.</summary>
    public Party? Originator { get; set; }

    /// <summary>Documents sent with the cancellation.</summary>
    public List<AdditionalDocument> AdditionalDocuments { get; } = [];

    /// <summary>What was reported while this was read. Empty for one built in code.</summary>
    public IReadOnlyList<Diagnostic> Diagnostics { get; set; } = [];

    /// <summary>How the declared specification identifier was resolved. <c>null</c> for one built in code.</summary>
    public ProfileResolution? Profile { get; set; }
}
