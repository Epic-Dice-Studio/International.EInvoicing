# Running your own logic during generation

## The problem this solves

Every company has something it does to every outgoing invoice. A numbering scheme accounting insists on. A
rounding rule the ERP applies and the norm does not describe. A signature. An audit line. An element a single
large customer demands and nobody else has ever asked for.

There are two usual answers, and both are bad. Fork the library, and you own the merge forever. Do it at each
call site, and it works until the day somebody adds a call site.

The third answer is a **write pipeline step**: your code, running inside generation, for every document the
library writes.

```csharp
EInvoicing library = EInvoicing.Create(einvoicing => einvoicing
    .AddDefaults()
    .AddWriteStep((context, next) =>
    {
        context.Invoice.BuyerReference = HouseReferences.For(context.Invoice);
        next(context);
    }));

library.Write(invoice, DocumentFormat.Ubl);   // the reference is there
```

## The shape

It is ASP.NET Core's middleware, for the same reason: it is the shape that lets you work before, after, or
instead of what comes next.

```csharp
internal sealed class SignEverythingWeSend : IWritePipelineStep
{
    public void Write(WriteContext context, Action<WriteContext> next)
    {
        // Before: the invoice, still a model.
        context.Invoice.Notes.Add(new InvoiceNote { Text = "Signed copy" });

        next(context);          // the rest of the pipeline, then the writer

        // After: the document, as text.
        context.Xml = Signatures.Sign(context.Xml);
    }
}

EInvoicing library = EInvoicing.Create(e => e.AddDefaults().AddWriteStep(new SignEverythingWeSend()));
```

The context holds what there is to work with:

| | |
|---|---|
| `Invoice` | The invoice being written. Change it, or replace it, before calling `next`. |
| `Syntax` | Which syntax this write is in, when a step should only act on one. |
| `Xml` | The document. Empty until `next` has run. |
| `Items` | Anything one step wants to hand to a later one. |

Steps run in the order they were added: the first added is the outermost, so it acts first on the way in and
last on the way out.

A step that does not call `next` stops the write, and whatever it left in `context.Xml` is what the caller
gets. That is deliberate — it is how a step refuses to emit a document it is not happy with — but it means an
early return needs to be a decision, not an oversight.

## There is no way past it

The steps are wrapped **around the writer**, not called by the facade. So all of these run them:

```csharp
library.Write(invoice, DocumentFormat.Ubl);
library.UblWriter.WriteToString(invoice);
provider.GetRequiredService<EInvoicing>().Write(invoice);
```

A guarantee with a bypass is not a guarantee. If "every invoice we send is signed" is the rule, a colleague
reaching for `UblWriter` directly must not be able to break it by accident.

The consequence worth knowing: `library.UblWriter` is a `WritePipeline` once you have added a step, not a
`UblInvoiceWriter`. Its `Inner` property is the writer underneath, for the rare case you genuinely want to
write without the steps.

## In a container

Same call, and the step can take dependencies:

```csharp
services.AddEInvoicing(einvoicing => einvoicing
    .AddDefaults()
    .AddWriteStep(new SignEverythingWeSend()));
```

`AddWriteStep` registers the step as a singleton, and the facade picks up every `IWritePipelineStep` the
container knows about — so a step registered by any other means joins the pipeline too.

## What it is not for

**Validation.** A step could inspect the XML and throw, but rules belong in a rule set, where they are named,
reportable and suppressible one by one. See [validating a document](validation.md).

**Reading.** There is no read pipeline yet. What arrives is described by diagnostics rather than transformed;
if you need to normalise something on the way in, do it on the model after
[reading](reading.md).
