# 0004 — Hand-written `XmlReader`/`XmlWriter` serialisation

**Status:** Accepted · 2026-08-29

## Context

Two alternatives existed: generate classes from the XSD and use `XmlSerializer`, or write readers and writers
by hand. Generation is cheap to produce and cheap to update.

## Decision

Write them by hand.

## Consequences

- Element order — which is normative in UBL and CII, and the most common cause of rejected invoices — is
  controlled explicitly rather than inferred from attribute placement.
- No reflection: fast, trimmable, AOT-friendly, and able to run in WebAssembly for the demo site.
- Namespaces, decimal formatting and attribute handling are explicit, which is what `Field<T>` requires.
- The cost is a large amount of mapping code. It is repetitive and well specified, which makes it good work
  to parallelise across contributors and coding agents — hence the recipes in `docs/recipes/`.
- Updating to a new schema version is manual. The `spec-sync` workflow raises an issue when upstream moves.
