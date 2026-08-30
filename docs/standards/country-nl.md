# Netherlands

> Recorded state: August 2026. Verify against Logius and the Dutch Standardisation Forum before relying on
> dates.

## The mandate

Electronic invoicing to central government has been mandatory since **1 January 2017**, and to the wider
public sector since 2019, over Peppol. B2B is not mandated. The national CIUS is **NLCIUS**, carried by
**SI-UBL 2.0**; Peppol BIS Billing is what crosses the border, and what the Dutch rules in the Peppol rule
set apply to.

## Scope for this library

| Capability | Package | Status |
|---|---|---|
| Peppol BIS Billing 3.0 | `.Peppol` | done |
| Dutch national rules (`NL-R-001` … `NL-R-009`) | inside the Peppol rule set | done |
| KvK and OIN legal entity schemes (0106, 0190) | `.Countries.Netherlands` | done |
| NLCIUS / SI-UBL 2.0 | — | **not carried** — see below |
| Peppol transmission, Digipoort | — | permanently out of scope |

**Why NLCIUS is absent.** Its published specification identifier is not in any artefact this repository
holds, and this library does not guess identifiers: a wrong one in BT-24 makes every document it writes
wrong, and makes documents it should read look unknown. Registering it from your own code takes a few lines
and wins over anything built in — see [add a profile](../recipes/add-a-profile.md). It goes in here the day
the identifier can be read from something authoritative.

## What is specifically Dutch

- **`NL-R-003` and `NL-R-005` are the rules to know.** When the supplier is Dutch, both parties' legal entity
  identifiers must carry scheme `0106` (KvK) or `0190` (OIN). An invoice naming both companies perfectly and
  omitting the scheme is refused, and nothing in EN 16931 hints at it. `NlLegalIdentifier` and
  `DutchEInvoicing.Describe` put it where the rule looks.
- **`NL-R-002`, `NL-R-004`, `NL-R-006`** — a Dutch party needs a street, a city and a postcode, not just a
  country.
- **`NL-R-009`** — an order line reference requires a document-level order reference to go with it.

## Official sources

| Source | Use it for |
|---|---|
| <https://www.logius.nl/domeinen/e-facturatie> | Dutch government e-invoicing. |
| <https://www.forumstandaardisatie.nl> | NLCIUS and SI-UBL. |
| <https://docs.peppol.eu/poacc/billing/3.0/> | The profile and the Dutch rules inside it. |
