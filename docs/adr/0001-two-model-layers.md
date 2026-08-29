# 0001 — Native models and a canonical model, in two layers

**Status:** Amended 2026-08-29 — see *Amendment* below · originally accepted 2026-08-29

## Context

An invoice can be modelled at two levels: faithfully to its XML schema, or semantically as EN 16931 business
terms. Libraries usually pick one. Picking the schema gives fidelity and an unpleasant API; picking the
semantic model gives a pleasant API and loses everything the norm does not cover — which is exactly what a
developer needs when a partner sends something unusual.

## Decision

Ship both, layered. The low layer holds native models per syntax (`UblInvoice`, `CrossIndustryInvoice`,
`CdarMessage`), faithful to the schema. The high layer holds the canonical `EInvoice`, mirroring EN 16931.
Mappers connect them. Both layers use the same `Field<T>` value system, so raw access is available at either
level.

## Consequences

- Cross-syntax conversion falls out of the design: UBL → canonical → CII.
- Most developers only ever see the canonical model; the native layer is there when the norm is not enough.
- The cost is real: two models and a mapper per syntax. This is the single largest cost in the project, and
  it is accepted deliberately.
- The canonical model must never gain syntax-specific concepts. When tempted, the answer is `ExtensionData`
  or the native layer.

## Amendment — the native layer is deferred, not dropped

Once `ExtensionData` existed on every node of the canonical model, the low layer lost most of what justified
it. An element the reader does not map is already kept verbatim and written back unchanged, so "reading a
document loses nothing" holds without a second model.

What the native layer would still buy is *typed* access to elements outside EN 16931 — Factur-X EXTENDED,
the XRechnung Extension. That is real, but it roughly doubles the model and mapper work for every syntax, and
nobody has asked for it yet. AGENTS.md forbids speculative generality for exactly this situation.

**Decision:** readers and writers map directly to and from the canonical model. Anything outside it goes to
`ExtensionData`. A native layer is added for a specific syntax when a concrete need appears, and this ADR is
amended again when it does.
