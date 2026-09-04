using System.Text;
using International.EInvoicing.FacturX.Pdf;
using International.EInvoicing.FacturX.PdfSharp;
using International.EInvoicing.Model;
using Shouldly;
using Xunit;

namespace International.EInvoicing.OrderX.Tests;

/// <summary>
/// Order-X inside a PDF, which is how it is actually sent.
/// </summary>
/// <remarks>
/// Order-X is hybrid for the same reason Factur-X is: the buyer's system reads the XML and the buyer reads
/// the PDF. The machinery for opening one is the same — the attachment reader takes the names to look for —
/// but the name is not: an order is filed as <c>order-x.xml</c>, and a reader handed only the invoice names
/// finds nothing in a perfectly good order.
/// </remarks>
public class HybridOrderTests
{
    [Fact]
    public void ThePublishedHybridOrderYieldsItsXml()
    {
        string? path = OrderXCorpus.Find("ORDER-X_EX01_ORDER_FULL_DATA-COMFORT.pdf");
        Assert.SkipWhen(path is null, "run build/fetch-specs.sh order-x");

        using FileStream pdf = File.OpenRead(path!);

        FacturXAttachment? attachment = new PdfSharpAttachmentReader()
            .FindAttachment(pdf, OrderXAttachment.KnownFileNames, 10_000_000);

        attachment.ShouldNotBeNull("FNFE-MPE publishes this PDF with the order embedded in it");
        attachment!.FileName.ShouldBe(OrderXAttachment.FileName);
        Encoding.UTF8.GetString(attachment.Xml).ShouldContain("SCRDMCCBDACIOMessageStructure");
    }

    /// <summary>And the order inside it reads, so the hybrid path reaches the model.</summary>
    [Fact]
    public void AndTheOrderInsideItReads()
    {
        string? path = OrderXCorpus.Find("ORDER-X_EX01_ORDER_FULL_DATA-COMFORT.pdf");
        Assert.SkipWhen(path is null, "run build/fetch-specs.sh order-x");

        using FileStream pdf = File.OpenRead(path!);
        FacturXAttachment attachment = new PdfSharpAttachmentReader()
            .FindAttachment(pdf, OrderXAttachment.KnownFileNames, 10_000_000)
            .ShouldNotBeNull();

        Order order = OrderXCorpus.Reader()
            .Read(Encoding.UTF8.GetString(attachment.Xml))
            .Value
            .ShouldNotBeNull();

        order.Number.Value.ShouldNotBeNullOrWhiteSpace();
        order.Lines.ShouldNotBeEmpty();
    }

    /// <summary>
    /// And the invoice names alone do not find it, which is the mistake this constant exists to prevent.
    /// </summary>
    [Fact]
    public void AndTheInvoiceNamesAloneDoNotFindIt()
    {
        string? path = OrderXCorpus.Find("ORDER-X_EX01_ORDER_FULL_DATA-COMFORT.pdf");
        Assert.SkipWhen(path is null, "run build/fetch-specs.sh order-x");

        using FileStream pdf = File.OpenRead(path!);

        new PdfSharpAttachmentReader()
            .FindAttachment(pdf, FacturXAttachment.KnownFileNames, 10_000_000)
            .ShouldBeNull("an order is not filed under an invoice's name");
    }
}
