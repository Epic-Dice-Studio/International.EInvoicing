namespace International.EInvoicing.Model;

/// <summary>
/// Walks the canonical model.
/// </summary>
/// <remarks>
/// Written out by hand rather than by reflection, for the same reason the readers and writers are: this
/// library must survive trimming and ahead-of-time compilation, and a traversal is no reason to give that up.
/// Adding a node type to <see cref="EInvoice"/> means adding it here — the test that counts the nodes a fully
/// populated invoice yields is what catches the omission.
/// </remarks>
public static class InvoiceNodes
{
    /// <summary>Every node the invoice contains, at any depth, the invoice itself included.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="invoice"/> is <c>null</c>.</exception>
    public static IEnumerable<InvoiceNode> Descendants(this EInvoice invoice)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        yield return invoice;

        foreach (InvoiceNode node in Many(invoice.Notes)
            .Concat(Many(invoice.PrecedingInvoices))
            .Concat(Party(invoice.Seller))
            .Concat(Party(invoice.Buyer))
            .Concat(Party(invoice.Payee))
            .Concat(Party(invoice.SellerTaxRepresentative))
            .Concat(Delivery(invoice.Delivery))
            .Concat(One(invoice.Period))
            .Concat(Payment(invoice.Payment))
            .Concat(Many(invoice.AllowancesAndCharges))
            .Concat(Many(invoice.VatBreakdown))
            .Concat(Many(invoice.AdditionalDocuments))
            .Concat(One(invoice.Totals))
            .Concat(invoice.Lines.SelectMany(Line)))
        {
            yield return node;
        }
    }

    /// <summary>Every extension element the invoice carries, at any depth.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="invoice"/> is <c>null</c>.</exception>
    public static IEnumerable<ExtensionElement> Extensions(this EInvoice invoice) =>
        invoice.Descendants().SelectMany(node => node.Extensions);

    private static IEnumerable<InvoiceNode> Line(InvoiceLine line) =>
        One(line)
            .Concat(One(line.Period))
            .Concat(Many(line.AllowancesAndCharges))
            .Concat(Price(line.Price))
            .Concat(Item(line.Item));

    private static IEnumerable<InvoiceNode> Price(LinePrice? price) =>
        price is null ? [] : One(price).Concat(Many(price.Adjustments));

    private static IEnumerable<InvoiceNode> Item(Item? item) =>
        item is null
            ? []
            : One(item).Concat(Many(item.Characteristics)).Concat(Many(item.Classifications));

    private static IEnumerable<InvoiceNode> Party(Party? party) =>
        party is null ? [] : One(party).Concat(One(party.Address)).Concat(One(party.Contact));

    private static IEnumerable<InvoiceNode> Delivery(DeliveryInformation? delivery) =>
        delivery is null ? [] : One(delivery).Concat(One(delivery.Address));

    private static IEnumerable<InvoiceNode> Payment(PaymentInstructions? payment) =>
        payment is null
            ? []
            : One(payment)
                .Concat(Many(payment.CreditTransfers))
                .Concat(One(payment.Card))
                .Concat(One(payment.DirectDebit));

    private static IEnumerable<InvoiceNode> One(InvoiceNode? node) => node is null ? [] : [node];

    private static IEnumerable<InvoiceNode> Many<TNode>(IEnumerable<TNode> nodes)
        where TNode : InvoiceNode => nodes;
}
