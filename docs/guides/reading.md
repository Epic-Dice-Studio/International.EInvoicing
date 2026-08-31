# Reading a document

## The short way

Hand over what arrived. You do not say what it is.

```csharp
using International.EInvoicing;

EInvoicing einvoicing = EInvoicing.CreateDefault();

DocumentResult result = einvoicing.Read(stream);

if (result.Invoice is { } invoice)
{
    Console.WriteLine($"{invoice.Number.Value} — {invoice.Totals.DuePayableAmount}");
}
else if (result.LifecycleStatus is { } status)
{
    Console.WriteLine(status.References[0].ProcessCondition.Value);
}
```

`Read` accepts a `Stream`, a `string` or a `byte[]`. A stream is left open — the caller owns it.

UBL, CII, a lifecycle message and a PDF carrying an invoice are all recognised, by root element and namespace
rather than by file name, because senders name files whatever they like.

## Invoice or credit note

Both are invoices to EN 16931; what separates them is BT-3, the type code. Amounts stay **positive** on a
credit note — it is not an invoice with negative numbers.

```csharp
if (result.IsCreditNote)
{
    // BT-3 is 381, or one of the other credit note codes.
}
```

## Nothing throws on a document you received

That is the contract. A document written by someone else's software is data, not a programming error, so
readers report instead of raising. Exceptions are reserved for your own mistakes — a null argument, a
disposed stream.

```csharp
DocumentResult result = einvoicing.Read(stream);

if (!result.IsUsable)
{
    // Nothing came out. The diagnostics say why, and where.
    foreach (Diagnostic diagnostic in result.Diagnostics)
    {
        logger.LogError("{Code} {Message} at {Location}", diagnostic.Code, diagnostic.Message, diagnostic.Location);
    }

    return;
}

if (result.HasErrors)
{
    // Something came out, but part of it could not be trusted. Look before using it.
}
```

Every diagnostic carries a stable code with [its own page](../diagnostics/README.md), a severity, where it
happened, the business term concerned, and what was done instead.

## What you get when something is not understood

| Situation | What happens | Code |
|---|---|---|
| Profile identifier not registered | Read with the nearest ancestor profile, or generically | [EIV1042](../diagnostics/EIV1042.md) |
| Profile known but not implemented | Same, reported more severely | [EIV1043](../diagnostics/EIV1043.md) |
| A value that is not what its type says | Field keeps the raw text, `Value` is `null`, `IsRawOnly` is true | [EIV2001](../diagnostics/EIV2001.md) |
| An element outside EN 16931 | Kept verbatim on the node that contained it | [EIV2020](../diagnostics/EIV2020.md) |
| Not well-formed, or beyond a limit | Nothing is produced | [EIV5001](../diagnostics/EIV5001.md) |

None of these loses data. See [raw values](raw-values.md) for what a field keeps, and
[EIV2020](../diagnostics/EIV2020.md) for reaching the elements the model does not describe.

## Reading a hybrid PDF

A Factur-X or ZUGFeRD invoice is a PDF with the CII payload inside it. Extracting it needs a PDF reader,
which is a separate package so the choice of PDF library stays yours.

```csharp
using International.EInvoicing.FacturX.PdfSharp;

EInvoicing einvoicing = EInvoicing.CreateDefault(new PdfSharpAttachmentReader());

DocumentResult result = einvoicing.Read(File.OpenRead("invoice.pdf"));
```

Without a PDF reader, a PDF is reported rather than read — the diagnostic says which of the two cases you are
in. See [EIV4001](../diagnostics/EIV4001.md).

## Deciding what is acceptable

The library never decides for you whether a document is good enough. It reports, you decide — and you can say
so once, at startup, instead of at every call site.

```csharp
EInvoicingOptions options = new()
{
    DiagnosticPolicy = DiagnosticPolicy.Create(policy => policy
        .UsePreset(DiagnosticPreset.Balanced)
        // We only process complete invoices: a MINIMUM profile is not one.
        .OnCode("EIV4010", DiagnosticAction.Fail)
        // We do not care about elements we do not map.
        .OnCategory(DiagnosticCategory.UnmappedElement, DiagnosticAction.Suppress)),
};

EInvoicing einvoicing = EInvoicing.Create(options);
```

Three presets exist. `Balanced` reports what each descriptor declares. `Lenient` drops what a caller reading
only the EN 16931 core cannot act on. `Strict` makes anything not fully interpreted fatal.

## Limits, because documents come from outside

An invoice arrives from a third party, so it is bounded before it is read.

```csharp
EInvoicingOptions options = new()
{
    Limits = new DocumentLimits
    {
        MaxDocumentCharacters = 4_000_000,
        MaxAttachmentBytes = 16L * 1024 * 1024,
        MaxDocumentLines = 5_000,
    },
};
```

Exceeding one produces a fatal diagnostic, never an unbounded allocation. XXE, external entities and entity
expansion are refused by construction and are not configurable.

## Going one layer down

When you already know what you hold, address the reader directly. Same objects, same diagnostics.

```csharp
ParseResult<EInvoice> result = einvoicing.Ubl.Read(stream);
ParseResult<EInvoice> fromCii = einvoicing.Cii.Read(stream);
ParseResult<LifecycleStatusMessage> status = einvoicing.Lifecycle.Read(stream);
```

Or construct one yourself, which is what you do when you have replaced part of the library:

```csharp
var reader = new UblInvoiceReader(options, profileResolver);
```

## Without blocking

Every reader has an asynchronous twin, and so does every writer:

```csharp
DocumentResult result = await einvoicing.ReadAsync(request.Body, cancellationToken);
await einvoicing.UblWriter.WriteAsync(invoice, response.Body, cancellationToken);
```

The awaiting is the **transfer** — receiving the bytes, sending them back. The parsing in between is work,
not waiting, so it stays synchronous: an `Async` suffix here never means "made to look like it waits". See
[ADR 0012](../adr/0012-async-at-the-boundary.md) for why, including why asynchronous *parsing* was
considered and rejected.

## Next

- [Writing a document](writing.md)
- [Lifecycle statuses](lifecycle.md)
- [Raw values](raw-values.md)
- [Validation](validation.md)

## Run it

[`samples/International.EInvoicing.Samples/Chapters/HostileDocuments.cs`](https://github.com/Epic-Dice-Studio/International.EInvoicing/blob/main/samples/International.EInvoicing.Samples/Chapters/HostileDocuments.cs) is this page as code — reading, and what happens when a document fights back.

```bash
dotnet run --project samples/International.EInvoicing.Samples
```
