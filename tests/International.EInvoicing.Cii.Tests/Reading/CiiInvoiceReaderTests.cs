using International.EInvoicing.Cii.Reading;
using International.EInvoicing.Configuration;
using International.EInvoicing.Diagnostics;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Cii.Tests.Reading;

public class CiiInvoiceReaderTests
{
    private static CiiInvoiceReader Reader() =>
        new(new EInvoicingOptions(), new ProfileResolver(new ProfileRegistry(KnownProfiles.All)));

    [Theory]
    [MemberData(nameof(GoldenCorpus.CiiInvoiceCases), MemberType = typeof(GoldenCorpus))]
    public void EveryOfficialCiiInvoiceIsRead(string fileName)
    {
        ParseResult<EInvoice> result = Reader().Read(GoldenCorpus.Read(fileName));

        result.IsUsable.ShouldBeTrue(
            $"{fileName} produced no invoice: {string.Join("; ", result.Diagnostics.Select(d => d.ToString()))}");
        result.Value!.Number.IsSet.ShouldBeTrue();
        result.Value.IssueDate.HasValue.ShouldBeTrue();
    }

    [Theory]
    [MemberData(nameof(GoldenCorpus.CiiInvoiceCases), MemberType = typeof(GoldenCorpus))]
    public void NoOfficialInvoiceProducesAnUnreadableValue(string fileName)
    {
        ParseResult<EInvoice> result = Reader().Read(GoldenCorpus.Read(fileName));

        result.Diagnostics
            .Where(d => d.Code == "EIV2001")
            .ShouldBeEmpty($"{fileName} should not contain values this library cannot read");
    }

    [Fact]
    public void TheDocumentLevelBusinessTermsAreMapped()
    {
        EInvoice invoice = Reader().Read(GoldenCorpus.Read("01.01a-INVOICE_uncefact.xml")).Value!;

        invoice.Number.Value.ShouldBe("123456XX");
        invoice.IssueDate.Value.ShouldBe(new DateOnly(2016, 4, 4));
        invoice.TypeCode.Value.ShouldBe("380");
        invoice.CurrencyCode.Value.ShouldBe("EUR");
        invoice.SpecificationIdentifier.Value.ShouldBe(
            "urn:cen.eu:en16931:2017#compliant#urn:xeinkauf.de:kosit:xrechnung_3.0");
    }

    [Fact]
    public void ACiiDateKeepsItsFormatCodeAndItsRawText()
    {
        EInvoice invoice = Reader().Read(GoldenCorpus.Read("01.01a-INVOICE_uncefact.xml")).Value!;

        invoice.IssueDate.Raw.ShouldBe("20160404");
        invoice.IssueDate.FormatCode.ShouldBe("102");
        invoice.IssueDate.Value.ShouldBe(new DateOnly(2016, 4, 4));
    }

    [Fact]
    public void PartiesAndTheirTaxRegistrationsAreMapped()
    {
        EInvoice invoice = Reader().Read(GoldenCorpus.Read("01.01a-INVOICE_uncefact.xml")).Value!;

        invoice.Seller!.Name.IsSet.ShouldBeTrue();
        invoice.Seller.VatIdentifier.IsSet.ShouldBeTrue();
        invoice.Seller.Address!.CountryCode.Value.ShouldBe("DE");
        invoice.Buyer!.Name.IsSet.ShouldBeTrue();
    }

    [Fact]
    public void LinesTotalsAndTheVatBreakdownAreMapped()
    {
        EInvoice invoice = Reader().Read(GoldenCorpus.Read("01.01a-INVOICE_uncefact.xml")).Value!;

        invoice.Lines.ShouldNotBeEmpty();
        invoice.Lines[0].NetAmount.HasValue.ShouldBeTrue();
        invoice.Lines[0].VatCategoryCode.Value.ShouldBe("S");
        invoice.Totals.DuePayableAmount.Value.ShouldBe(336.9m);
        invoice.VatBreakdown.ShouldNotBeEmpty();
        invoice.VatBreakdown[0].TaxAmount.Value.ShouldBe(22.04m);
    }

    [Fact]
    public void ThePaymentMeansAreMapped()
    {
        EInvoice invoice = Reader().Read(GoldenCorpus.Read("01.01a-INVOICE_uncefact.xml")).Value!;

        invoice.Payment!.MeansTypeCode.Value.ShouldBe("58");
        invoice.Payment.CreditTransfers.ShouldHaveSingleItem()
            .AccountIdentifier.Value.ShouldBe("DE79000000001234567890");
        invoice.PaymentTerms.Value.ShouldBe("Zahlbar sofort ohne Abzug.");
    }
}
