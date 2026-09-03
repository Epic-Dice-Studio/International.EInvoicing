using International.EInvoicing.Diagnostics;
using International.EInvoicing.Profiles;
using International.EInvoicing.Values;

namespace International.EInvoicing.Model;

/// <summary>
/// A despatch advice: what was actually sent, against what was ordered.
/// </summary>
/// <remarks>
/// It is the document an invoice is reconciled against. An invoice says what is owed and an order says what
/// was asked for; only this says what left the warehouse — which is why a buyer who receives ten of an
/// ordered twelve needs it to know the invoice for ten is right.
/// </remarks>
public sealed class DespatchAdvice : InvoiceNode
{
    /// <summary>The despatch advice number.</summary>
    public IdentifierField Number { get; set; }

    /// <summary>When it was issued.</summary>
    public DateTimeField IssuedAt { get; set; }

    /// <summary>What kind of despatch advice this is, when the sender says.</summary>
    public CodeField TypeCode { get; set; }

    /// <summary>A free-text note about the despatch as a whole.</summary>
    public TextField Note { get; set; }

    /// <summary>The order this despatch fulfils.</summary>
    public IdentifierField OrderReference { get; set; }

    /// <summary>What the document claims to conform to.</summary>
    public ProfileIdentifier SpecificationIdentifier { get; set; }

    /// <summary>The business process this document takes part in.</summary>
    public IdentifierField BusinessProcessType { get; set; }

    /// <summary>Who sends the goods.</summary>
    public Party? DespatchParty { get; set; }

    /// <summary>Who receives them, which is not always who bought them.</summary>
    public Party? DeliveryParty { get; set; }

    /// <summary>Who bought them.</summary>
    public Party? BuyerParty { get; set; }

    /// <summary>Who sold them, when that differs from who despatches them.</summary>
    public Party? SellerParty { get; set; }

    /// <summary>Who originated the order, when a third party did.</summary>
    public Party? OriginatorParty { get; set; }

    /// <summary>How the goods travel: weight, volume, carrier, and when they are expected.</summary>
    public Shipment? Shipment { get; set; }

    /// <summary>Documents sent with the despatch — a weight statement, a timesheet, a certificate.</summary>
    public List<AdditionalDocument> AdditionalDocuments { get; } = [];

    /// <summary>One per kind of goods despatched.</summary>
    public List<DespatchLine> Lines { get; } = [];

    /// <summary>What was reported while this was read. Empty for a document built in code.</summary>
    /// <remarks>Set by whichever reader produced it, including a reader you wrote yourself.</remarks>
    public IReadOnlyList<Diagnostic> Diagnostics { get; set; } = [];

    /// <summary>
    /// How the declared specification identifier was resolved, and what was given up along the way.
    /// <c>null</c> for a document built in code.
    /// </summary>
    public ProfileResolution? Profile { get; set; }
}
