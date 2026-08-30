# Guides

Task-oriented documentation: each page answers "how do I…" for a real situation, with code you can compile.

**New here? Start with [getting started](getting-started.md).**

## The whole job

| | |
|---|---|
| [Getting started](getting-started.md) | Install, read, write, check — in one page |
| [Reading a document](reading.md) | Hand over a stream, get back an invoice, credit note or lifecycle status |
| [Writing a document](writing.md) | Builders, credit notes, hybrid Factur-X, anything the norm has no field for |
| [Lifecycle statuses](lifecycle.md) | Every French status, and how sending to a partner differs from the public portal |
| [Validation](validation.md) | What ran, what failed, and your own rule sets |
| [Raw values](raw-values.md) | The text and attributes behind any field |
| [National identifiers](identifiers.md) | SIREN, SIRET, VAT, Leitweg-ID, KBO/BCE, structured communication |

## Going further

- `extend-a-format.md` — add a field the norm does not have *(planned)*
- `create-a-profile.md` — your own profile, registered from your own code — see
  [the recipe](../recipes/add-a-profile.md) meanwhile
- `hook-into-generation.md` — run your own logic during generation *(planned)*
- `convert-between-syntaxes.md` — UBL ↔ CII, and what conversion can lose *(planned)*
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
