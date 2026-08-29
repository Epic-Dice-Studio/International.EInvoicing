# 0001 — Native models and a canonical model, in two layers

**Status:** Accepted · 2026-08-29

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
