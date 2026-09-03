# Guides

Task-oriented documentation: each page answers "how do I…" for a real situation, with code you can compile.

**New here? Start with [getting started](getting-started.md).** Every page below has a matching chapter in
the [runnable sample](https://github.com/Epic-Dice-Studio/International.EInvoicing/blob/main/samples/README.md) — `dotnet run --project samples/International.EInvoicing.Samples`.

## The whole job

| | |
|---|---|
| [Getting started](getting-started.md) | Install, read, write, check — in one page |
| [The command line](command-line.md) | `einvoice validate`, `inspect` and `convert`, without writing any code |
| [One country, one object](country-shortcuts.md) | France, Germany, Belgium — one type each, everything that country needs |
| [Reading a document](reading.md) | Hand over a stream, get back an invoice, credit note or lifecycle status |
| [Writing a document](writing.md) | Builders, credit notes, hybrid Factur-X, anything the norm has no field for |
| [Lifecycle statuses](lifecycle.md) | Every French status, the Peppol Invoice Response, and who reports what to whom |
| [E-reporting](e-reporting.md) | The French flux 10 report: sales, transactions abroad, and when the money arrived |
| [Validation](validation.md) | What ran, what failed, and your own rule sets |
| [Converting between syntaxes](convert-between-syntaxes.md) | UBL ↔ CII, and a report of what the crossing cost |
| [The readable copy, and what came with it](attachments.md) | The PDF a hybrid invoice arrived in, and the documents BG-24 carries |
| [Hooking into generation](hook-into-generation.md) | Your own numbering, rounding or signature, on every document written |
| [Testing your integration](testing.md) | Conforming samples, a round-trip harness, hostile documents, assertions |
| [Raw values](raw-values.md) | The text and attributes behind any field |
| [National identifiers](identifiers.md) | SIREN, SIRET, VAT, Leitweg-ID, KBO/BCE, structured communication |

## Going further

- `extend-a-format.md` — add a field the norm does not have *(planned)*
- `create-a-profile.md` — your own profile, registered from your own code — see
  [the recipe](../recipes/add-a-profile.md) meanwhile
- `migrate-from-zugferd-csharp.md` — concept mapping from the most common .NET alternative *(planned)*

Pages marked *planned* land with the feature they document. A guide describing an API that does not exist is
worse than no guide.

## Layers

Everything has a short way and a specific way, and the specific one is never hidden.

| Layer | Use it when | Entry point |
|---|---|---|
| Highest | You have a document and want an object | `EInvoicing.Read` / `.Write` / `.Validate` |
| Per syntax | You already know what you hold | `einvoicing.Ubl`, `.Cii`, `.Lifecycle`, `.UblWriter` … |
| Per country | You need what a country adds | `FrCdar`, `FrLifecycleStatus` |
| Model | You need a field the builders do not cover | `EInvoice`, `InvoiceLine`, `.Extend(…)` |
| Extension data | The norm has no field for it | `node.Extensions` |
| Replacement | Ours is wrong for you | Register your own through the registry |
