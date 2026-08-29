# 0004 — Hand-written `XmlReader`/`XmlWriter` serialisation

**Status:** Amended 2026-08-29 — see *Amendment* below · originally accepted 2026-08-29

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

## Amendment — reading builds a tree, writing stays explicit

The decision was written as one rule for both directions. In practice they have different constraints.

**Writing keeps hand-written `XmlWriter`**, unchanged. Element order is normative in UBL and CII, and a writer
that emits the right elements in the wrong order produces documents recipients reject. That order must be
expressed in code, explicitly, one element at a time.

**Reading loads the document into an `XElement` tree**, through the hardened reader from `SecureXml` and with
line information enabled. Order is not normative on the way in, a hand-rolled state machine over `XmlReader`
would be far more error-prone for a tree-shaped mapping, and `IXmlLineInfo` gives every field the source
position that diagnostics and `Field<T>.Location` promise. Documents are bounded by `DocumentLimits` before
anything is loaded, so the memory argument for streaming does not apply to invoices.

Neither direction uses reflection or `XmlSerializer`, which is what the original decision was really about.
