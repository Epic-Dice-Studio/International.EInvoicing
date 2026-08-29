# International.EInvoicing.FacturX

Factur-X and ZUGFeRD are the same standard published by two organisations: a PDF with a CII invoice embedded
in it, so humans read the PDF and machines read the XML.

This package owns the five profiles and the hybrid orchestration. Attaching to and extracting from the PDF
sits behind an abstraction, so you keep your own PDF library —
`International.EInvoicing.FacturX.PdfSharp` is the implementation shipped by default.

```csharp
services.AddEInvoicing(o => o.AddFacturX());
```

Full documentation: <https://github.com/Epic-Dice-Studio/International.EInvoicing>
