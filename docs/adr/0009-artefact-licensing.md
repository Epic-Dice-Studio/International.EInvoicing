# 0009 — Licensing of the normative artefacts we redistribute

**Status:** Accepted · 2026-08-29

## Context

The plan assumed the validation artefacts were Apache-2.0. Checking them when they were first fetched showed
otherwise:

| Artefact | Actual licence |
|---|---|
| EN 16931 Schematron (`ConnectingEurope/eInvoicing-EN16931`) | **EUPL-1.2** |
| XRechnung Schematron and test suite (`itplr-kosit/*`) | Apache-2.0 |
| Peppol BIS Billing 3.0 artefacts (`OpenPEPPOL/peppol-bis-invoice-3`) | **none declared** |

This matters twice over. This repository is MIT, and it ships a NuGet package that is meant to embed
validation artefacts as resources.

## Decision

**Peppol artefacts are not redistributed.** A repository with no declared licence grants nothing.
`build/fetch-specs.sh peppol` downloads them into `specs/peppol/rules/`, which is git-ignored, and
`PROVENANCE.md` says why. Anyone who needs them fetches them under OpenPEPPOL's own terms.

**EN 16931 artefacts are redistributed verbatim, under EUPL-1.2**, with `LICENSE.upstream.txt` beside them
and an entry in `NOTICE`. They are not relicensed: MIT covers this repository's own code, not these files.

**Apache-2.0 artefacts (XRechnung) are redistributed** with their licence and a `NOTICE` entry, as that
licence requires.

## Resolved by the project owner, 2026-08-29

Verbatim redistribution of the EUPL artefacts alongside MIT code is **accepted**: the files keep their own
licence, the repository keeps MIT, and mere aggregation creates no derivative work. The project stays MIT.

Peppol artefacts stay **out of the repository**, fetched locally by anyone who needs them. OpenPEPPOL's
published terms are explicit — Peppol BIS documents may not be redistributed or repackaged without their
prior consent — so this is not a gap to be closed later but the intended arrangement. Asking OpenPEPPOL for
consent remains possible if the local fetch ever becomes a burden.

## What remains open

The EUPL is reciprocal, so **compiling the rules into C#** — deriving from them rather than running them —
would put the generated code under EUPL inside an MIT package. That is now an argument rather than a
blocker: [ADR 0008](0008-schematron-engine.md) prefers executing the artefacts as data precisely because
doing so raises no derivation question at all. Should that route prove impossible and code generation become
necessary, the question returns and deserves an answer before shipping.

## Consequences

- Peppol validation works from a local fetch rather than from anything shipped, and the support matrix says
  so.
- Every folder under `specs/` carries a `PROVENANCE.md` naming the actual licence, verified at fetch time
  rather than assumed. The three assumptions this ADR corrects were all wrong.
- `build/fetch-specs.sh` copies the upstream licence next to the artefacts it covers.
