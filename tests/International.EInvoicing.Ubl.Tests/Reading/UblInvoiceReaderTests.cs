using International.EInvoicing.Configuration;
using International.EInvoicing.Diagnostics;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;
using International.EInvoicing.Ubl.Reading;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Ubl.Tests.Reading;

public class UblInvoiceReaderTests
{
    private static UblInvoiceReader Reader(EInvoicingOptions? options = null) =>
        new(options ?? new EInvoicingOptions(), new ProfileResolver(new ProfileRegistry(KnownProfiles.All)));

    [Theory]
    [MemberData(nameof(GoldenCorpus.UblInvoiceCases), MemberType = typeof(GoldenCorpus))]
    public void EveryOfficialUblInvoiceIsRead(string fileName)
    {
        ParseResult<EInvoice> result = Reader().Read(GoldenCorpus.Read(fileName));

        result.IsUsable.ShouldBeTrue(
            $"{fileName} produced no invoice: {string.Join("; ", result.Diagnostics.Select(d => d.ToString()))}");
        result.Value!.Number.IsSet.ShouldBeTrue();
        result.Value.IssueDate.HasValue.ShouldBeTrue();
    }

    [Theory]
    [MemberData(nameof(GoldenCorpus.UblInvoiceCases), MemberType = typeof(GoldenCorpus))]
    public void NoOfficialInvoiceProducesAnUnreadableValue(string fileName)
    {
        ParseResult<EInvoice> result = Reader().Read(GoldenCorpus.Read(fileName));

        result.Diagnostics
            .Where(d => d.Category == DiagnosticCategory.InvalidValue)
            .ShouldBeEmpty($"{fileName} should not contain values this library cannot read");
    }

    [Fact]
    public void TheDocumentLevelBusinessTermsAreMapped()
    {
        EInvoice invoice = Reader().Read(GoldenCorpus.Read("01.01a-INVOICE_ubl.xml")).Value!;

        invoice.Number.Value.ShouldBe("123456XX");
        invoice.IssueDate.Value.ShouldBe(new DateOnly(2016, 4, 4));
        invoice.TypeCode.Value.ShouldBe("380");
        invoice.CurrencyCode.Value.ShouldBe("EUR");
        invoice.BuyerReference.Value.ShouldBe("04011000-12345-03");
        invoice.SpecificationIdentifier.Value.ShouldStartWith("urn:cen.eu:en16931:2017#compliant#");
    }

    [Fact]
    public void EveryFieldKeepsTheTextTheDocumentContained()
    {
        EInvoice invoice = Reader().Read(GoldenCorpus.Read("01.01a-INVOICE_ubl.xml")).Value!;

        invoice.IssueDate.Raw.ShouldBe("2016-04-04");
        invoice.IssueDate.IsFromSource.ShouldBeTrue();
        invoice.IssueDate.Location.Path.ShouldBe("/Invoice/cbc:IssueDate");
        invoice.IssueDate.Location.Line.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void PartiesTheirAddressesAndTheirIdentifiersAreMapped()
    {
        EInvoice invoice = Reader().Read(GoldenCorpus.Read("01.01a-INVOICE_ubl.xml")).Value!;

        invoice.Seller!.Name.IsSet.ShouldBeTrue();
        invoice.Seller.VatIdentifier.IsSet.ShouldBeTrue();
        invoice.Seller.Address!.CountryCode.Value.ShouldBe("DE");
        invoice.Buyer!.Address!.CountryCode.Value.ShouldBe("DE");
    }

    [Fact]
    public void LinesCarryTheirQuantityUnitAmountAndVat()
    {
        EInvoice invoice = Reader().Read(GoldenCorpus.Read("01.01a-INVOICE_ubl.xml")).Value!;

        invoice.Lines.ShouldNotBeEmpty();
        InvoiceLine line = invoice.Lines[0];
        line.Quantity.UnitCode.ShouldBe("XPP");
        line.NetAmount.CurrencyCode.ShouldBe("EUR");
        line.VatCategoryCode.Value.ShouldBe("S");
        line.Item!.Name.IsSet.ShouldBeTrue();
    }

    [Fact]
    public void TotalsAndTheVatBreakdownAreMapped()
    {
        EInvoice invoice = Reader().Read(GoldenCorpus.Read("01.01a-INVOICE_ubl.xml")).Value!;

        invoice.Totals.TaxExclusiveAmount.HasValue.ShouldBeTrue();
        invoice.Totals.DuePayableAmount.HasValue.ShouldBeTrue();
        invoice.VatBreakdown.ShouldNotBeEmpty();
        invoice.VatBreakdown[0].CategoryCode.Value.ShouldBe("S");
    }

    [Fact]
    public void TheDeclaredProfileIsResolvedAndReported()
    {
        ParseResult<EInvoice> result = Reader().Read(GoldenCorpus.Read("01.01a-INVOICE_ubl.xml"));

        result.Value!.Profile.ShouldNotBeNull();
        result.Value.Profile!.Declared.IsDeclared.ShouldBeTrue();
    }
}
