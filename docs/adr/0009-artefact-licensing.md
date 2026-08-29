# 0009 — Licensing of the normative artefacts we redistribute

**Status:** Accepted, with one open question · 2026-08-29

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

## The open question

The EUPL is a **reciprocal** licence. Redistributing the Schematron files unchanged is clearly permitted.
Two things are not clearly permitted, and both are on the roadmap:

1. **Compiling the rules into C#** — ADR 0008's preferred route. A generated rule class is plausibly a
   derivative work of the Schematron, which would put it under EUPL rather than MIT.
2. **Embedding the artefacts in a NuGet package** — distribution of the Work, which the EUPL allows, but
   which requires the licence and notices to travel with it and constrains how the whole is offered.

Until this is resolved, the safe positions are: keep the artefacts as separate files carrying their own
licence rather than fusing them into compiled output, and treat ADR 0008's option 3 as **blocked pending
legal review**, not merely as a technical spike.

The EUPL's Article 5 compatibility clause and the fact that CEN publishes these artefacts precisely so that
implementations can use them both suggest a permissive answer. Neither is a substitute for checking. This is
flagged for the project owner rather than decided here.

## Consequences

- The support matrix cannot claim Peppol validation until either the artefacts gain a licence, or the rules
  are obtained another way, or the user fetches them.
- Every folder under `specs/` carries a `PROVENANCE.md` naming the actual licence, verified at fetch time
  rather than assumed. The three assumptions this ADR corrects were all wrong.
- `build/fetch-specs.sh` copies the upstream licence next to the artefacts it covers.
