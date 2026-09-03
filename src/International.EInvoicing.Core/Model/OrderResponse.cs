using International.EInvoicing.Diagnostics;
using International.EInvoicing.Profiles;
using International.EInvoicing.Values;

namespace International.EInvoicing.Model;

/// <summary>
/// The seller's answer to an order: accepted, rejected, or accepted with changes.
/// </summary>
/// <remarks>
/// Without it a buyer who has sent an order knows nothing until goods arrive or do not — the same gap on the
/// pre-award side that the Invoice Response closes after the invoice. What makes it more than a yes or no is
/// that a seller may accept a line on different terms: a different quantity, a later date, or a substitute
/// product altogether, each of which the buyer has to see before the goods turn up.
/// </remarks>
public sealed class OrderResponse : InvoiceNode
{
    /// <summary>The response's own number.</summary>
    public IdentifierField Number { get; set; }

    /// <summary>The seller's number for the order being answered.</summary>
    public IdentifierField SalesOrderNumber { get; set; }

    /// <summary>When the response was issued.</summary>
    public DateTimeField IssuedAt { get; set; }

    /// <summary>
    /// The answer itself, from UNCL 4343 as Peppol restricts it — accepted, rejected, or with changes.
    /// </summary>
    public CodeField ResponseCode { get; set; }

    /// <summary>A free-text note about the response as a whole.</summary>
    public TextField Note { get; set; }

    /// <summary>The currency the response is expressed in.</summary>
    public CodeField CurrencyCode { get; set; }

    /// <summary>The buyer's reference, carried back from the order.</summary>
    public TextField BuyerReference { get; set; }

    /// <summary>Which order is being answered.</summary>
    public IdentifierField OrderReference { get; set; }

    /// <summary>
    /// Which order <em>change</em> is being answered, when the response follows an amendment.
    /// </summary>
    /// <remarks>
    /// A buyer who has changed an order needs to know which version the seller answered; without this a
    /// response to the amendment is indistinguishable from a late response to the original.
    /// </remarks>
    public IdentifierField OrderChangeReference { get; set; }

    /// <summary>What the document claims to conform to.</summary>
    public ProfileIdentifier SpecificationIdentifier { get; set; }

    /// <summary>The business process this document takes part in.</summary>
    public IdentifierField BusinessProcessType { get; set; }

    /// <summary>Who placed the order.</summary>
    public Party? Buyer { get; set; }

    /// <summary>Who is answering it.</summary>
    public Party? Seller { get; set; }

    /// <summary>When the seller undertakes to deliver the order as a whole.</summary>
    public OrderDelivery? Delivery { get; set; }

    /// <summary>The answer line by line, when it differs from the answer as a whole.</summary>
    public List<OrderResponseLine> Lines { get; } = [];

    /// <summary>What was reported while this was read. Empty for a response built in code.</summary>
    public IReadOnlyList<Diagnostic> Diagnostics { get; set; } = [];

    /// <summary>How the declared specification identifier was resolved. <c>null</c> for one built in code.</summary>
    public ProfileResolution? Profile { get; set; }
}
