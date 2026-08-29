# Guides

Task-oriented documentation: each page answers "how do I…" for a real situation, with code you can compile.

## Producing invoices
- `issue-a-facturx-invoice.md` — pick a profile, build the invoice, get a PDF/A-3 *(planned)*
- `hook-into-generation.md` — run your own logic during generation: numbering, rounding, signing *(planned)*
- `convert-between-syntaxes.md` — UBL ↔ CII, and what conversion can lose *(planned)*

## Consuming invoices
- [raw-values.md](raw-values.md) — reach the raw text and XML attributes behind any field
- `read-an-unknown-invoice.md` — what you can extract from a document whose profile you do not support *(planned)*
- `understand-diagnostics.md` — read a parse result, choose a policy, escalate or suppress *(planned)*
- `understand-validation-reports.md` — what was checked, what was not, and why it matters *(planned)*

## Extending the library
- `extend-a-format.md` — add a field the norm does not have, or a partner-specific element *(planned)*
- `create-a-profile.md` — your own profile, registered from your own code *(planned)*

## Lifecycle
- `produce-and-read-cdar.md` — lifecycle status messages, including unknown profiles *(planned)*

## Migration
- `migrate-from-zugferd-csharp.md` — concept mapping from the most common .NET alternative *(planned)*

Pages marked *planned* land with the feature they document. A guide that describes an API which does not
exist yet is worse than no guide.
