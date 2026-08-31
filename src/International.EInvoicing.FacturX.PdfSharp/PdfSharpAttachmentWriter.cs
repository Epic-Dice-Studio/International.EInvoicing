using System.Globalization;
using International.EInvoicing.FacturX.Pdf;
using International.EInvoicing.Profiles;
using PdfSharp.Pdf;
using PdfSharp.Pdf.Advanced;
using PdfSharp.Pdf.IO;

namespace International.EInvoicing.FacturX.PdfSharp;

/// <summary>
/// Embeds the invoice payload into a PDF, using PDFsharp.
/// </summary>
/// <remarks>
/// This writes what makes a PDF a Factur-X document: the embedded file, its declaration as an associated file
/// with the <c>Alternative</c> relationship, and the XMP metadata naming the profile. It does <em>not</em>
/// convert a plain PDF into a PDF/A-3 one — colour spaces, fonts and output intents are properties of the
/// document you start from. Start from a PDF/A-conforming file when the receiver requires conformance.
/// </remarks>
public sealed class PdfSharpAttachmentWriter : IPdfAttachmentWriter
{
    private const string FacturXNamespace = "urn:factur-x:pdfa:CrossIndustryDocument:invoice:1p0#";

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    public void Attach(Stream source, FacturXAttachment attachment, Profile profile, Stream destination)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(attachment);
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(destination);

        using PdfDocument document = PdfReader.Open(source, PdfDocumentOpenMode.Modify);

        PdfDictionary specification = CreateFileSpecification(document, attachment);
        DeclareAsAssociatedFile(document, specification);
        RegisterInNameTree(document, specification, attachment.FileName);
        WriteMetadata(document, attachment, profile);

        document.Save(destination, closeStream: false);
    }

    private static PdfDictionary CreateFileSpecification(PdfDocument document, FacturXAttachment attachment)
    {
        var embedded = new PdfDictionary(document);
        embedded.CreateStream(attachment.Xml);
        embedded.Elements["/Type"] = new PdfName("/EmbeddedFile");
        embedded.Elements["/Subtype"] = new PdfName("/text#2Fxml");

        var parameters = new PdfDictionary(document);
        parameters.Elements["/Size"] = new PdfInteger(attachment.Xml.Length);
        parameters.Elements["/ModDate"] = new PdfString(FormatDate(DateTimeOffset.UtcNow));
        embedded.Elements["/Params"] = parameters;
        document.Internals.AddObject(embedded);

        var files = new PdfDictionary(document);
        files.Elements["/F"] = embedded.Reference;
        files.Elements["/UF"] = embedded.Reference;

        var specification = new PdfDictionary(document);
        specification.Elements["/Type"] = new PdfName("/Filespec");
        specification.Elements["/F"] = new PdfString(attachment.FileName);
        specification.Elements["/UF"] = new PdfString(attachment.FileName);
        specification.Elements["/AFRelationship"] = new PdfName("/" + attachment.Relationship);
        specification.Elements["/Desc"] = new PdfString("Invoice data in CII format");
        specification.Elements["/EF"] = files;
        document.Internals.AddObject(specification);

        return specification;
    }

    /// <summary>
    /// Declares the payload as an associated file of the document. This is what tells a reader the XML is
    /// part of the invoice rather than an ordinary attachment somebody added.
    /// </summary>
    private static void DeclareAsAssociatedFile(PdfDocument document, PdfDictionary specification)
    {
        PdfDictionary catalog = document.Internals.Catalog;
        PdfArray associated = catalog.Elements.GetArray("/AF") ?? new PdfArray(document);
        associated.Elements.Add(specification.Reference!);
        catalog.Elements["/AF"] = associated;
    }

    private static void RegisterInNameTree(PdfDocument document, PdfDictionary specification, string fileName)
    {
        PdfDictionary catalog = document.Internals.Catalog;
        PdfDictionary names = catalog.Elements.GetDictionary("/Names") ?? Add(document, catalog, "/Names");
        PdfDictionary embeddedFiles = names.Elements.GetDictionary("/EmbeddedFiles")
            ?? Add(document, names, "/EmbeddedFiles");

        PdfArray pairs = embeddedFiles.Elements.GetArray("/Names") ?? new PdfArray(document);
        pairs.Elements.Add(new PdfString(fileName));
        pairs.Elements.Add(specification.Reference!);
        embeddedFiles.Elements["/Names"] = pairs;
    }

    private static PdfDictionary Add(PdfDocument document, PdfDictionary parent, string key)
    {
        var dictionary = new PdfDictionary(document);
        parent.Elements[key] = dictionary;
        return dictionary;
    }

    /// <summary>
    /// Writes the XMP metadata a Factur-X reader looks for: the file name, the profile, and the document
    /// type. Without it the payload is just an attachment, and conforming readers will not find it.
    /// </summary>
    private static void WriteMetadata(PdfDocument document, FacturXAttachment attachment, Profile profile)
    {
        string xmp = $"""
            <?xpacket begin="﻿" id="W5M0MpCehiHzreSzNTczkc9d"?>
            <x:xmpmeta xmlns:x="adobe:ns:meta/">
              <rdf:RDF xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#">
                <rdf:Description rdf:about="" xmlns:pdfaid="http://www.aiim.org/pdfa/ns/id/">
                  <pdfaid:part>3</pdfaid:part>
                  <pdfaid:conformance>B</pdfaid:conformance>
                </rdf:Description>
                <rdf:Description rdf:about="" xmlns:fx="{FacturXNamespace}">
                  <fx:DocumentType>INVOICE</fx:DocumentType>
                  <fx:DocumentFileName>{attachment.FileName}</fx:DocumentFileName>
                  <fx:Version>1.0</fx:Version>
                  <fx:ConformanceLevel>{FacturXProfiles.ConformanceLevelOf(profile)}</fx:ConformanceLevel>
                </rdf:Description>
              </rdf:RDF>
            </x:xmpmeta>
            <?xpacket end="w"?>
            """;

        // A PdfMetadata rather than a plain dictionary, and this is not decoration: PDFsharp writes its own
        // XMP as it saves, and replaces anything in /Metadata that is not one of these. The Factur-X block
        // then survives in the file as an object nobody points at, which is the same as not writing it —
        // a receiver reading the document's metadata sees the PDF library's, with no profile in it.
        // A PdfMetadata rather than a plain dictionary, which is as close as this backend gets: PDFsharp
        // writes its own XMP while saving and puts it in /Metadata whatever was there, so the Factur-X block
        // below ends up in the file as an object nothing points at. A receiver reading the document's
        // metadata sees PDFsharp's, with no profile in it. The block is still written, and the day the
        // backend stops overwriting it the document is conformant with no further change here — but it is a
        // known gap today, pinned by a test and recorded in the roadmap.
        var metadata = new PdfMetadata(document);
        metadata.Stream.Value = System.Text.Encoding.UTF8.GetBytes(xmp);
        metadata.Elements["/Type"] = new PdfName("/Metadata");
        metadata.Elements["/Subtype"] = new PdfName("/XML");

        document.Internals.Catalog.Elements["/Metadata"] = metadata.ReferenceNotNull;
    }

    private static string FormatDate(DateTimeOffset moment) =>
        moment.ToString("'D:'yyyyMMddHHmmss'Z'", CultureInfo.InvariantCulture);
}
