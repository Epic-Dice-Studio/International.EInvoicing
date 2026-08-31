using International.EInvoicing.Building;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;
using Shouldly;
using Xunit;

namespace International.EInvoicing.Tests;

/// <summary>
/// Converting between UBL and CII, and being told what it cost.
/// </summary>
/// <remarks>
/// A French recipient must accept UBL, CII and Factur-X, so converting between them is a real requirement.
/// Doing it silently is the dangerous version: what these tests defend is that the report is truthful — that
/// what survives is reported as surviving, and what cannot cross is named rather than dropped quietly.
/// </remarks>
public class ConversionTests
{
    [Fact]
    public void ConvertingAnInvoiceWithNothingSyntaxSpecificLosesNothing()
    {
        ConversionResult result = EInvoicing.CreateDefault().Convert(AnInvoice(), DocumentFormat.Cii);

        result.IsLossless.ShouldBeTrue(result.ToString());
        result.Losses.ShouldBeEmpty();
        result.Format.ShouldBe(DocumentFormat.Cii);
    }

    [Fact]
    public void TheBusinessTermsSurviveTheCrossing()
    {
        EInvoicing library = EInvoicing.CreateDefault();
        EInvoice source = AnInvoice();

        EInvoice? crossed = library.Convert(source, DocumentFormat.Cii).Invoice;

        crossed.ShouldNotBeNull();
        crossed.Number.Value.ShouldBe(source.Number.Value);
        crossed.IssueDate.Value.ShouldBe(source.IssueDate.Value);
        crossed.CurrencyCode.Value.ShouldBe(source.CurrencyCode.Value);
        crossed.Seller!.Name.Value.ShouldBe(source.Seller!.Name.Value);
        crossed.Buyer!.Name.Value.ShouldBe(source.Buyer!.Name.Value);
        crossed.Lines.Count.ShouldBe(source.Lines.Count);
        crossed.Totals.DuePayableAmount.Value.ShouldBe(source.Totals.DuePayableAmount.Value);
    }

    /// <summary>
    /// The round trip, both ways: a document that crossed and came back says the same things it started with.
    /// </summary>
    [Theory]
    [InlineData(DocumentFormat.Ubl, DocumentFormat.Cii)]
    [InlineData(DocumentFormat.Cii, DocumentFormat.Ubl)]
    public void ADocumentThatCrossesAndComesBackSaysTheSameThings(DocumentFormat from, DocumentFormat to)
    {
        EInvoicing library = EInvoicing.CreateDefault();
        string original = library.Write(AnInvoice(), from);

        ConversionResult there = library.Convert(original, to);
        ConversionResult back = library.Convert(there.Xml, from);

        back.Invoice.ShouldNotBeNull();
        back.Invoice.Number.Value.ShouldBe("FA-2026-001");
        back.Invoice.Totals.DuePayableAmount.Value.ShouldBe(540m);
        back.Invoice.Lines.Single().Item!.Name.Value.ShouldBe("Conseil");
    }

    /// <summary>
    /// The promise that matters: an element the source syntax carried and the model has no field for is
    /// <em>named</em>, not dropped in silence.
    /// </summary>
    [Fact]
    public void SyntaxSpecificContentIsNamedRatherThanDroppedQuietly()
    {
        EInvoicing library = EInvoicing.CreateDefault();
        string ubl = library.Write(AnInvoice(), DocumentFormat.Ubl)
            .Replace(
                "</cac:AccountingSupplierParty>",
                "</cac:AccountingSupplierParty><cbc:HouseNote>kept by the reader</cbc:HouseNote>",
                StringComparison.Ordinal);

        ConversionResult result = library.Convert(ubl, DocumentFormat.Cii);

        result.IsLossless.ShouldBeFalse();
        result.Losses
            .ShouldContain(loss =>
                loss.Kind == ConversionLossKind.SyntaxSpecificContent && loss.What.Contains("HouseNote"));
    }

    /// <summary>A conversion of something that will not read produces no document and says why.</summary>
    [Fact]
    public void AnUnreadableDocumentConvertsToNothingAndSaysSo()
    {
        ConversionResult result = EInvoicing.CreateDefault().Convert("<nothing/>", DocumentFormat.Cii);

        result.Invoice.ShouldBeNull();
        result.Xml.ShouldBeEmpty();
        result.Diagnostics.ShouldNotBeEmpty();
    }

    /// <summary>Every node the model can hold is reachable from the walk the loss report depends on.</summary>
    [Fact]
    public void TheWalkReachesEveryNodeAnInvoiceHolds()
    {
        EInvoice invoice = AnInvoice();
        invoice.Payment = new PaymentInstructions();
        invoice.Payment.CreditTransfers.Add(new CreditTransfer());
        invoice.Delivery = new DeliveryInformation { Address = new PostalAddress() };
        invoice.Seller!.Contact = new Contact();
        invoice.Lines[0].Item!.Characteristics.Add(new ItemCharacteristic());

        IReadOnlyList<InvoiceNode> nodes = [.. invoice.Descendants()];

        nodes.ShouldContain(invoice);
        nodes.ShouldContain(invoice.Totals);
        nodes.ShouldContain(invoice.Seller.Contact);
        nodes.ShouldContain(invoice.Delivery.Address);
        nodes.ShouldContain(invoice.Payment.CreditTransfers[0]);
        nodes.ShouldContain(invoice.Lines[0].Item!.Characteristics[0]);
        nodes.ShouldContain(invoice.VatBreakdown[0]);
        nodes.Distinct().Count().ShouldBe(nodes.Count);
    }

    private static EInvoice AnInvoice() => EInvoiceBuilder
        .Create(KnownProfiles.En16931Ubl)
        .WithNumber("FA-2026-001")
        .IssuedOn(new DateOnly(2026, 9, 1))
        .InCurrency("EUR")
        .From("Fournisseur SARL", "FR32732829320")
        .To("Client SA", "FR89552081317")
        .AddLine(line => line.WithItem("Conseil").WithNetAmount(450m).WithVat("S", 20m))
        .WithComputedVatBreakdown()
        .WithComputedTotals()
        .Build();
}
