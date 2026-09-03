using System.Globalization;
using System.Text;
using System.Xml.Linq;
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
    private const string PdfANamespace = "http://www.aiim.org/pdfa/ns/id/";
    private const string EndOfDescriptions = "</rdf:RDF>";

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    public void Attach(Stream source, FacturXAttachment attachment, Profile profile, Stream destination)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(attachment);
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(destination);

        using PdfDocument document = PdfReader.Open(source, PdfDocumentOpenMode.Modify);

        string? pdfaIdentification = DeclaredPdfAIdentification(document);

        PdfDictionary specification = CreateFileSpecification(document, attachment);
        DeclareAsAssociatedFile(document, specification);
        RegisterInNameTree(document, specification, attachment.FileName);

        using var saved = new MemoryStream();
        document.Save(saved, closeStream: false);

        WriteMetadata(document, saved.ToArray(), attachment, profile, pdfaIdentification, destination);
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
    /// <remarks>
    /// It goes in after the save rather than before it, as a PDF incremental update. PDFsharp generates XMP
    /// of its own while saving and points the catalogue at that, whatever the catalogue held before, so a
    /// block written beforehand survives in the file as an object nothing references — present in the bytes
    /// and invisible to every reader that follows the specification. The update supersedes the object the
    /// catalogue does point at, with what that object holds plus the Factur-X block, and leaves every byte
    /// the backend produced where it is.
    /// </remarks>
    private static void WriteMetadata(
        PdfDocument document,
        byte[] saved,
        FacturXAttachment attachment,
        Profile profile,
        string? pdfaIdentification,
        Stream destination)
    {
        PdfReference metadata = document.Internals.Catalog.Elements.GetReference("/Metadata")
            ?? throw new InvalidOperationException("The saved PDF has no metadata object to supersede.");

        string generated = Encoding.UTF8.GetString(((PdfDictionary)metadata.Value).Stream.UnfilteredValue);

        PdfIncrementalUpdate.RewriteStreamObject(
            saved,
            metadata.ObjectID,
            "/Type/Metadata/Subtype/XML",
            Encoding.UTF8.GetBytes(WithFacturXDescriptions(generated, attachment, profile, pdfaIdentification)),
            destination);
    }

    private static string WithFacturXDescriptions(
        string xmp,
        FacturXAttachment attachment,
        Profile profile,
        string? pdfaIdentification)
    {
        int end = xmp.IndexOf(EndOfDescriptions, StringComparison.Ordinal);
        if (end < 0)
        {
            throw new InvalidOperationException("The saved PDF has metadata that is not an XMP packet.");
        }

        return xmp[..end] + pdfaIdentification + ExtensionSchema + FacturXDescription(attachment, profile) + xmp[end..];
    }

    private static string FacturXDescription(FacturXAttachment attachment, Profile profile) =>
        $"""
          <rdf:Description rdf:about="" xmlns:fx="{FacturXNamespace}">
           <fx:DocumentType>INVOICE</fx:DocumentType>
           <fx:DocumentFileName>{Escaped(attachment.FileName)}</fx:DocumentFileName>
           <fx:Version>1.0</fx:Version>
           <fx:ConformanceLevel>{Escaped(FacturXProfiles.ConformanceLevelOf(profile))}</fx:ConformanceLevel>
          </rdf:Description>

        """;

    /// <summary>
    /// The PDF/A extension schema for the four Factur-X properties.
    /// </summary>
    /// <remarks>
    /// PDF/A allows no metadata property it cannot describe, so a file carrying the <c>fx</c> namespace
    /// without this block is rejected by a conformance checker even when every other rule is satisfied. It
    /// is written whether or not the document claims PDF/A: it describes the properties, and describing them
    /// is true either way.
    /// </remarks>
    private const string ExtensionSchema = """
         <rdf:Description rdf:about="" xmlns:pdfaExtension="http://www.aiim.org/pdfa/ns/extension/" xmlns:pdfaSchema="http://www.aiim.org/pdfa/ns/schema#" xmlns:pdfaProperty="http://www.aiim.org/pdfa/ns/property#">
          <pdfaExtension:schemas>
           <rdf:Bag>
            <rdf:li rdf:parseType="Resource">
             <pdfaSchema:schema>Factur-X PDFA Extension Schema</pdfaSchema:schema>
             <pdfaSchema:namespaceURI>urn:factur-x:pdfa:CrossIndustryDocument:invoice:1p0#</pdfaSchema:namespaceURI>
             <pdfaSchema:prefix>fx</pdfaSchema:prefix>
             <pdfaSchema:property>
              <rdf:Seq>
               <rdf:li rdf:parseType="Resource">
                <pdfaProperty:name>DocumentFileName</pdfaProperty:name>
                <pdfaProperty:valueType>Text</pdfaProperty:valueType>
                <pdfaProperty:category>external</pdfaProperty:category>
                <pdfaProperty:description>The name of the embedded XML document</pdfaProperty:description>
               </rdf:li>
               <rdf:li rdf:parseType="Resource">
                <pdfaProperty:name>DocumentType</pdfaProperty:name>
                <pdfaProperty:valueType>Text</pdfaProperty:valueType>
                <pdfaProperty:category>external</pdfaProperty:category>
                <pdfaProperty:description>The type of the hybrid document in capital letters, e.g. INVOICE or ORDER</pdfaProperty:description>
               </rdf:li>
               <rdf:li rdf:parseType="Resource">
                <pdfaProperty:name>Version</pdfaProperty:name>
                <pdfaProperty:valueType>Text</pdfaProperty:valueType>
                <pdfaProperty:category>external</pdfaProperty:category>
                <pdfaProperty:description>The actual version of the standard applying to the embedded XML document</pdfaProperty:description>
               </rdf:li>
               <rdf:li rdf:parseType="Resource">
                <pdfaProperty:name>ConformanceLevel</pdfaProperty:name>
                <pdfaProperty:valueType>Text</pdfaProperty:valueType>
                <pdfaProperty:category>external</pdfaProperty:category>
                <pdfaProperty:description>The conformance level of the embedded XML document</pdfaProperty:description>
               </rdf:li>
              </rdf:Seq>
             </pdfaSchema:property>
            </rdf:li>
           </rdf:Bag>
          </pdfaExtension:schemas>
         </rdf:Description>

        """;

    /// <summary>
    /// The PDF/A conformance the incoming document declares, as the XMP description that declares it, or
    /// <c>null</c> when it declares none.
    /// </summary>
    /// <remarks>
    /// This library attaches XML to a PDF and does not make one PDF/A (ADR 0010), so it must not stamp a
    /// conformance level the file has not earned. What it must not do either is lose one: the backend
    /// regenerates the metadata while saving, and a caller who started from the PDF/A-3 file Factur-X asks
    /// for would otherwise get back a document that no longer says so.
    /// </remarks>
    private static string? DeclaredPdfAIdentification(PdfDocument source)
    {
        if (source.Internals.Catalog.Elements.GetDictionary("/Metadata") is not { Stream: not null } metadata)
        {
            return null;
        }

        XDocument packet;
        try
        {
            packet = XDocument.Parse(XmpPacket(Encoding.UTF8.GetString(metadata.Stream.UnfilteredValue)));
        }
        catch (System.Xml.XmlException)
        {
            return null;
        }

        XNamespace pdfa = PdfANamespace;
        string? part = Declared(packet, pdfa + "part");
        string? conformance = Declared(packet, pdfa + "conformance");

        // Values out of somebody else’s file end up inside XMP this library writes, so only the shapes
        // PDF/A defines are carried over and anything else counts as no declaration at all.
        return part is { Length: <= 2 } && part.All(char.IsAsciiDigit)
            && conformance is { Length: 1 } && char.IsAsciiLetterUpper(conformance[0])
            ? $"""
               <rdf:Description rdf:about="" xmlns:pdfaid="{PdfANamespace}">
                <pdfaid:part>{part}</pdfaid:part>
                <pdfaid:conformance>{conformance}</pdfaid:conformance>
               </rdf:Description>

              """
            : null;
    }

    /// <remarks>XMP says the same thing two ways, as a child element or as an attribute of the description.</remarks>
    private static string? Declared(XDocument packet, XName name)
    {
        string? value = packet.Descendants(name).FirstOrDefault()?.Value
            ?? packet.Descendants().Attributes(name).FirstOrDefault()?.Value;

        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string XmpPacket(string metadata)
    {
        int start = metadata.IndexOf("<x:xmpmeta", StringComparison.Ordinal);
        int end = metadata.IndexOf("</x:xmpmeta>", StringComparison.Ordinal);

        return start >= 0 && end > start ? metadata[start..(end + "</x:xmpmeta>".Length)] : metadata;
    }

    private static string Escaped(string value) => new XText(value).ToString();

    private static string FormatDate(DateTimeOffset moment) =>
        moment.ToString("'D:'yyyyMMddHHmmss'Z'", CultureInfo.InvariantCulture);
}
