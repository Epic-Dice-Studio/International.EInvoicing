using System.Text;
using International.EInvoicing.FacturX.Pdf;
using International.EInvoicing.FacturX.PdfSharp;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using Shouldly;
using Xunit;

namespace International.EInvoicing.FacturX.Tests;

/// <summary>
/// The shapes a hybrid invoice actually arrives in.
/// </summary>
/// <remarks>
/// A Factur-X PDF is not required to carry only the invoice XML. The specification allows a sender to embed
/// whatever else the invoice needs — a CSV of the line items, a timesheet, a delivery note, the supplier's
/// own rendering — and real ones do. A reader that finds the first embedded file, or that assumes there is
/// exactly one, reads the timesheet and reports that the invoice is not an invoice.
/// </remarks>
public class AttachmentVariantTests
{
    private const string Invoice = """
        <rsm:CrossIndustryInvoice xmlns:rsm="urn:un:unece:uncefact:data:standard:CrossIndustryInvoice:100">
          <rsm:ExchangedDocument/>
        </rsm:CrossIndustryInvoice>
        """;

    [Fact]
    public void TheInvoiceIsFoundWithACsvAttachedBesideIt()
    {
        byte[] pdf = AHybridInvoiceWith(("data.csv", "reference;amount\nA-1;100.00"));

        FacturXAttachment? found = Read(pdf);

        found.ShouldNotBeNull("a CSV beside the invoice must not hide the invoice");
        Encoding.UTF8.GetString(found!.Xml).ShouldContain("CrossIndustryInvoice");
        found.FileName.ShouldBe(FacturXAttachment.FacturXFileName);
    }

    /// <summary>And when the other files come first, which is where an ordering assumption would show.</summary>
    [Fact]
    public void AndWhenTheOtherFilesAreListedBeforeIt()
    {
        byte[] pdf = AHybridInvoiceWith(
            first: true,
            ("timesheet.csv", "day;hours\nmonday;8"),
            ("notes.txt", "delivered on tuesday"));

        Read(pdf).ShouldNotBeNull("the invoice must be found by name, not by position");
    }

    /// <summary>And with several of them, of assorted types.</summary>
    [Fact]
    public void AndWithSeveralOfAssortedTypes()
    {
        byte[] pdf = AHybridInvoiceWith(
            ("lines.csv", "a;1"),
            ("terms.txt", "net 30"),
            ("annex.xml", "<annex>not the invoice</annex>"));

        FacturXAttachment? found = Read(pdf);

        found.ShouldNotBeNull();
        Encoding.UTF8.GetString(found!.Xml).ShouldNotContain("not the invoice", Case.Sensitive);
    }

    /// <summary>
    /// And a PDF carrying attachments but no invoice says so, rather than returning one of them.
    /// </summary>
    [Fact]
    public void AndAPdfWithAttachmentsButNoInvoiceIsNotAnInvoice()
    {
        byte[] pdf = APdfWith(null, ("data.csv", "a;1"));

        Read(pdf).ShouldBeNull("a CSV is not an invoice, however lonely it is in the file");
    }

    /// <summary>
    /// Two XML files, one of them the invoice: a supporting document (BG-24) travels as XML often enough.
    /// </summary>
    [Fact]
    public void TheInvoiceIsFoundBesideAnotherXmlThatIsNotOne()
    {
        byte[] pdf = AHybridInvoiceWith(("delivery-note.xml", "<note>delivered</note>"));

        FacturXAttachment? found = Read(pdf);

        found!.FileName.ShouldBe(FacturXAttachment.FacturXFileName);
        Encoding.UTF8.GetString(found.Xml).ShouldContain("CrossIndustryInvoice");
    }

    /// <summary>
    /// And when both known names are present, the Factur-X one wins.
    /// </summary>
    /// <remarks>
    /// Transitional producers embedded the same payload twice, once under each name, so that readers of
    /// either generation would find it. Preference order is what makes that predictable rather than lucky.
    /// </remarks>
    [Fact]
    public void AndWhenBothKnownNamesArePresentTheFacturXOneIsChosen()
    {
        byte[] pdf = AHybridInvoiceWith(
            first: true,
            ("zugferd-invoice.xml", "<rsm:CrossIndustryInvoice xmlns:rsm=\"urn:zugferd\"><old/></rsm:CrossIndustryInvoice>"));

        FacturXAttachment? found = Read(pdf);

        found!.FileName.ShouldBe(FacturXAttachment.FacturXFileName);
        Encoding.UTF8.GetString(found.Xml).ShouldNotContain("<old/>", Case.Sensitive);
    }

    /// <summary>
    /// And the name is matched however it is cased, because producers disagree about that.
    /// </summary>
    [Fact]
    public void AndTheNameIsMatchedWhateverItsCase()
    {
        byte[] pdf = APdfWith(null, [("FACTUR-X.XML", Invoice)], othersFirst: false);

        Read(pdf).ShouldNotBeNull("the file name is a name, not a password");
    }

    private static FacturXAttachment? Read(byte[] pdf)
    {
        using var stream = new MemoryStream(pdf);

        return new PdfSharpAttachmentReader()
            .FindAttachment(stream, FacturXAttachment.KnownFileNames, 10_000_000);
    }

    private static byte[] AHybridInvoiceWith(params (string Name, string Content)[] others) =>
        AHybridInvoiceWith(first: false, others);

    private static byte[] AHybridInvoiceWith(bool first, params (string Name, string Content)[] others) =>
        APdfWith(
            new FacturXAttachment(FacturXAttachment.FacturXFileName, Encoding.UTF8.GetBytes(Invoice)),
            first ? others : others,
            othersFirst: first);

    private static byte[] APdfWith(FacturXAttachment? invoice, params (string Name, string Content)[] others) =>
        APdfWith(invoice, others, othersFirst: false);

    /// <summary>
    /// A PDF with the invoice attached the way this library attaches it, and the other files added the way
    /// any other tool would: their own file specifications in the same two places.
    /// </summary>
    private static byte[] APdfWith(
        FacturXAttachment? invoice,
        (string Name, string Content)[] others,
        bool othersFirst)
    {
        using var blank = new MemoryStream();
        using (var document = new PdfDocument())
        {
            document.AddPage();
            document.Save(blank, closeStream: false);
        }

        blank.Position = 0;
        using var withInvoice = new MemoryStream();

        if (invoice is not null)
        {
            new PdfSharpAttachmentWriter().Attach(blank, invoice, FacturXProfiles.En16931, withInvoice);
        }
        else
        {
            blank.CopyTo(withInvoice);
        }

        withInvoice.Position = 0;
        using PdfDocument opened = PdfReader.Open(withInvoice, PdfDocumentOpenMode.Modify);

        foreach ((string name, string content) in others)
        {
            AddPlainAttachment(opened, name, content, othersFirst);
        }

        using var result = new MemoryStream();
        opened.Save(result, closeStream: false);
        return result.ToArray();
    }

    /// <summary>
    /// And an invoice filed only in the EmbeddedFiles name tree, with no associated-files array at all.
    /// </summary>
    /// <remarks>
    /// The associated-files array is what PDF/A-3 requires and what a Factur-X producer writes. Plenty of
    /// older tooling files attachments only in the name tree, and those PDFs carry perfectly good invoices.
    /// </remarks>
    [Fact]
    public void AndAnInvoiceFiledOnlyInTheNameTreeIsStillFound()
    {
        byte[] pdf = APdfInNameTreeOnly(FacturXAttachment.FacturXFileName, Invoice);

        FacturXAttachment? found = Read(pdf);

        found.ShouldNotBeNull("the name tree is the other place a PDF keeps its attachments");
        Encoding.UTF8.GetString(found!.Xml).ShouldContain("CrossIndustryInvoice");
    }

    /// <summary>And one whose name is given only as the Unicode name.</summary>
    [Fact]
    public void AndOneNamedOnlyByItsUnicodeName()
    {
        byte[] pdf = APdfInNameTreeOnly(FacturXAttachment.FacturXFileName, Invoice, unicodeNameOnly: true);

        Read(pdf).ShouldNotBeNull("/UF is a file name too");
    }

    private static byte[] APdfInNameTreeOnly(string name, string content, bool unicodeNameOnly = false)
    {
        using var blank = new MemoryStream();
        using (var document = new PdfDocument())
        {
            document.AddPage();
            document.Save(blank, closeStream: false);
        }

        blank.Position = 0;
        using PdfDocument opened = PdfReader.Open(blank, PdfDocumentOpenMode.Modify);

        var stream = new PdfDictionary(opened);
        stream.CreateStream(Encoding.UTF8.GetBytes(content));
        stream.Elements["/Type"] = new PdfName("/EmbeddedFile");
        opened.Internals.AddObject(stream);

        var embedded = new PdfDictionary(opened);
        embedded.Elements["/F"] = stream.Reference;

        var specification = new PdfDictionary(opened);
        specification.Elements["/Type"] = new PdfName("/Filespec");

        if (!unicodeNameOnly)
        {
            specification.Elements["/F"] = new PdfString(name);
        }

        specification.Elements["/UF"] = new PdfString(name);
        specification.Elements["/EF"] = embedded;
        opened.Internals.AddObject(specification);

        var names = new PdfDictionary(opened);
        var tree = new PdfArray(opened);
        tree.Elements.Add(new PdfString(name));
        tree.Elements.Add(specification.Reference!);

        var embeddedFiles = new PdfDictionary(opened);
        embeddedFiles.Elements["/Names"] = tree;
        names.Elements["/EmbeddedFiles"] = embeddedFiles;
        opened.Internals.Catalog.Elements["/Names"] = names;

        using var result = new MemoryStream();
        opened.Save(result, closeStream: false);
        return result.ToArray();
    }

    private static void AddPlainAttachment(PdfDocument document, string name, string content, bool first)
    {
        var stream = new PdfDictionary(document);
        stream.CreateStream(Encoding.UTF8.GetBytes(content));
        stream.Elements["/Type"] = new PdfName("/EmbeddedFile");
        document.Internals.AddObject(stream);

        var embedded = new PdfDictionary(document);
        embedded.Elements["/F"] = stream.Reference;

        var specification = new PdfDictionary(document);
        specification.Elements["/Type"] = new PdfName("/Filespec");
        specification.Elements["/F"] = new PdfString(name);
        specification.Elements["/UF"] = new PdfString(name);
        specification.Elements["/AFRelationship"] = new PdfName("/Supplement");
        specification.Elements["/EF"] = embedded;
        document.Internals.AddObject(specification);

        PdfArray associated = document.Internals.Catalog.Elements.GetArray("/AF")
            ?? Created(document, "/AF");

        if (first)
        {
            associated.Elements.Insert(0, specification.Reference!);
        }
        else
        {
            associated.Elements.Add(specification.Reference!);
        }
    }

    private static PdfArray Created(PdfDocument document, string key)
    {
        var array = new PdfArray(document);
        document.Internals.AddObject(array);
        document.Internals.Catalog.Elements[key] = array.Reference;
        return array;
    }
}
