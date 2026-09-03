# Peppol BIS Billing 3.0 validation artefacts

| | |
|---|---|
| **Source** | <https://github.com/OpenPEPPOL/peppol-bis-invoice-3> |
| **Version** | 3.0, current quarterly release |
| **Retrieved** | *(not yet fetched)* |
| **Licence** | **none declared upstream** |
| **Redistributable** | **no** — nothing from this folder is committed |

`build/fetch-specs.sh peppol` downloads these into `specs/peppol/rules/`, which is git-ignored. The
repository at <https://github.com/OpenPEPPOL/peppol-bis-invoice-3> declares no licence, so redistribution is
not established; the artefacts are governed by OpenPEPPOL's own terms. Fetch them yourself.

Re-checked August 2026: the repository still carries no `LICENSE`, `COPYING` or `NOTICE` file and no
licensing statement in its README. See [the standards page](../../docs/standards/peppol-bis-3.md) for what
can be validated without them, and how.

Expected content: `rules/PEPPOL-EN16931-{UBL,CII}.sch`, `rules/CEN-EN16931-{UBL,CII}.sch`, the example
documents in `examples/`, and Peppol's unit corpus in `unit-UBL-PEPPOL/` and `unit-CII-PEPPOL/` — each case
naming how many times a rule should fire, which is what the engine is measured against.

Peppol adopts the EN 16931 artefacts and adds its own rules on top: a document must satisfy both rule sets.
The version of EN 16931 artefacts adopted by a given Peppol release is stated in the Peppol release notes —
record it here when fetching, because a mismatch produces false validation failures.

## Peppol PINT

| | |
|---|---|
| **Source** | <https://github.com/phax/phive-rules> (`phive-rules-peppol-pint`), which carries what OpenPEPPOL publishes |
| **Fetched by** | `build/fetch-specs.sh pint` |
| **Redistributable** | **No.** Same position as the BIS rules above: OpenPEPPOL declares no licence permitting it. |

PINT is what every Peppol jurisdiction outside Europe runs on. What is fetched here is **pre-compiled XSLT**,
not source Schematron — OpenPEPPOL publishes it that way, and this library's engine executes Schematron. So
these artefacts are not a rule set that runs; they are the source the jurisdiction identifiers in
`PeppolPintProfiles` are checked against, which `PeppolPintProfilesTests` does on every build once they are
present.

Running them needs an XSLT processor. That is an open item — see `docs/roadmap.md`.

## The Peppol post-award documents that are not invoices

| | |
|---|---|
| **Source** | <https://github.com/OpenPEPPOL/poacc-upgrade-3> — examples, use cases and the unit corpus; <https://github.com/phax/phive-rules> (`phive-rules-peppol`) for the runnable rule sets |
| **Version** | Invoice Response transaction 3.1 and Message Level Response, OpenPEPPOL release 2026.5 |
| **Fetched by** | `build/fetch-specs.sh poacc` |
| **Licence** | **none declared upstream** |
| **Redistributable** | **no** — nothing under `specs/peppol/poacc/` is committed |

`poacc-upgrade-3` is OpenPEPPOL's development repository for the post-award documents, and carries no
`LICENSE`, `COPYING` or `NOTICE` file — the same position as `peppol-bis-invoice-3` above. Fetch them
yourself.

**The rule sets are fetched from a different place than their sources, and that is deliberate.** The `.sch`
files in `poacc-upgrade-3/rules/sch` are not whole: each one `include`s a `target/generated/T*-basic.sch`
that the repository's own build produces from its structure spreadsheets and does not commit. Running them
as published would silently drop the structural half of every rule set. What is fetched instead is the
**compiled** form phive-rules carries, which is complete — the assertions are recovered from it by
`CompiledSchematron`, the same path Croatia and the tax data documents take.

Expected content:

| | |
|---|---|
| `poacc/examples/` | `InvoiceResponse_Example.xml`, `MessageLevelResponse_Example.xml` and the thirteen `T111-uc*` use cases — one per business situation a response reports — plus `DespatchAdvice_Example.xml` and its five use cases, and `Order_Example.xml` and its six |
| `poacc/unit-invoice-response/`, `poacc/unit-despatch-advice/`, `poacc/unit-order/` | Peppol's unit corpora: each case names how many times a rule should fire |
| `poacc/codelist/` | `UNCL4343-T111.xml`, `OPStatusReason.xml`, `OPStatusAction.xml` — what `PeppolResponseCodes` is compared against on every build they are present for |
| `poacc/rules/` | `PEPPOLBIS-T111.xslt`, `PEPPOLBIS-T71.xslt`, `PEPPOLBIS-T16.xslt`, `PEPPOLBIS-T01.xslt` |

Each rule set governs one transaction and is registered against its profile. Both documents share a root
element and differ in what they mean, so a rule set let loose on the other's documents reports failures that
are not in them — twelve of them, on OpenPEPPOL's own example.

