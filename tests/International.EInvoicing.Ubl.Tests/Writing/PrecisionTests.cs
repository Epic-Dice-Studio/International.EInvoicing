using System.Xml.Linq;
using International.EInvoicing.Building;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;
using International.EInvoicing.Ubl.Writing;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Ubl.Tests.Writing;

/// <summary>
/// Amounts are written with the precision they were given.
/// </summary>
/// <remarks>
/// This is pinned rather than assumed because it is where the neighbouring libraries have been reported
/// most: a unit price rounded to two decimals turns a fuel invoice, a per-thousand rate or a currency
/// conversion into a document whose totals no longer add up. EN 16931 allows more than two decimals on a
/// unit price for exactly that reason, and rounding a caller's number is a decision that is not this
/// library's to make.
/// </remarks>
public class PrecisionTests
{
    [Theory]
    [InlineData(1.23456)]
    [InlineData(0.0001)]
    [InlineData(1234567.89)]
    public void AUnitPriceIsWrittenWithThePrecisionItWasGiven(decimal price)
    {
        EInvoice invoice = EInvoiceBuilder
            .Create(KnownProfiles.En16931Ubl)
            .WithNumber("FA-1")
            .IssuedOn(new DateOnly(2026, 8, 30))
            .InCurrency("EUR")
            .AddLine(line => line.WithIdentifier("1").WithNetPrice(price).WithItem("Carburant"))
            .Build();

        XElement written = XElement.Parse(new UblInvoiceWriter().WriteToString(invoice));

        written.Descendants(UblNames.Cbc + "PriceAmount").ShouldHaveSingleItem()
            .Value.ShouldBe(price.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    /// <summary>Trailing zeros are part of the value a caller chose, so they are kept.</summary>
    [Fact]
    public void TrailingZerosSurvive()
    {
        EInvoice invoice = EInvoiceBuilder
            .Create(KnownProfiles.En16931Ubl)
            .WithNumber("FA-1")
            .IssuedOn(new DateOnly(2026, 8, 30))
            .InCurrency("EUR")
            .WithTotals(totals => totals.DuePayableAmount = new Values.AmountField(100.00m, "EUR"))
            .Build();

        XElement written = XElement.Parse(new UblInvoiceWriter().WriteToString(invoice));

        written.Descendants(UblNames.Cbc + "PayableAmount").ShouldHaveSingleItem().Value.ShouldBe("100.00");
    }
}
