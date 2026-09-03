using International.EInvoicing.Diagnostics;
using International.EInvoicing.Documents;
using International.EInvoicing.Model;
using Shouldly;
using Xunit;

namespace International.EInvoicing.OrderX.Tests;

/// <summary>
/// What the one published Order-X document says, read back out of the model.
/// </summary>
/// <remarks>
/// FNFE-MPE publishes a single reference order, and it is deliberately full: every term the COMFORT profile
/// allows appears in it once. That makes it a poor corpus for variety and a good one for coverage — if a
/// term is not read from this document, it is not read at all.
/// </remarks>
public class ReadingTheReferenceOrderTests
{
    [Fact]
    public void TheOrderIsAnOrderAndSaysWhichProfileItFollows()
    {
        Order order = TheReferenceOrder();

        order.Number.Value.ShouldBe("PO123456789");
        order.TypeCode.Value.ShouldBe(OrderXTypeCodes.Order);
        order.SpecificationIdentifier.Value.ShouldBe(OrderXProfiles.Comfort.Id.Value);
        order.Profile!.Profile.ShouldBe(OrderXProfiles.Comfort);
    }

    /// <summary>
    /// The issue time is a moment, not a day: Order-X writes <c>CCYYMMDDHHMM</c>, which nothing in this
    /// library read before Order-X arrived.
    /// </summary>
    [Fact]
    public void TheIssueTimeKeepsItsHourAndMinute()
    {
        Order order = TheReferenceOrder();

        order.IssuedAt.Value.ShouldNotBeNull();
        order.IssuedAt.Value!.Value.ToUniversalTime().ShouldBe(
            new DateTimeOffset(2020, 3, 31, 12, 32, 0, TimeSpan.Zero));
        order.IssuedAt.FormatCode.ShouldBe("203");
    }

    [Fact]
    public void ThePartiesAreTheThreeTheDocumentNames()
    {
        Order order = TheReferenceOrder();

        order.Seller.ShouldNotBeNull();
        order.Buyer.ShouldNotBeNull();
        order.Originator.ShouldNotBeNull("the buyer requisitioner is who asked for the order");
        order.Invoicee.ShouldNotBeNull("stated in the settlement, not the agreement");
        order.Delivery!.Recipient.ShouldNotBeNull();
        order.Delivery.Consignor.ShouldNotBeNull("ship-from, which the model had nowhere to put before");
    }

    [Fact]
    public void EveryLineCarriesItsQuantityItsPricesAndItsTax()
    {
        Order order = TheReferenceOrder();

        order.Lines.Count.ShouldBe(3, "the reference document orders three items");
        order.Lines.Select(l => l.Identifier.Value).ShouldBe(["1", "2", "3"]);

        OrderLine line = order.Lines[0];
        line.Identifier.Value.ShouldBe("1");
        line.Quantity.Value.ShouldNotBeNull();
        line.Price!.GrossPrice.Value.ShouldNotBeNull();
        line.Price.NetPrice.Value.ShouldNotBeNull();
        line.Item!.VatCategoryCode.Value.ShouldBe("S");
        line.NetAmount.Value.ShouldNotBeNull();
    }

    [Fact]
    public void TheItemCarriesWhatTheModelHadNoRoomForUntilNow()
    {
        OrderItem item = TheReferenceOrder().Lines[0].Item.ShouldNotBeNull();

        item.BatchIdentifier.IsSet.ShouldBeTrue();
        item.BrandName.IsSet.ShouldBeTrue();
        item.OriginCountryCode.IsSet.ShouldBeTrue();
        item.Packaging.ShouldNotBeNull();
        item.Packaging!.TypeCode.IsSet.ShouldBeTrue();
        item.Instances.ShouldNotBeEmpty();
        item.Characteristics.ShouldNotBeEmpty();
    }

    [Fact]
    public void TheTotalsAreTheSixTheDocumentStates()
    {
        DocumentTotals totals = TheReferenceOrder().Totals;

        totals.LineTotalAmount.Value.ShouldNotBeNull();
        totals.ChargeTotalAmount.Value.ShouldNotBeNull();
        totals.AllowanceTotalAmount.Value.ShouldNotBeNull();
        totals.TaxExclusiveAmount.Value.ShouldNotBeNull();
        totals.TaxAmount.Value.ShouldNotBeNull();
        totals.TaxInclusiveAmount.Value.ShouldNotBeNull();
    }

    private static Order TheReferenceOrder()
    {
        string? path = OrderXCorpus.Find(OrderXCorpus.ReferenceOrder);
        Assert.SkipWhen(path is null, "run build/fetch-specs.sh order-x");

        ParseResult<Order> result = OrderXCorpus.Reader().Read(File.ReadAllText(path!));
        return result.Value.ShouldNotBeNull(
            string.Join(Environment.NewLine, result.Diagnostics.Select(d => d.ToString())));
    }
}
