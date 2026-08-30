using International.EInvoicing.Cii.Reading;
using International.EInvoicing.Cii.Writing;
using International.EInvoicing.Configuration;
using International.EInvoicing.Diagnostics;
using International.EInvoicing.FacturX;
using International.EInvoicing.FacturX.Pdf;
using International.EInvoicing.FacturX.PdfSharp;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;
using PdfSharp.Pdf;

namespace International.EInvoicing.Samples.Chapters;

/// <summary>
/// Factur-X: the CII payload attached to a PDF you already have.
/// </summary>
/// <remarks>
/// The scope is deliberate. This library puts the invoice inside a PDF and takes it back out; it does not
/// draw the PDF a person reads. Producing PDF/A is a job for the tool that already lays out your invoices,
/// and the trackers of every neighbouring library are full of the arguments that follow from doing both.
/// </remarks>
internal static class HybridPdf
{
    public static void Run(EInvoice invoice)
    {
        Report.Chapter("Factur-X — an invoice inside a PDF");

        EInvoice payload = AsFacturX(invoice);

        var writer = new FacturXWriter(new CiiInvoiceWriter(), new PdfSharpAttachmentWriter());
        using MemoryStream readable = APdfSomeoneElseProduced();
        using var hybrid = new MemoryStream();

        writer.Write(payload, readable, hybrid);
        hybrid.Position = 0;

        Report.Fact("still a PDF", FacturXReader.LooksLikePdf(hybrid.ToArray()));
        Report.Fact("size", $"{hybrid.Length} bytes");

        FacturXAttachment? attachment = new PdfSharpAttachmentReader()
            .FindAttachment(hybrid, FacturXAttachment.KnownFileNames, maximumBytes: 16 * 1024 * 1024);

        Report.Fact("attached as", attachment?.FileName);
        Report.Fact("relationship", attachment?.Relationship);

        hybrid.Position = 0;
        var options = new EInvoicingOptions();
        var reader = new FacturXReader(
            options,
            new CiiInvoiceReader(options, new ProfileResolver(new ProfileRegistry(KnownProfiles.All))),
            new PdfSharpAttachmentReader());

        ParseResult<EInvoice> result = reader.Read(hybrid);

        Report.Fact("read back out", result.IsUsable);
        Report.Fact("number", result.Value?.Number.Value);
        Report.Fact("profile", result.Value?.SpecificationIdentifier.Value);
        Report.Say("Hand the same reader a PDF or an XML file: it works out which it is.");
    }

    /// <summary>The same invoice, declaring a Factur-X profile so the payload says what it is.</summary>
    private static EInvoice AsFacturX(EInvoice invoice)
    {
        invoice.SpecificationIdentifier = FacturXProfiles.Basic.Id;
        return invoice;
    }

    /// <summary>
    /// Stands in for the PDF your invoicing tool already produces. Blank on purpose: drawing text would need
    /// a font resolver, and this chapter is about the container.
    /// </summary>
    private static MemoryStream APdfSomeoneElseProduced()
    {
        using var document = new PdfDocument();
        document.AddPage();

        var stream = new MemoryStream();
        document.Save(stream, closeStream: false);
        stream.Position = 0;
        return stream;
    }
}
