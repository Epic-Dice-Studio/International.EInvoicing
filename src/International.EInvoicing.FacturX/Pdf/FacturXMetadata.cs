using System.Xml.Linq;

namespace International.EInvoicing.FacturX.Pdf;

/// <summary>
/// What a hybrid PDF's XMP says about the invoice inside it.
/// </summary>
/// <remarks>
/// Factur-X puts four things in the PDF's metadata: that it is an invoice, the name of the embedded file,
/// the version of the specification, and the profile — the "conformance level". A reader that trusts them
/// and a reader that opens the XML must reach the same document, and when they do not, both sides think
/// they are right. This is the half of the answer that does not require a PDF library.
/// </remarks>
/// <param name="DocumentType">What the container says it holds, normally <c>INVOICE</c>.</param>
/// <param name="FileName">The name it says the embedded file has.</param>
/// <param name="Version">The version of Factur-X it claims.</param>
/// <param name="ConformanceLevel">The profile it claims — <c>MINIMUM</c> through <c>EXTENDED</c>.</param>
public sealed record FacturXMetadata(
    string? DocumentType,
    string? FileName,
    string? Version,
    string? ConformanceLevel)
{
    /// <summary>The namespaces Factur-X and ZUGFeRD have used for this block, in that order.</summary>
    /// <remarks>
    /// ZUGFeRD 2.x and Factur-X are the same specification under two names, and a document may declare
    /// either. Reading only one of them means not reading half the documents in circulation.
    /// </remarks>
    public static IReadOnlyList<string> Namespaces { get; } =
    [
        "urn:factur-x:pdfa:CrossIndustryDocument:invoice:1p0#",
        "urn:zugferd:pdfa:CrossIndustryDocument:invoice:2p0#",
        "urn:zugferd:pdfa:CrossIndustryDocument:invoice:1p0#",
    ];

    /// <summary>
    /// Reads what an XMP packet says, or <c>null</c> when it says nothing about an invoice.
    /// </summary>
    /// <remarks>
    /// XMP that will not parse is metadata this library cannot judge, which is not the same as metadata that
    /// disagrees — so it answers <c>null</c> rather than raising, and the reader reports nothing.
    /// </remarks>
    public static FacturXMetadata? Read(string? xmp)
    {
        if (string.IsNullOrWhiteSpace(xmp))
        {
            return null;
        }

        XDocument document;
        try
        {
            document = XDocument.Parse(Trimmed(xmp));
        }
        catch (System.Xml.XmlException)
        {
            return null;
        }

        foreach (string uri in Namespaces)
        {
            XNamespace fx = uri;

            if (document.Descendants().FirstOrDefault(element => element.Name.Namespace == fx) is null)
            {
                continue;
            }

            return new FacturXMetadata(
                Value(document, fx + "DocumentType"),
                Value(document, fx + "DocumentFileName"),
                Value(document, fx + "Version"),
                Value(document, fx + "ConformanceLevel"));
        }

        return null;
    }

    /// <summary>The packet is wrapped in processing instructions that are not XML the parser will take.</summary>
    private static string Trimmed(string xmp)
    {
        int start = xmp.IndexOf("<x:xmpmeta", StringComparison.Ordinal);
        int end = xmp.IndexOf("</x:xmpmeta>", StringComparison.Ordinal);

        return start >= 0 && end > start
            ? xmp[start..(end + "</x:xmpmeta>".Length)]
            : xmp;
    }

    private static string? Value(XDocument document, XName name) =>
        document.Descendants(name).FirstOrDefault()?.Value.Trim();
}
