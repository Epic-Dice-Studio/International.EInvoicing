using International.EInvoicing.Building;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Core.Tests.Model;

public class EInvoiceBuilderTests
{
    private static EInvoiceBuilder AnInvoice() =>
        EInvoiceBuilder.Create(KnownProfiles.FacturXBasic)
            .WithNumber("FA-2026-001")
            .IssuedOn(new DateOnly(2026, 8, 29))
            .OfType("380")
            .InCurrency("EUR");

    [Fact]
    public void TheBuilderTakesOrdinaryValuesAndProducesFields()
    {
        EInvoice invoice = AnInvoice().Build();

        invoice.Number.Value.ShouldBe("FA-2026-001");
        invoice.IssueDate.Value.ShouldBe(new DateOnly(2026, 8, 29));
        invoice.SpecificationIdentifier.ShouldBe(KnownProfiles.FacturXBasic.Id);
    }

    [Fact]
    public void AnInvoiceBuiltInCodeCarriesNoRawTextAndNoDiagnostics()
    {
        EInvoice invoice = AnInvoice().Build();

        invoice.Number.IsFromSource.ShouldBeFalse();
        invoice.Number.Raw.ShouldBeNull();
        invoice.Diagnostics.ShouldBeEmpty();
        invoice.Profile.ShouldBeNull();
    }

    [Fact]
    public void AmountsInheritTheDocumentCurrency()
    {
        EInvoice invoice = AnInvoice()
            .AddLine(line => line.WithIdentifier("1").WithNetAmount(100m))
            .AddVatBreakdown("S", 20m, 100m, 20m)
            .Build();

        invoice.Lines[0].NetAmount.CurrencyCode.ShouldBe("EUR");
        invoice.VatBreakdown[0].TaxAmount.CurrencyCode.ShouldBe("EUR");
    }

    [Fact]
    public void PartiesAreBuiltThroughTheirOwnBuilder()
    {
        EInvoice invoice = AnInvoice()
            .WithSeller(seller => seller
                .Named("Epic Dice Studio")
                .WithVatIdentifier("FR12345678901")
                .WithIdentifier("73282932000074", schemeId: "0009")
                .WithAddress(address =>
                {
                    address.City = "Angers";
                    address.PostCode = "49000";
                    address.CountryCode = "FR";
                })
                .WithContact(contact => contact.Email = "billing@example.test"))
            .Build();

        invoice.Seller!.Name.Value.ShouldBe("Epic Dice Studio");
        invoice.Seller.Identifiers.Single().SchemeId.ShouldBe("0009");
        invoice.Seller.Address!.CountryCode.Value.ShouldBe("FR");
        invoice.Seller.Contact!.Email.Value.ShouldBe("billing@example.test");
    }

    [Fact]
    public void ALineCarriesItsQuantityUnitPriceAndVat()
    {
        EInvoice invoice = AnInvoice()
            .AddLine(line => line
                .WithIdentifier("1")
                .WithItem("Consulting")
                .WithQuantity(3m, "HUR")
                .WithNetPrice(150m)
                .WithNetAmount(450m)
                .WithVat("S", 20m))
            .Build();

        InvoiceLine line = invoice.Lines.ShouldHaveSingleItem();
        line.Quantity.UnitCode.ShouldBe("HUR");
        line.Price!.NetPrice.Value.ShouldBe(150m);
        line.Item!.Name.Value.ShouldBe("Consulting");
        line.VatRate.Value.ShouldBe(20m);
    }

    [Fact]
    public void ExtendReachesAnythingTheBuilderDoesNotCover()
    {
        EInvoice invoice = AnInvoice()
            .Extend(i => i.TenderOrLotReference = "LOT-7")
            .AddLine(line => line.Extend(l => l.BuyerAccountingReference = "CC-42"))
            .Build();

        invoice.TenderOrLotReference.Value.ShouldBe("LOT-7");
        invoice.Lines[0].BuyerAccountingReference.Value.ShouldBe("CC-42");
    }

    [Fact]
    public void EveryNodeCanKeepWhatTheModelDoesNotDescribe()
    {
        EInvoice invoice = AnInvoice().Build();
        invoice.Extensions.Add(new ExtensionElement("urn:acme:1p0", "Ref", "<acme:Ref>1</acme:Ref>"));

        invoice.Extensions.ShouldHaveSingleItem().LocalName.ShouldBe("Ref");
        invoice.Totals.Extensions.IsEmpty.ShouldBeTrue();
    }

    [Fact]
    public void TheBuilderDoesNotInventTotals()
    {
        EInvoice invoice = AnInvoice()
            .AddLine(line => line.WithNetAmount(100m))
            .Build();

        invoice.Totals.LineTotalAmount.IsSet.ShouldBeFalse();
    }

    [Fact]
    public void BuildersRejectNullConfiguration()
    {
        Should.Throw<ArgumentNullException>(() => AnInvoice().WithSeller(null!));
        Should.Throw<ArgumentNullException>(() => AnInvoice().AddLine(null!));
    }
}
