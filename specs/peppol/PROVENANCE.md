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

Expected content: `rules/sch/PEPPOL-EN16931-UBL.sch`, `rules/sch/PEPPOL-EN16931-CII.sch`, and the
example documents.

Peppol adopts the EN 16931 artefacts and adds its own rules on top: a document must satisfy both rule sets.
The version of EN 16931 artefacts adopted by a given Peppol release is stated in the Peppol release notes —
record it here when fetching, because a mismatch produces false validation failures.
