using System.Globalization;
using System.Text;
using International.EInvoicing.Building;
using International.EInvoicing.FacturX.Pdf;
using International.EInvoicing.FacturX.PdfSharp;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using Shouldly;
using Xunit;

namespace International.EInvoicing.FacturX.Tests;

/// <summary>
/// Whether the container tells the truth about the invoice inside it.
/// </summary>
/// <remarks>
/// A hybrid PDF says what it holds twice: once in the XML, once in the XMP wrapped around it. A receiver
/// that trusts the metadata and one that opens the payload then hold different documents, and both are
/// confident. Nothing else in the chain notices — no Schematron rule looks at a PDF, and neither does a
/// schema — which is why this check is the library's to make.
/// </remarks>
public class ContainerMetadataTests
{
    [Fact]
    public void WhatThisLibraryWritesIsReadBackWithoutComplaint()
    {
        DocumentResult read = Library().Read(Hybrid(FacturXProfiles.En16931));

        read.Invoice.ShouldNotBeNull();
        read.Diagnostics.ShouldNotContain(diagnostic => diagnostic.Code == FacturXDiagnostics.MetadataDisagrees.Code);
    }

    /// <summary>
    /// The one that matters: metadata claiming a fuller profile than the payload carries.
    /// </summary>
    /// <remarks>
    /// A MINIMUM document is not an invoice under EN 16931 — header data and totals, no lines. A receiver
    /// that read only the metadata would have accepted it as a full one. This is the shape a template with a
    /// fixed profile and a varying payload produces, and it arrives from real senders.
    /// </remarks>
    [Fact]
    public void MetadataClaimingAFullerProfileThanThePayloadIsReported()
    {
        var pdf = new StubPdf(
            new FacturXAttachment(FacturXAttachment.FacturXFileName, Payload(FacturXProfiles.Minimum)),
            Xmp("INVOICE", "EN 16931", FacturXAttachment.FacturXFileName));

        DocumentResult read = EInvoicing.Create(builder => builder.AddDefaults(), pdf).Read(SomePdfBytes());

        read.Diagnostics.ShouldContain(
            diagnostic => diagnostic.Code == FacturXDiagnostics.MetadataDisagrees.Code
                && diagnostic.Found == "EN 16931"
                && diagnostic.Expected == "MINIMUM");

        // The XML is the invoice, so it is still read: this is a warning about the sender, not a refusal.
        read.Invoice.ShouldNotBeNull();
    }

    /// <summary>And a container that tells the truth says nothing at all.</summary>
    [Fact]
    public void AContainerThatAgreesWithItsPayloadIsSilent()
    {
        var pdf = new StubPdf(
            new FacturXAttachment(FacturXAttachment.FacturXFileName, Payload(FacturXProfiles.Minimum)),
            Xmp("INVOICE", "MINIMUM", FacturXAttachment.FacturXFileName));

        EInvoicing.Create(builder => builder.AddDefaults(), pdf).Read(SomePdfBytes()).Diagnostics
            .ShouldNotContain(diagnostic => diagnostic.Code == FacturXDiagnostics.MetadataDisagrees.Code);
    }

    /// <summary>The other disagreement: the metadata names a file the PDF does not carry.</summary>
    [Fact]
    public void AndMetadataNamingAFileThePdfDoesNotCarry()
    {
        var pdf = new StubPdf(
            new FacturXAttachment(FacturXAttachment.FacturXFileName, Payload(FacturXProfiles.En16931)),
            Xmp("INVOICE", "EN 16931", "zugferd-invoice.xml"));

        DocumentResult read = EInvoicing.Create(builder => builder.AddDefaults(), pdf).Read(SomePdfBytes());

        read.Diagnostics.ShouldContain(
            diagnostic => diagnostic.Code == FacturXDiagnostics.MetadataDisagrees.Code
                && diagnostic.Found == "zugferd-invoice.xml"
                && diagnostic.Expected == FacturXAttachment.FacturXFileName);
    }

    /// <summary>
    /// Metadata that says nothing about an invoice is not a disagreement.
    /// </summary>
    /// <remarks>
    /// Every PDF library stamps its own XMP, and most of it is about fonts and producers. Reporting a
    /// disagreement there would make the check noise, and noise is how a real warning gets ignored.
    /// </remarks>
    [Fact]
    public void MetadataThatSaysNothingAboutAnInvoiceIsSilent()
    {
        var pdf = new StubPdf(
            new FacturXAttachment(FacturXAttachment.FacturXFileName, Payload(FacturXProfiles.En16931)),
            """
            <x:xmpmeta xmlns:x="adobe:ns:meta/" x:xmptk="3.1-701">
              <rdf:RDF xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#">
                <rdf:Description rdf:about="" xmlns:pdf="http://ns.adobe.com/pdf/1.3/">
                  <pdf:Producer>Some PDF library</pdf:Producer>
                </rdf:Description>
              </rdf:RDF>
            </x:xmpmeta>
            """);

        EInvoicing.Create(builder => builder.AddDefaults(), pdf).Read(SomePdfBytes()).Diagnostics
            .ShouldNotContain(diagnostic => diagnostic.Code == FacturXDiagnostics.MetadataDisagrees.Code);
    }

    /// <summary>A reader written before this check existed keeps working, and says nothing.</summary>
    [Fact]
    public void AReaderThatDoesNotOfferMetadataIsNotAskedTwice()
    {
        var pdf = new StubPdf(
            new FacturXAttachment(FacturXAttachment.FacturXFileName, Payload(FacturXProfiles.En16931)),
            metadata: null);

        DocumentResult read = EInvoicing.Create(builder => builder.AddDefaults(), pdf).Read(SomePdfBytes());

        read.Invoice.ShouldNotBeNull();
        read.Diagnostics.ShouldNotContain(diagnostic => diagnostic.Code == FacturXDiagnostics.MetadataDisagrees.Code);
    }

    /// <summary>ZUGFeRD 2.x is Factur-X under another name, and its metadata uses its own namespace.</summary>
    [Fact]
    public void TheZugferdNamespaceIsReadAsWell()
    {
        string xmp = Xmp("ZUGFeRD", "MINIMUM", "factur-x.xml")
            .Replace(FacturXMetadata.Namespaces[0], FacturXMetadata.Namespaces[1], StringComparison.Ordinal);

        FacturXMetadata? metadata = FacturXMetadata.Read(xmp);

        metadata.ShouldNotBeNull();
        metadata!.ConformanceLevel.ShouldBe("MINIMUM");
        metadata.DocumentType.ShouldBe("ZUGFeRD");
    }

    /// <summary>Metadata this library cannot parse is metadata it cannot judge, and it says nothing.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("<x:xmpmeta><not closed")]
    [InlineData("<x:xmpmeta xmlns:x=\"adobe:ns:meta/\"><rdf:RDF xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\"/></x:xmpmeta>")]
    public void MetadataThatSaysNothingAboutAnInvoiceIsNotJudged(string xmp)
    {
        FacturXMetadata.Read(xmp).ShouldBeNull();
    }

    /// <summary>
    /// The other half of the check: the container this library writes is the document's own metadata.
    /// </summary>
    /// <remarks>
    /// PDFsharp generates XMP of its own while saving and points the catalogue at it, whatever the catalogue
    /// held before, so the Factur-X block used to end up in the file as an object nothing referenced —
    /// present in the bytes and invisible to every reader that follows the specification. It is now written
    /// after the save, as a PDF incremental update, and this reads it back the way a receiver does: from the
    /// catalogue.
    /// </remarks>
    [Fact]
    public void TheMetadataThisWriterProducesIsTheDocumentsOwn()
    {
        using var stream = new MemoryStream(Hybrid(FacturXProfiles.En16931));
        string? catalogue = new PdfSharpAttachmentReader().FindMetadata(stream);

        FacturXMetadata.Read(catalogue).ShouldNotBeNull()
            .ShouldBe(new FacturXMetadata("INVOICE", FacturXAttachment.FacturXFileName, "1.0", "EN 16931"));
    }

    /// <summary>
    /// The document keeps the metadata the PDF backend wrote about itself, with the invoice block added.
    /// </summary>
    /// <remarks>
    /// The update supersedes the object the catalogue points at, so everything that object said has to
    /// survive: replacing it with a Factur-X block alone would trade one kind of silence for another.
    /// </remarks>
    [Fact]
    public void AndKeepsWhatThePdfAlreadySaidAboutItself()
    {
        using var stream = new MemoryStream(Hybrid(FacturXProfiles.Basic));
        string catalogue = new PdfSharpAttachmentReader().FindMetadata(stream).ShouldNotBeNull();

        catalogue.ShouldContain("<pdf:Producer>");
        catalogue.ShouldContain("<xmp:CreateDate>");
        catalogue.ShouldContain("<xmpMM:DocumentID>");
    }

    /// <summary>
    /// PDF/A allows no metadata property it cannot describe, and the four Factur-X ones are described here.
    /// </summary>
    [Fact]
    public void AndDescribesTheFacturXPropertiesAsPdfARequires()
    {
        using var stream = new MemoryStream(Hybrid(FacturXProfiles.En16931));
        string catalogue = new PdfSharpAttachmentReader().FindMetadata(stream).ShouldNotBeNull();

        catalogue.ShouldContain("Factur-X PDFA Extension Schema");
        foreach (string property in new[] { "DocumentFileName", "DocumentType", "Version", "ConformanceLevel" })
        {
            catalogue.ShouldContain($"<pdfaProperty:name>{property}</pdfaProperty:name>");
        }
    }

    /// <summary>
    /// A conformance level is the source document's to claim, and this library neither invents nor loses one.
    /// </summary>
    /// <remarks>
    /// Attaching XML to a PDF does not make it PDF/A (ADR 0010), so a document that claimed nothing still
    /// claims nothing. A document that claimed PDF/A-3 keeps saying so, because the backend regenerates the
    /// metadata while saving and would otherwise drop the declaration on the floor.
    /// </remarks>
    [Fact]
    public void APdfThatClaimsNoConformanceLevelIsGivenNone()
    {
        using var stream = new MemoryStream(Hybrid(FacturXProfiles.En16931));

        new PdfSharpAttachmentReader().FindMetadata(stream).ShouldNotBeNull()
            .ShouldNotContain("pdfaid:part");
    }

    [Fact]
    public void AndOneThatClaimsPdfAKeepsSayingSo()
    {
        byte[] pdf = HybridFrom(PdfDeclaring(PdfADeclaration("3", "B")), FacturXProfiles.En16931);

        using var stream = new MemoryStream(pdf);
        string catalogue = new PdfSharpAttachmentReader().FindMetadata(stream).ShouldNotBeNull();

        catalogue.ShouldContain("<pdfaid:part>3</pdfaid:part>");
        catalogue.ShouldContain("<pdfaid:conformance>B</pdfaid:conformance>");
    }

    /// <summary>The incoming metadata is somebody else's file, and it ends up inside XMP this library writes.</summary>
    [Theory]
    [InlineData("3\"><script/><x a=\"", "B")]
    [InlineData("3", "B</pdfaid:conformance><fx:ConformanceLevel>EXTENDED")]
    [InlineData("not a part", "B")]
    public void AConformanceLevelThatIsNotOneIsNotCarriedOver(string part, string conformance)
    {
        byte[] pdf = HybridFrom(PdfDeclaring(PdfADeclaration(part, conformance)), FacturXProfiles.Minimum);

        using var stream = new MemoryStream(pdf);
        string catalogue = new PdfSharpAttachmentReader().FindMetadata(stream).ShouldNotBeNull();

        catalogue.ShouldNotContain("pdfaid:part");
        FacturXMetadata.Read(catalogue).ShouldNotBeNull().ConformanceLevel.ShouldBe("MINIMUM");
    }

    private static EInvoicing Library() =>
        EInvoicing.Create(builder => builder.AddDefaults(), new PdfSharpAttachmentReader());

    /// <summary>
    /// A hybrid PDF whose payload and whose stamp come from different profiles when asked to.
    /// </summary>
    /// <remarks>
    /// The writer takes the profile it stamps as an argument and the payload as bytes, so a mismatched
    /// container is built by giving it two different answers — which is exactly how one arises in the wild.
    /// </remarks>
    private static byte[] Hybrid(Profile payload, Profile? stamped = null) =>
        HybridFrom(SomePdfBytes(), payload, stamped);

    private static byte[] HybridFrom(byte[] source, Profile payload, Profile? stamped = null)
    {
        using var pdf = new MemoryStream(source);
        using var written = new MemoryStream();

        new PdfSharpAttachmentWriter().Attach(
            pdf,
            new FacturXAttachment(FacturXAttachment.FacturXFileName, Payload(payload)),
            stamped ?? payload,
            written);

        return written.ToArray();
    }

    private static byte[] Payload(Profile profile)
    {
        EInvoice invoice = EInvoiceBuilder
            .Create(profile)
            .WithNumber("2026-0001")
            .IssuedOn(new DateOnly(2026, 9, 1))
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

    /// <summary>A one-page PDF whose own metadata says what a test needs it to say.</summary>
    /// <remarks>
    /// Assembled byte by byte because PDFsharp cannot write one: it replaces the catalogue's metadata with
    /// XMP of its own while saving, which is the defect the code under test exists to work around.
    /// </remarks>
    private static byte[] PdfDeclaring(string metadata)
    {
        string[] objects =
        [
            "<</Type/Catalog/Pages 2 0 R/Metadata 4 0 R>>",
            "<</Type/Pages/Kids[3 0 R]/Count 1>>",
            "<</Type/Page/Parent 2 0 R/MediaBox[0 0 595 842]>>",
            $"<</Type/Metadata/Subtype/XML/Length {Encoding.UTF8.GetByteCount(metadata)}>>\nstream\n{metadata}\nendstream",
        ];

        var pdf = new StringBuilder("%PDF-1.7\n");
        var offsets = new List<int>();

        for (int number = 1; number <= objects.Length; number++)
        {
            offsets.Add(Encoding.UTF8.GetByteCount(pdf.ToString()));
            pdf.Append(CultureInfo.InvariantCulture, $"{number} 0 obj\n{objects[number - 1]}\nendobj\n");
        }

        int crossReference = Encoding.UTF8.GetByteCount(pdf.ToString());
        pdf.Append(CultureInfo.InvariantCulture, $"xref\n0 {objects.Length + 1}\n0000000000 65535 f \n");
        foreach (int offset in offsets)
        {
            pdf.Append(CultureInfo.InvariantCulture, $"{offset:0000000000} 00000 n \n");
        }

        pdf.Append(CultureInfo.InvariantCulture,
            $"trailer\n<</Size {objects.Length + 1}/Root 1 0 R>>\nstartxref\n{crossReference}\n%%EOF\n");

        return Encoding.UTF8.GetBytes(pdf.ToString());
    }

    private static string PdfADeclaration(string part, string conformance) =>
        $"""
        <?xpacket begin="" id="W5M0MpCehiHzreSzNTczkc9d"?>
        <x:xmpmeta xmlns:x="adobe:ns:meta/">
          <rdf:RDF xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#">
            <rdf:Description rdf:about="" xmlns:pdfaid="http://www.aiim.org/pdfa/ns/id/">
              <pdfaid:part>{new System.Xml.Linq.XText(part)}</pdfaid:part>
              <pdfaid:conformance>{new System.Xml.Linq.XText(conformance)}</pdfaid:conformance>
            </rdf:Description>
          </rdf:RDF>
        </x:xmpmeta>
        <?xpacket end="w"?>
        """;

    /// <summary>A PDF reader that hands back exactly what a test wants it to, including nothing.</summary>
    private sealed class StubPdf(FacturXAttachment attachment, string? metadata) : IPdfAttachmentReader
    {
        public FacturXAttachment? FindAttachment(Stream pdf, IReadOnlyList<string> fileNames, long maximumBytes) =>
            attachment;

        public string? FindMetadata(Stream pdf) => metadata;
    }

    private static string Xmp(string documentType, string level, string fileName) =>
        $"""
        <?xpacket begin="" id="W5M0MpCehiHzreSzNTczkc9d"?>
        <x:xmpmeta xmlns:x="adobe:ns:meta/">
          <rdf:RDF xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#">
            <rdf:Description rdf:about="" xmlns:fx="{FacturXMetadata.Namespaces[0]}">
              <fx:DocumentType>{documentType}</fx:DocumentType>
              <fx:DocumentFileName>{fileName}</fx:DocumentFileName>
              <fx:Version>1.0</fx:Version>
              <fx:ConformanceLevel>{level}</fx:ConformanceLevel>
            </rdf:Description>
          </rdf:RDF>
        </x:xmpmeta>
        <?xpacket end="w"?>
        """;
}
