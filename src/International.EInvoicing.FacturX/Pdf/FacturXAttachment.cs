namespace International.EInvoicing.FacturX.Pdf;

/// <summary>
/// The XML payload carried inside a hybrid invoice, and the name it is filed under.
/// </summary>
/// <param name="FileName">
/// The embedded file name. Factur-X requires <c>factur-x.xml</c>; ZUGFeRD 2.x documents in the wild also use
/// <c>zugferd-invoice.xml</c>, which is why reading accepts several names and writing settles on one.
/// </param>
/// <param name="Xml">The CII invoice itself.</param>
/// <param name="Relationship">
/// The PDF <c>AFRelationship</c> the file is attached with. Factur-X requires <c>Alternative</c>: the XML is
/// another rendering of the same invoice, not a supplement to it.
/// </param>
public sealed record FacturXAttachment(string FileName, byte[] Xml, string Relationship = "Alternative")
{
    /// <summary>The file name Factur-X requires.</summary>
    public const string FacturXFileName = "factur-x.xml";

    /// <summary>The file names a hybrid invoice may carry its payload under, in the order they are looked for.</summary>
    public static IReadOnlyList<string> KnownFileNames { get; } =
    [
        FacturXFileName,
        "zugferd-invoice.xml",
        "ZUGFeRD-invoice.xml",
        "xrechnung.xml",
    ];
}
