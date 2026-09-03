using System.Text;
using International.EInvoicing.Cii.Writing;
using International.EInvoicing.Documents;
using International.EInvoicing.FacturX;
using International.EInvoicing.FacturX.Pdf;
using International.EInvoicing.FacturX.PdfSharp;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;
using International.EInvoicing.Values;
using PdfSharp.Pdf;

namespace International.EInvoicing.Samples.Chapters;

/// <summary>
/// Getting the things a person opens out of an invoice, with the three of them named apart.
/// </summary>
/// <remarks>
/// The naming is the point of this chapter. A <em>rendition</em> is the invoice in a form somebody can look
/// at; a <em>supporting document</em> is something else, attached; a BG-24 entry that gives only a URI is
/// neither, because fetching it is network I/O this library does not do. A caller who treats a delivery note
/// as the invoice's readable copy has mixed up two different things, and the type names stop them.
/// </remarks>
internal static class OpeningWhatArrived
{
    public static void Run(EInvoicing einvoicing, EInvoice invoice)
    {
        Report.Chapter("Opening what arrived — the readable copy, and what came with it");

        DocumentResult read = einvoicing.Read(AHybridInvoice(invoice));

        Report.Say("The invoice as a person reads it — for a hybrid invoice, the PDF it arrived in:");
        InvoiceRendition? rendition = read.Rendition;
        Report.Fact("rendition", rendition is null ? null : $"{rendition.Content.Length} bytes");
        Report.Fact("media type", rendition?.MediaType);
        Report.Fact("file name", rendition?.FileName);
        Report.Note("null for a document that arrived as bare XML: there is no readable copy to hand back.");

        Console.WriteLine();
        Report.Say("What the invoice carries beside itself (BG-24, attached as BT-125):");
        foreach (SupportingDocument document in read.RequireInvoice().SupportingDocuments)
        {
            Report.Fact(document.FileName ?? document.Identifier ?? "(unnamed)",
                $"{document.MediaType} · {document.Content.Length} bytes · {document.Description}");

            using var content = new StreamReader(document.OpenRead());
            Report.Note($"first line: {content.ReadLine()}");
        }

        Console.WriteLine();
        Report.Say("What it points at rather than carries (BT-124) — an address, not a document:");
        foreach (SupportingDocumentLink link in read.RequireInvoice().SupportingDocumentLinks)
        {
            Report.Fact(link.Identifier ?? "(unidentified)", link.Location);
        }

        Report.Note("Fetching one is yours to do: this library performs no network I/O, ever.");
    }

    private static byte[] AHybridInvoice(EInvoice invoice)
    {
        invoice.SpecificationIdentifier = FacturXProfiles.Basic.Id;

        invoice.AdditionalDocuments.Add(new AdditionalDocument
        {
            Identifier = "TIME-42",
            Description = "Timesheet for the period",
            Attachment = new BinaryField(
                Encoding.UTF8.GetBytes("day;hours;rate\n2026-09-01;8;120"),
                "text/csv",
                "timesheet.csv"),
        });

        invoice.AdditionalDocuments.Add(new AdditionalDocument
        {
            Identifier = "CONTRACT-9",
            Description = "Framework contract",
            ExternalLocation = "https://example.invalid/contracts/9.pdf",
        });

        var writer = new FacturXWriter(new CiiInvoiceWriter(), new PdfSharpAttachmentWriter());

        using var readable = APdfSomeoneElseProduced();
        using var hybrid = new MemoryStream();
        writer.Write(invoice, readable, hybrid);

        return hybrid.ToArray();
    }

    /// <summary>Stands in for the PDF your invoicing tool already produces. Blank on purpose.</summary>
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
