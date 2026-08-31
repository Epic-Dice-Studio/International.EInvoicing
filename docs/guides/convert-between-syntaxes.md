# Converting between UBL and CII

## The problem this solves

From September 2026 a French company must be able to *receive* an electronic invoice, and it does not get to
choose the syntax. Its customers send UBL, CII, or a Factur-X PDF carrying CII. Its own accounting system
reads one of them.

So conversion is not a nicety, it is the integration. And the dangerous version is the silent one: a converter
that hands back a well-formed document and says nothing about the three elements it dropped on the way.

Here the report is the feature.

```csharp
EInvoicing library = EInvoicing.CreateDefault();

ConversionResult result = library.Convert(receivedXml, DocumentFormat.Cii);

if (!result.IsLossless)
{
    foreach (ConversionLoss loss in result.Losses)
    {
        Console.WriteLine(loss);   // SyntaxSpecificContent at /Invoice[1]/HouseNote[1]: {…}HouseNote
    }
}

File.WriteAllText("converted.xml", result.Xml);
```

`Convert` takes either an `EInvoice` you built or the XML of a document you received. Given XML, it reads the
document first — and what reading reported is part of the report, because a conversion built on a document
that would not read cleanly is not a clean conversion.

## What the result holds

| | |
|---|---|
| `Xml` | The converted document. Empty when the source would not read at all. |
| `Format` | The syntax it is written in. |
| `Invoice` | The invoice **as the converted document reads back** — not the one that went in. |
| `Losses` | What the conversion could not carry. |
| `Diagnostics` | Everything reading reported, on the source and on the result. |
| `IsLossless` | Whether `Losses` is empty. |

## How the losses are found

They are **found, not predicted**. There is no hand-maintained table of "things UBL has that CII does not",
because such a table is wrong the day a mapping changes and nobody notices.

Instead:

1. The invoice is written in the target syntax.
2. That document is read back, and anything it reports at `Warning` or above becomes a
   `ReportedOnReread` loss.
3. Every extension element the source carried becomes a `SyntaxSpecificContent` loss.

The second one deserves the explanation. Extension data is what the reader kept verbatim because the model
has no field for it — a `cbc:HouseNote` your partner adds, a national element we have not mapped yet. It is
syntax-specific by definition, and there is nowhere in the other syntax for it to go. It is not lost from the
*source* — `invoice.Extensions()` still holds every one of them, and writing back in the original syntax
re-emits them unchanged — but it does not cross.

What this deliberately does **not** do is diff the two models field by field. Every mapped business term
survives by construction: both writers write from the same model, so a term that reached the model reaches
the target. Diffing would only ever re-discover that, at the cost of a second thing to keep in step.

## Round trips

A document that crosses and comes back says the same things it started with:

```csharp
ConversionResult there = library.Convert(ublXml, DocumentFormat.Cii);
ConversionResult back = library.Convert(there.Xml, DocumentFormat.Ubl);
```

What is **not** promised is that `back.Xml` equals `ublXml` byte for byte. Namespace prefixes, insignificant
whitespace and attribute order are not normative, and the two syntaxes format some values differently — CII
writes a date as `20260901` with `format="102"`, UBL as `2026-09-01`. The business content is what round-trips;
see [reading the raw value behind a field](raw-values.md) for what the library keeps of the original text.

## Validate the result, don't assume it

A lossless conversion is not the same as a valid document. The target profile may require a term the source
never carried — Factur-X BASIC needs things MINIMUM does not — and conversion cannot invent it. Validate what
comes out, in the profile you intend to send:

```csharp
ValidationReport report = library.Validate(result.Xml);
```

See [validating a document](validation.md).
