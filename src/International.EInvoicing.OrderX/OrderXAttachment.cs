namespace International.EInvoicing.OrderX;

/// <summary>
/// The names an Order-X document is filed under inside a hybrid PDF.
/// </summary>
/// <remarks>
/// Order-X is hybrid the same way Factur-X is — a PDF a person reads with the XML embedded beside it — and
/// the publishers are the same two. What differs is the file name: an invoice is <c>factur-x.xml</c>, an
/// order is <c>order-x.xml</c>. A reader given only the invoice names opens an Order-X PDF and finds
/// nothing, which is why this is stated rather than assumed.
/// </remarks>
public static class OrderXAttachment
{
    /// <summary>The file name Order-X requires.</summary>
    public const string FileName = "order-x.xml";

    /// <summary>The names to look for, in the order they are looked for.</summary>
    public static IReadOnlyList<string> KnownFileNames { get; } = [FileName];
}
