# French DGFiP external specifications (B2B)

| | |
|---|---|
| **Source** | <https://www.impots.gouv.fr/specifications-externes-b2b>, <https://github.com/phax/phive-rules> |
| **Version** | specification package v3.0, reviewed against v3.2; CTC rules and lifecycle samples 1.4.0.04; e-reporting (flux 10) 1.0 |
| **Retrieved** | 2026-09-07 |
| **Licence** | DGFiP — free of charge, redistribution not granted; phive-rules declares no licence of its own |
| **Redistributable** | no, apart from the worked examples noted below |

It is the authoritative source for: the French CIUS, the CDAR lifecycle status codes and their sequencing,
directory identifiers, and e-reporting (which is deferred past 1.0).

The AFNOR standards XP Z12-012 / -013 / -014 that underpin it are sold by AFNOR and are likewise not
redistributable.

## What `build/fetch-specs.sh france` fills, and what is committed

The rule sets and lifecycle samples are carried by phive-rules, which declares no licence. They are fetched
into git-ignored folders — `rules/ctc/`, `rules/flux10/`, `schemas/flux10/`, `samples/` — and never
committed. The pins live in `build/fetch-specs.sh` as `FRENCH_RULES_VERSION` and `FRENCH_FLUX10_VERSION`;
phive-rules publishes no release tag for them, only one directory per DGFiP version, so `.github/workflows/
spec-sync.yml` watches those directories instead.

The exception is `examples/`, which **is** committed: the DGFiP's own worked examples — an invoice in both
syntaxes, the lifecycle messages, and the three e-reporting flows — ship inside the specification package
rather than in anybody's repository, and the tests that measure this library against them would otherwise
have nothing to run.

## What v3.2 changed, and what it changed here

The package moved twice since v3.0 — v3.1 (30/10/2025) and v3.2 (30/04/2026). The model was read against
both, annexe by annexe. Almost nothing needed doing, for a reason worth recording: the rule sets and the
flux 10 schemas are fetched from phive-rules at `master`, which already carries the v3.2 artefacts, and the
French code lists in `Countries.France` were built from those rather than from the v3.0 package.

- **E-reporting.** `References` (TG-2, TT-5, TT-6) was deleted and `TransactionsCount` (TT-85) relaxed to
  `0..1`. This library never modelled the first and always treated the second as optional. The five
  `schemas/flux10/1.0/*.xsd` fetched from phive-rules are, normalised for whitespace, identical to the
  v3.2 package's own — not to v3.0's.
- **Transmission types.** TT-4 lost `CO` and `MO`; `FrEReportCodes` only ever offered `IN` and `RE`.
- **The CDV sender.** MDT-18 is now `0238` (PDP/PPF matricule) alone and MDT-19 is four characters, which is
  what `FrCdar.FromPlatform` and `SentBy` have always written.
- **Deleted CDV block.** MDG-36 and MDT-107 to MDT-109 — `ram:IncludedNote` — were informative and are gone.
  Nothing here wrote them.
- **What did need doing.** MDT-217 stopped being a unit code and became a measured quantity, with the unit
  moving to a new MDT-217-1 `@unitCode`. That, and the rest of the MDG-43 characteristic block, is now
  modelled on `DocumentStatusCharacteristic`: `ValueMeasure`, `ValueDateTime`, `ValueCode`, `ValueQuantity`,
  `ValueNumeric`, plus `Description` and `AdjustmentDirectionCode`. `ValueDateTime` is the one the DGFiP's
  own corpus exercises — the CDV-211 sample dates the payment with it, as `CCYYMMDD`, format 102.

Field lengths, business definitions and required/optional changes are enforced by the Schematron, not by
this library, and that is fetched at 1.4.0.04.

## Why the specification package stays at v3.0

`DGFIP_SPEC_VERSION` is pinned to `v3.0` on purpose, not by neglect: it is the last package to carry the
`Exemples` folder. v3.1 and v3.2 dropped it. The pin is therefore deliberately behind, and `spec-sync.yml`
does not report on it — a newer package would not bring back what this pin is here for. The PDF, annexes,
XSD and Swagger definitions of the current package must still be downloaded by hand.
