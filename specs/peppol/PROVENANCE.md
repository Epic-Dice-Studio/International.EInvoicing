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
