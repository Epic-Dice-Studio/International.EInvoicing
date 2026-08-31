using International.EInvoicing.Cii.Reading;
using International.EInvoicing.Configuration;
using International.EInvoicing.Diagnostics;
using International.EInvoicing.Documents;
using International.EInvoicing.FacturX.Pdf;
using International.EInvoicing.Model;
using International.EInvoicing.Profiles;

namespace International.EInvoicing.FacturX;

/// <summary>
/// Reads a Factur-X or ZUGFeRD invoice, whether it arrives as a hybrid PDF or as bare CII XML.
/// </summary>
/// <remarks>
/// Senders are inconsistent about which of the two they hand over, so the format is detected rather than
/// declared. Reading never throws on the document: a PDF with no payload is reported, not raised.
/// </remarks>
public sealed class FacturXReader
{
    private static readonly byte[] PdfSignature = "%PDF-"u8.ToArray();

    private readonly EInvoicingOptions _options;
    private readonly IDocumentReader<EInvoice> _cii;
    private readonly IPdfAttachmentReader? _pdf;

    /// <summary>
    /// Creates a reader. Without <paramref name="pdf"/> only bare CII can be read, which is reported rather
    /// than thrown when a PDF arrives.
    /// </summary>
    /// <exception cref="ArgumentNullException">An argument other than <paramref name="pdf"/> is <c>null</c>.</exception>
    public FacturXReader(EInvoicingOptions options, IDocumentReader<EInvoice> cii, IPdfAttachmentReader? pdf = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(cii);

        _options = options;
        _cii = cii;
        _pdf = pdf;
    }

    /// <summary>Whether <paramref name="content"/> starts with the PDF signature.</summary>
    public static bool LooksLikePdf(ReadOnlySpan<byte> content) =>
        content.Length >= PdfSignature.Length && content[..PdfSignature.Length].SequenceEqual(PdfSignature);

    /// <summary>Reads an invoice from a hybrid PDF or from bare CII XML. The stream is left open.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="content"/> is <c>null</c>.</exception>
    public ParseResult<EInvoice> Read(Stream content)
    {
        ArgumentNullException.ThrowIfNull(content);

        byte[] signature = new byte[PdfSignature.Length];
        long start = content.Position;
        int read = content.Read(signature, 0, signature.Length);
        content.Position = start;

        return read == signature.Length && LooksLikePdf(signature)
            ? ReadHybrid(content)
            : Report(_cii.Read(content));
    }

    private ParseResult<EInvoice> ReadHybrid(Stream pdf)
    {
        var diagnostics = new DiagnosticCollector(_options.DiagnosticPolicy);

        if (_pdf is null)
        {
            diagnostics.Add(Diagnostic.Create(
                FacturXDiagnostics.MissingPayload,
                "a PDF reader") with
            {
                Expected = "an IPdfAttachmentReader, such as the one in International.EInvoicing.FacturX.PdfSharp",
                Found = "no PDF reader registered",
            });

            return diagnostics.ToResult<EInvoice>(null);
        }

        FacturXAttachment? attachment = _pdf.FindAttachment(
            pdf,
            FacturXAttachment.KnownFileNames,
            _options.Limits.MaxAttachmentBytes);

        if (attachment is null)
        {
            diagnostics.Add(Diagnostic.Create(
                FacturXDiagnostics.MissingPayload,
                string.Join(", ", FacturXAttachment.KnownFileNames)) with
            {
                Expected = FacturXAttachment.FacturXFileName,
                Found = "no embedded invoice",
            });

            return diagnostics.ToResult<EInvoice>(null);
        }

        using var xml = new MemoryStream(attachment.Xml);
        ParseResult<EInvoice> result = Report(_cii.Read(xml));

        CheckMetadata(pdf, attachment, result.Value, diagnostics);

        return diagnostics.Diagnostics.Count == 0
            ? result
            : new ParseResult<EInvoice>(result.Value, [.. result.Diagnostics, .. diagnostics.Diagnostics]);
    }

    /// <summary>
    /// Whether the container tells the truth about the document inside it.
    /// </summary>
    /// <remarks>
    /// A PDF whose XMP says EN 16931 over MINIMUM XML, or names a file it does not carry, is read as two
    /// different invoices by two receivers who each believe they are right. The XML is the invoice, so this
    /// warns rather than refuses — but it warns, because nothing else in the chain will: no Schematron rule
    /// looks at a PDF, and a schema does not either.
    /// </remarks>
    private void CheckMetadata(
        Stream pdf,
        FacturXAttachment attachment,
        EInvoice? invoice,
        DiagnosticCollector diagnostics)
    {
        if (!pdf.CanSeek)
        {
            return;
        }

        pdf.Position = 0;

        if (FacturXMetadata.Read(_pdf?.FindMetadata(pdf)) is not { } metadata)
        {
            return;
        }

        if (metadata.FileName is { Length: > 0 } declared
            && !string.Equals(declared, attachment.FileName, StringComparison.OrdinalIgnoreCase))
        {
            diagnostics.Add(Diagnostic.Create(
                FacturXDiagnostics.MetadataDisagrees,
                "the embedded file is called",
                declared,
                attachment.FileName) with
            {
                Expected = attachment.FileName,
                Found = declared,
            });
        }

        if (invoice is null || metadata.ConformanceLevel is not { Length: > 0 } claimed)
        {
            return;
        }

        Profile? profile = FacturXProfiles.All
            .FirstOrDefault(candidate => candidate.Id.Value == invoice.SpecificationIdentifier.Value);

        if (profile is null)
        {
            return;
        }

        string actual = FacturXProfiles.ConformanceLevelOf(profile);

        if (!string.Equals(claimed, actual, StringComparison.OrdinalIgnoreCase))
        {
            diagnostics.Add(Diagnostic.Create(
                FacturXDiagnostics.MetadataDisagrees,
                "the profile is",
                claimed,
                actual) with
            {
                Expected = actual,
                Found = claimed,
            });
        }
    }

    /// <summary>
    /// Adds what only this layer knows: whether the declared profile is a complete EN 16931 invoice. A
    /// MINIMUM document reads perfectly well and is still not an invoice under the norm.
    /// </summary>
    private static ParseResult<EInvoice> Report(ParseResult<EInvoice> result)
    {
        if (result.Value is not { } invoice)
        {
            return result;
        }

        Profile? profile = KnownProfiles.Find(invoice.SpecificationIdentifier, DocumentSyntax.Cii);
        if (profile is null || FacturXProfiles.IsEn16931Compliant(profile))
        {
            return result;
        }

        Diagnostic diagnostic = Diagnostic.Create(
            FacturXDiagnostics.ProfileIsNotAnEn16931Invoice,
            profile.Name) with
        {
            BusinessTerm = "BT-24",
            Expected = "a profile carrying invoice lines",
            Found = profile.Name,
            AppliedFallback = "read in full; the document is not an EN 16931 invoice",
        };

        var diagnostics = new List<Diagnostic>(result.Diagnostics) { diagnostic };
        invoice.Diagnostics = diagnostics;
        return result with { Diagnostics = diagnostics };
    }
}
