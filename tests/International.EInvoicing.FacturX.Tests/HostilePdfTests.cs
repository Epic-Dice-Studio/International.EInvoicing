using System.Text;
using International.EInvoicing.FacturX.Pdf;
using International.EInvoicing.FacturX.PdfSharp;
using International.EInvoicing.Model;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using PdfSharp.Pdf.Security;
using Shouldly;
using Xunit;

namespace International.EInvoicing.FacturX.Tests;

/// <summary>
/// The hostile corpus, for PDFs.
/// </summary>
/// <remarks>
/// <para>
/// The XML side of this library has been defended against documents somebody else wrote since the hostile
/// corpus landed. The PDF side had never been pointed at one: every test opened a PDF this library had just
/// written. The neighbours' test folders are full of the other kind — <c>PDFWithPassword.pdf</c>,
/// <c>not-embedded.pdf</c>, <c>corrupt-factur-x-waytoosmall.pdf</c>, <c>invalidXMP-ParseError.pdf</c> — which
/// is where this list comes from.
/// </para>
/// <para>
/// The promise is the same as for XML: reading never throws, and the answer to "is there an invoice in this
/// file?" is no.
/// </para>
/// </remarks>
public class HostilePdfTests
{
    private static readonly PdfSharpAttachmentReader Reader = new();

    public static TheoryData<string, byte[]> Documents => new()
    {
        { "empty", [] },
        { "not a PDF at all", Encoding.UTF8.GetBytes("<Invoice xmlns=\"urn:oasis\"><cbc:ID>1</cbc:ID></Invoice>") },
        { "a header and nothing else", Encoding.ASCII.GetBytes("%PDF-1.7\n") },
        { "truncated halfway", Truncated() },
        { "one byte flipped in the trailer", Damaged() },
        { "a PDF with no attachment", Plain() },
        { "an encrypted PDF", Encrypted() },
    };

    [Theory]
    [MemberData(nameof(Documents))]
    public void NoneOfThemThrows(string what, byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);

        FacturXAttachment? attachment = Should.NotThrow(
            () => Reader.FindAttachment(stream, FacturXAttachment.KnownFileNames, 10_000_000),
            $"reading {what} threw");

        attachment.ShouldBeNull(what);
    }

    /// <summary>Read through the facade, the same documents are a diagnostic rather than an exception.</summary>
    [Theory]
    [MemberData(nameof(Documents))]
    public void AndThroughTheFacadeTheyAreDiagnosticsRatherThanExceptions(string what, byte[] bytes)
    {
        EInvoicing library = EInvoicing.Create(builder => builder.AddDefaults(), new PdfSharpAttachmentReader());

        DocumentResult result = Should.NotThrow(() => library.Read(bytes), $"reading {what} threw");

        result.Invoice.ShouldBeNull(what);
        result.Diagnostics.ShouldNotBeEmpty(what);
    }

    /// <summary>The limit is enforced before the attachment is decoded, not after.</summary>
    [Fact]
    public void AnAttachmentOverTheLimitIsRefusedRatherThanDecoded()
    {
        using var stream = new MemoryStream(WithAttachment(new string('x', 200_000)));

        Reader.FindAttachment(stream, FacturXAttachment.KnownFileNames, maximumBytes: 1_000).ShouldBeNull();

        stream.Position = 0;
        Reader.FindAttachment(stream, FacturXAttachment.KnownFileNames, maximumBytes: 10_000_000).ShouldNotBeNull();
    }

    private static byte[] Plain()
    {
        using var document = new PdfDocument();
        document.AddPage();

        using var buffer = new MemoryStream();
        document.Save(buffer, closeStream: false);
        return buffer.ToArray();
    }

    private static byte[] Encrypted()
    {
        using var document = new PdfDocument();
        document.AddPage();
        document.SecuritySettings.UserPassword = "secret";

        using var buffer = new MemoryStream();
        document.Save(buffer, closeStream: false);
        return buffer.ToArray();
    }

    private static byte[] Truncated() => Plain()[..(Plain().Length / 2)];

    private static byte[] Damaged()
    {
        byte[] bytes = Plain();
        bytes[^20] = (byte)'X';
        return bytes;
    }

    private static byte[] WithAttachment(string xml)
    {
        using var document = new PdfDocument();
        document.AddPage();

        using var buffer = new MemoryStream();
        document.Save(buffer, closeStream: false);
        buffer.Position = 0;

        using var written = new MemoryStream();
        new PdfSharpAttachmentWriter().Attach(
            buffer,
            new FacturXAttachment(FacturXAttachment.FacturXFileName, Encoding.UTF8.GetBytes(xml), "Data"),
            FacturXProfiles.En16931,
            written);

        return written.ToArray();
    }
}
