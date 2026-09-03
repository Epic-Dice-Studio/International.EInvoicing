# The readable copy, and what came with it

## The problem this solves

An invoice arrives, and something has to be shown to a person: an approver, an auditor, the supplier's
contact when they call. For a Factur-X invoice that is the PDF the XML came inside. The library used to
throw it away — the XML came out, the container went out of scope with the stream — leaving a caller holding
an invoice they could not display.

Beside that, an invoice may carry documents that are **not** the invoice: a timesheet, a delivery note, a
contract. BG-24 holds them, either as bytes (BT-125) or as an address to fetch (BT-124).

Three different things, and taking one for another is a real mistake:

| | | |
|---|---|---|
| **`InvoiceRendition`** | The invoice, readable | `result.Rendition` |
| **`SupportingDocument`** | Something else, attached | `invoice.SupportingDocuments` |
| **`SupportingDocumentLink`** | Something else, *not* attached — only an address | `invoice.SupportingDocumentLinks` |

They are separate types on purpose. A caller who treats a delivery note as the invoice's readable copy has
mixed up two different things, and the compiler should be the one to notice.

## The readable copy

```csharp
DocumentResult result = einvoicing.ReadFile("facture.pdf");

if (result.Rendition is { } rendition)
{
    using Stream content = rendition.OpenRead();
    await content.CopyToAsync(response.Body);       // rendition.MediaType is "application/pdf"
}
```

`Rendition` is `null` for a document that arrived as bare XML — there is no readable copy to hand back, and
this library does not draw one ([ADR 0010](../adr/0010-no-pdf-rendering.md)). Nothing is invented.

`FileName` is filled in when the document was read with `ReadFile` or `ReadFileAsync`, which is the only call
that knows a name. A stream of bytes does not have one, so it stays `null` rather than being made up.

## What the invoice carries

```csharp
foreach (SupportingDocument document in result.RequireInvoice().SupportingDocuments)
{
    Console.WriteLine($"{document.FileName} · {document.MediaType} · {document.Description}");

    using Stream content = document.OpenRead();
    await content.CopyToAsync(File.Create(document.FileName ?? document.Identifier ?? "attachment"));
}
```

| | |
|---|---|
| `Content` | The decoded bytes. `OpenRead()` hands you a stream over them. |
| `MediaType` | BT-125-1, when the sender said. `null` when they did not — it is not guessed from the name. |
| `FileName` | BT-125-2, when the sender said. |
| `Identifier` | BT-122. |
| `Description` | BT-123. |

Attachments are the largest thing an invoice can carry, and are bounded before they are decoded — see
`DocumentLimits.MaxAttachmentBytes` and `MaxAttachmentCount`. A document over the limit is reported, not
returned.

## What the invoice only points at

```csharp
foreach (SupportingDocumentLink link in invoice.SupportingDocumentLinks)
{
    Console.WriteLine(link.Location);   // https://supplier.example/contracts/9.pdf
}
```

There is no `OpenRead()` here, and that is the point. **This library performs no network I/O, ever** — so a
URI on an invoice from a third party is handed to you as text, and whether to open it is your decision, made
with your own allow-list and your own timeout. Nothing here will fetch it behind your back.

## The one that is in neither list

A BG-24 entry may carry no bytes and no address — just an identifier and a description, "see our order
4711". It is a real thing to send, and it is not something you can open, so it appears in neither collection.
It is on the invoice where it always was:

```csharp
foreach (AdditionalDocument document in invoice.AdditionalDocuments)
{
    // every BG-24 entry, whatever it holds
}
```

`SupportingDocuments` and `SupportingDocumentLinks` are views over that list, not replacements for it.

## Next

- [Reading a document](reading.md)
- [Writing a document](writing.md) — including attaching a supporting document to one you build
- [Raw values](raw-values.md)

## Run it

[`samples/International.EInvoicing.Samples/Chapters/OpeningWhatArrived.cs`](https://github.com/Epic-Dice-Studio/International.EInvoicing/blob/main/samples/International.EInvoicing.Samples/Chapters/OpeningWhatArrived.cs) is this page as code.

```bash
dotnet run --project samples/International.EInvoicing.Samples
```
