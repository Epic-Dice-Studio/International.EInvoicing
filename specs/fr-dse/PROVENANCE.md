# French DGFiP external specifications (B2B)

| | |
|---|---|
| **Source** | <https://www.impots.gouv.fr/specifications-externes-b2b>, <https://github.com/phax/phive-rules> |
| **Version** | specification package v3.0; CTC rules and lifecycle samples 1.4.0.04; e-reporting (flux 10) 1.0 |
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

## Why the specification package stays at v3.0

`DGFIP_SPEC_VERSION` is pinned to `v3.0` on purpose, not by neglect: it is the last package to carry the
`Exemples` folder. v3.1 and v3.2 dropped it. The pin is therefore deliberately behind, and `spec-sync.yml`
does not report on it — a newer package would not bring back what this pin is here for. The PDF, annexes,
XSD and Swagger definitions of the current package must still be downloaded by hand.
