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
    /// A gap this library has today: the Factur-X metadata it writes is not the document's metadata.
    /// </summary>
    /// <remarks>
    /// <para>
    /// PDFsharp generates its own XMP as it saves and puts it in the catalogue's <c>/Metadata</c> whatever
    /// was there, so the Factur-X block ends up in the file as an object nothing points at. A receiver that
    /// reads the document's metadata — which is what the specification tells it to do — sees a PDF library's
    /// producer string and no profile at all.
    /// </para>
    /// <para>
    /// The check in this class still earns its keep on <em>incoming</em> documents, which is where the
    /// disagreement is dangerous. What is missing is the other half: our own container telling the truth.
    /// This test says exactly what is wrong, and fails the day it is fixed.
    /// </para>
    /// </remarks>
    [Fact]
    public void ButTheMetadataThisWriterProducesIsNotYetTheDocumentsOwn()
    {
        byte[] pdf = Hybrid(FacturXProfiles.En16931);

        using var stream = new MemoryStream(pdf);
        string? catalogue = new PdfSharpAttachmentReader().FindMetadata(stream);

        catalogue.ShouldNotBeNull("PDFsharp writes some metadata of its own");
        catalogue!.ShouldNotContain("fx:ConformanceLevel", Case.Sensitive, "the day this contains it, invert this test");

        // The block is written — it is simply not what the catalogue points at.
        Encoding.Latin1.GetString(pdf).ShouldContain("fx:ConformanceLevel");
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
    private static byte[] Hybrid(Profile payload, Profile? stamped = null)
    {
        using var blank = new MemoryStream(SomePdfBytes());
        using var written = new MemoryStream();

        new PdfSharpAttachmentWriter().Attach(
            blank,
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
