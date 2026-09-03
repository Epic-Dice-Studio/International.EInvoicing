using System.Collections;
using System.Reflection;
using International.EInvoicing.Model;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Core.Tests;

/// <summary>
/// Whether the hand-written walk of the model reaches every node in it.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="InvoiceNodes.Descendants"/> is written out by hand so the library survives trimming and
/// ahead-of-time compilation. The cost is that adding a node to the model and forgetting to add it there
/// fails silently: <see cref="InvoiceNodes.Extensions"/> is what <c>Convert</c> reports conversion losses
/// from and what <c>inspect</c> counts, so a node the walk misses is content nobody is told about.
/// </para>
/// <para>
/// This is the check that catches it. Reflection is fine here — a test is not trimmed — and comparing a
/// reflected walk against the hand-written one is what says the two agree.
/// </para>
/// </remarks>
public class InvoiceNodeWalkTests
{
    [Fact]
    public void TheHandWrittenWalkReachesEveryNodeReflectionCanFind()
    {
        EInvoice invoice = APopulatedInvoice();

        HashSet<InvoiceNode> byHand = [.. invoice.Descendants()];
        HashSet<InvoiceNode> byReflection = [];
        Collect(invoice, byReflection);

        string[] missed =
        [
            .. byReflection.Except(byHand)
                .Select(node => node.GetType().Name)
                .Order(StringComparer.Ordinal),
        ];

        missed.ShouldBeEmpty(
            $"InvoiceNodes.Descendants does not reach: {string.Join(", ", missed)}. "
            + "Add them there, or a document's extension data on them is lost to Convert and to inspect.");
    }

    /// <summary>And the check is a check: a node deliberately left out of the walk is named.</summary>
    [Fact]
    public void AndItWouldNoticeANodeLeftOut()
    {
        EInvoice invoice = APopulatedInvoice();

        HashSet<InvoiceNode> byReflection = [];
        Collect(invoice, byReflection);

        // Stand in for a walk that forgot one: everything the real walk finds, less a node it does find.
        HashSet<InvoiceNode> incomplete = [.. invoice.Descendants().Where(node => node is not LinePrice)];

        byReflection.Except(incomplete).ShouldNotBeEmpty("the comparison must be able to fail");
    }

    /// <summary>
    /// Walks whatever the model actually holds, following every property and collection that leads to a node.
    /// </summary>
    private static void Collect(object owner, HashSet<InvoiceNode> found)
    {
        if (owner is InvoiceNode node && !found.Add(node))
        {
            return;
        }

        foreach (PropertyInfo property in owner.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.GetIndexParameters().Length == 0))
        {
            object? value;
            try
            {
                value = property.GetValue(owner);
            }
            catch (TargetInvocationException)
            {
                continue;
            }

            switch (value)
            {
                case null:
                    break;
                case InvoiceNode child:
                    Collect(child, found);
                    break;
                case IEnumerable items and not string:
                    foreach (object? item in items)
                    {
                        if (item is InvoiceNode element)
                        {
                            Collect(element, found);
                        }
                    }

                    break;
            }
        }
    }

    /// <summary>An invoice with one of everything the model can hold.</summary>
    private static EInvoice APopulatedInvoice()
    {
        var invoice = new EInvoice
        {
            Seller = APopulatedParty(),
            Buyer = APopulatedParty(),
            Payee = APopulatedParty(),
            SellerTaxRepresentative = APopulatedParty(),
            Delivery = new DeliveryInformation { Address = new PostalAddress() },
            Period = new InvoicingPeriod(),
            Payment = new PaymentInstructions
            {
                Card = new PaymentCard(),
                DirectDebit = new DirectDebit(),
            },
        };

        invoice.Notes.Add(new InvoiceNote());
        invoice.PrecedingInvoices.Add(new DocumentReference());
        invoice.Payment.CreditTransfers.Add(new CreditTransfer());
        invoice.AllowancesAndCharges.Add(new AllowanceCharge());
        invoice.VatBreakdown.Add(new VatBreakdownEntry());
        invoice.AdditionalDocuments.Add(new AdditionalDocument());

        var line = new InvoiceLine
        {
            Period = new InvoicingPeriod(),
            Price = new LinePrice(),
            Item = new Item(),
        };

        line.AllowancesAndCharges.Add(new AllowanceCharge());
        line.Price.Adjustments.Add(new AllowanceCharge());
        line.Item.Characteristics.Add(new ItemCharacteristic());
        line.Item.Classifications.Add(new ItemClassification());
        invoice.Lines.Add(line);

        return invoice;
    }

    private static Party APopulatedParty() =>
        new() { Address = new PostalAddress(), Contact = new Contact() };
}
