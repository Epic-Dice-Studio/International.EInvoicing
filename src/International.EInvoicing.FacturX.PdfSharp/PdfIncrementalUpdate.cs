using System.Globalization;
using System.Text;
using PdfSharp.Pdf;

namespace International.EInvoicing.FacturX.PdfSharp;

/// <summary>
/// Gives one stream object of an already-written PDF new content, by appending an incremental update.
/// </summary>
/// <remarks>
/// An incremental update is how ISO 32000-1 §7.5.6 says a PDF changes after it is written: the original
/// bytes stay where they are, the new revision is appended, and a cross-reference section whose
/// <c>/Prev</c> names the old one says which objects it supersedes. Nothing already in the file moves, so
/// every byte offset the document records about itself stays true.
/// </remarks>
internal static class PdfIncrementalUpdate
{
    /// <summary>
    /// Writes <paramref name="pdf"/>, then a revision in which <paramref name="target"/> is a stream object
    /// holding <paramref name="content"/>.
    /// </summary>
    /// <remarks>
    /// The trailer of the last revision is carried over as it stands, which keeps its <c>/Size</c> correct:
    /// this supersedes an object the document already has, and never introduces a new object number.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="pdf"/> does not end in the cross-reference table and trailer every revision ends
    /// with. Its bytes come from the PDF backend moments earlier, so this means the backend changed shape.
    /// </exception>
    public static void RewriteStreamObject(
        byte[] pdf,
        PdfObjectID target,
        string dictionary,
        byte[] content,
        Stream destination)
    {
        string text = Encoding.Latin1.GetString(pdf);
        long previousCrossReference = CrossReferenceOffset(text);
        string trailer = Trailer(text);

        byte[] revision = Revision(target, dictionary, content);
        long revisionOffset = pdf.LongLength;

        destination.Write(pdf, 0, pdf.Length);
        destination.Write(revision, 0, revision.Length);

        byte[] section = Encoding.Latin1.GetBytes(CrossReferenceSection(
            target,
            revisionOffset,
            revisionOffset + revision.LongLength,
            previousCrossReference,
            trailer));
        destination.Write(section, 0, section.Length);
    }

    private static byte[] Revision(PdfObjectID target, string dictionary, byte[] content)
    {
        using var buffer = new MemoryStream();

        Append(buffer, string.Create(
            CultureInfo.InvariantCulture,
            $"{target.ObjectNumber} {target.GenerationNumber} obj\n<<{dictionary}/Length {content.Length}>>\nstream\n"));
        buffer.Write(content, 0, content.Length);
        Append(buffer, "\nendstream\nendobj\n");

        return buffer.ToArray();
    }

    /// <remarks>
    /// Every entry of a cross-reference table is exactly twenty bytes, trailing space included, and readers
    /// index into it arithmetically rather than parsing it. Object zero heads the table as the free list.
    /// </remarks>
    private static string CrossReferenceSection(
        PdfObjectID target,
        long revisionOffset,
        long crossReferenceOffset,
        long previousCrossReference,
        string trailer)
    {
        var section = new StringBuilder();

        section.Append("xref\n0 1\n0000000000 65535 f \n");
        section.Append(CultureInfo.InvariantCulture, $"{target.ObjectNumber} 1\n");
        section.Append(CultureInfo.InvariantCulture, $"{revisionOffset:0000000000} {target.GenerationNumber:00000} n \n");
        section.Append("trailer\n");
        section.Append(trailer[..^2]);
        section.Append(CultureInfo.InvariantCulture, $"/Prev {previousCrossReference}>>\n");
        section.Append(CultureInfo.InvariantCulture, $"startxref\n{crossReferenceOffset}\n%%EOF\n");

        return section.ToString();
    }

    private static long CrossReferenceOffset(string pdf)
    {
        int keyword = pdf.LastIndexOf("startxref", StringComparison.Ordinal);
        if (keyword < 0)
        {
            throw new InvalidOperationException("The saved PDF has no startxref.");
        }

        int start = keyword + "startxref".Length;
        while (start < pdf.Length && char.IsWhiteSpace(pdf[start]))
        {
            start++;
        }

        int end = start;
        while (end < pdf.Length && char.IsAsciiDigit(pdf[end]))
        {
            end++;
        }

        return end > start
            ? long.Parse(pdf[start..end], CultureInfo.InvariantCulture)
            : throw new InvalidOperationException("The saved PDF has a startxref with no offset after it.");
    }

    /// <summary>The trailer dictionary of the last revision, from its opening to its matching close.</summary>
    private static string Trailer(string pdf)
    {
        int keyword = pdf.LastIndexOf("trailer", StringComparison.Ordinal);
        int open = keyword < 0 ? -1 : pdf.IndexOf("<<", keyword, StringComparison.Ordinal);
        if (open < 0)
        {
            throw new InvalidOperationException("The saved PDF has no trailer dictionary.");
        }

        int depth = 0;
        for (int index = open; index < pdf.Length - 1; index++)
        {
            if (pdf[index] == '<' && pdf[index + 1] == '<')
            {
                depth++;
                index++;
            }
            else if (pdf[index] == '>' && pdf[index + 1] == '>')
            {
                depth--;
                index++;

                if (depth == 0)
                {
                    return pdf[open..(index + 1)];
                }
            }
        }

        throw new InvalidOperationException("The saved PDF has a trailer dictionary that never closes.");
    }

    private static void Append(Stream stream, string text)
    {
        byte[] bytes = Encoding.Latin1.GetBytes(text);
        stream.Write(bytes, 0, bytes.Length);
    }
}
