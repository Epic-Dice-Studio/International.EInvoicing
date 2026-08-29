namespace International.EInvoicing.Xml;

/// <summary>
/// Resource limits applied when reading a document received from an untrusted third party.
/// Exceeding a limit produces a fatal diagnostic, never an <see cref="OutOfMemoryException"/>.
/// A value of <c>0</c> means "no limit".
/// </summary>
public sealed record DocumentLimits
{
    /// <summary>Limits applied when the caller does not specify any.</summary>
    public static DocumentLimits Default { get; } = new();

    /// <summary>Every limit disabled. Only appropriate for documents from a trusted source.</summary>
    public static DocumentLimits Unlimited { get; } = new()
    {
        MaxDocumentCharacters = 0,
        MaxElementDepth = 0,
        MaxDocumentLines = 0,
        MaxAttachmentCount = 0,
        MaxAttachmentBytes = 0,
    };

    /// <summary>Maximum number of characters in the XML document.</summary>
    public long MaxDocumentCharacters { get; init; } = 16_000_000;

    /// <summary>Maximum element nesting depth. UBL 2.1 and CII D22B stay under 20 levels.</summary>
    public int MaxElementDepth { get; init; } = 100;

    /// <summary>Maximum number of invoice lines (BG-25).</summary>
    public int MaxDocumentLines { get; init; } = 100_000;

    /// <summary>Maximum number of embedded attachments (BT-125).</summary>
    public int MaxAttachmentCount { get; init; } = 100;

    /// <summary>Maximum decoded size of a single embedded attachment, in bytes.</summary>
    public long MaxAttachmentBytes { get; init; } = 64L * 1024 * 1024;
}
