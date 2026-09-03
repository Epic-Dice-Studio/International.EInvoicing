# International.EInvoicing.FacturX.PdfSharp

The PDF half of Factur-X, built on [PDFsharp](https://www.pdfsharp.net/) (MIT).

```csharp
services.AddEInvoicing(o => o.AddFacturX());
services.AddFacturXPdfSharp();
```

It embeds the CII payload into a PDF and extracts it back out, with the Factur-X XMP metadata — written into
the document's own metadata, where the specification says a receiver looks for it, and carrying the PDF/A
extension schema that describes the four `fx` properties.

It does not turn a plain PDF into a PDF/A-3 one: start from a PDF/A-conforming document if the receiver
requires it. A conformance level the source document declares is carried through; one it does not declare is
never invented.

Full documentation: <https://github.com/Epic-Dice-Studio/International.EInvoicing>
