# International.EInvoicing.Cii

Reads and writes UN/CEFACT Cross Industry Invoice (D22B), the syntax behind Factur-X, ZUGFeRD and
XRechnung CII, mapping to and from the EN 16931 canonical model.

Every field keeps the raw text and XML attributes it was read with — including the UNTDID 2379 `format` code
that CII dates carry — elements outside EN 16931 are kept verbatim as extension data, and reading never
throws on a document you received.

```csharp
services.AddEInvoicing(o => o.AddCii());
```

Full documentation: <https://github.com/Epic-Dice-Studio/International.EInvoicing>
