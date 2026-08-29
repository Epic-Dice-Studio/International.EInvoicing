# International.EInvoicing.Ubl

Reads and writes OASIS UBL 2.1 invoices, mapping them to and from the EN 16931 canonical model.

Every field keeps the raw text and XML attributes it was read with, elements outside EN 16931 are kept
verbatim as extension data, and reading never throws on a document you received.

```csharp
services.AddEInvoicing(o => o.AddUbl());
```

Full documentation: <https://github.com/Epic-Dice-Studio/International.EInvoicing>
