using System.Text;
using International.EInvoicing.Building;
using International.EInvoicing.Documents;
using International.EInvoicing.FacturX.Pdf;
using International.EInvoicing.FacturX.PdfSharp;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;
using PdfSharp.Pdf;
using Shouldly;
using Xunit;

namespace International.EInvoicing.FacturX.Tests;

/// <summary>
/// The readable copy of a hybrid invoice, which used to be dropped once the XML was out of it.
/// </summary>
/// <remarks>
/// A caller who reads a Factur-X PDF wants two things out of it: the invoice as data, and the invoice as
/// something they can show a person. The second one is the container itself, and until now it went out of
/// scope with the stream it arrived on — leaving a caller holding an invoice they could not display.
/// </remarks>
public class RenditionTests
{
    [Fact]
    public void AHybridInvoiceHandsBackThePdfItArrivedIn()
    {
        byte[] pdf = Hybrid();

        DocumentResult read = Library().Read(pdf);

        InvoiceRendition rendition = read.Rendition.ShouldNotBeNull();
        rendition.MediaType.ShouldBe("application/pdf");
        rendition.Content.ShouldBe(pdf);

        using var stream = rendition.OpenRead();
        byte[] signature = new byte[5];
        stream.ReadExactly(signature);
        Encoding.ASCII.GetString(signature).ShouldBe("%PDF-");
    }

    /// <summary>A file has a name; a stream of bytes does not, and this does not invent one.</summary>
    [Fact]
    public void AndTheNameOfTheFileWhenItWasReadFromOne()
    {
        string path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-facture.pdf");
        File.WriteAllBytes(path, Hybrid());

        try
        {
            Library().ReadFile(path).Rendition.ShouldNotBeNull().FileName.ShouldBe(Path.GetFileName(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void AndNoNameWhenItWasNot()
    {
        Library().Read(Hybrid()).Rendition.ShouldNotBeNull().FileName.ShouldBeNull();
    }

    /// <summary>
    /// A document that arrived as bare XML has no readable copy, and is not given one.
    /// </summary>
    /// <remarks>
    /// Rendering an invoice is out of scope (ADR 0010), so the honest answer here is nothing rather than
    /// something generated. A caller who needs a PDF from CII needs a renderer, and this says so by being
    /// empty rather than by being wrong.
    /// </remarks>
    [Fact]
    public void BareXmlHasNoReadableCopyToHandBack()
    {
        Library().Read(Payload(FacturXProfiles.En16931)).Rendition.ShouldBeNull();
    }

    private static EInvoicing Library() =>
        EInvoicing.Create(builder => builder.AddDefaults(), new PdfSharpAttachmentReader());

    private static byte[] Hybrid()
    {
        using var blank = new MemoryStream(SomePdfBytes());
        using var written = new MemoryStream();

        new PdfSharpAttachmentWriter().Attach(
            blank,
            new FacturXAttachment(FacturXAttachment.FacturXFileName, Payload(FacturXProfiles.En16931)),
            FacturXProfiles.En16931,
            written);

        return written.ToArray();
    }

    private static byte[] Payload(Profile profile)
    {
        EInvoice invoice = EInvoiceBuilder
            .Create(profile)
            .WithNumber("2026-0003")
            .IssuedOn(new DateOnly(2026, 9, 3))
            .OfType(InvoiceTypeCodes.CommercialInvoice)
            .InCurrency("EUR")
            .From(seller => seller.Named("Vendeur SAS").WithVatIdentifier("FR40303265045"))
            .To(buyer => buyer.Named("Acheteur GmbH"))
            .Extend(document => document.Totals.DuePayableAmount = 1200m)
            .Build();

        return Encoding.UTF8.GetBytes(new Cii.Writing.CiiInvoiceWriter().WriteToString(invoice));
    }

    private static byte[] SomePdfBytes()
    {
        using var document = new PdfDocument();
        document.AddPage();

        using var buffer = new MemoryStream();
        document.Save(buffer, closeStream: false);
        return buffer.ToArray();
    }
}
