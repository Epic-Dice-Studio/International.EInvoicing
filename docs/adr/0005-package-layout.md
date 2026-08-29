# 0005 — Core + one package per format + one per country

**Status:** Accepted · 2026-08-29

## Context

Requirements split three ways: universal, shared by several countries, or genuinely national. A single package
would force a French user to carry German rules; a package per profile would produce dozens of artefacts and a
dependency maze.

## Decision

Three axes: `Core`, one package per format (`Ubl`, `Cii`, `Cdar`, `FacturX`, `Peppol`, validation rule sets),
one per country (`Countries.France`, `Countries.Germany`, `Countries.Belgium`), plus a convenience
meta-package.

Naming rules, enforced in review:

- universal capability → `Core` or the format package;
- shared by several countries → the format package, *used* by each country, never copied;
- national → `Countries.<Country>`, public types prefixed with the ISO 3166 alpha-2 code;
- universal capability with national behaviour → a Core abstraction with one implementation per country.

## Consequences

- Belgium depends on `Peppol`; it does not restate Peppol rules.
- Users install what they need.
- All packages version together from a single git tag. Independent versioning was considered and rejected as
  premature: it can be introduced later, the reverse is much harder.
