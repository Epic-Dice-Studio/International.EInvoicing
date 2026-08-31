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
| NLCIUS and its G-account extension | `.Countries.Netherlands` | done |
| NLCIUS rules (SI-UBL, nlcius-cii) | fetched — `AddNlciusRulesFrom` | done |
| Peppol transmission, Digipoort | — | permanently out of scope |

**NLCIUS was absent, and now is not.** It was left out on the grounds that its specification identifier was
in no artefact this repository held — which was true of the artefacts it held then. The identifier is in the
Dutch rule set itself, which `build/fetch-specs.sh national` now fetches, so it is read from there:

```
urn:cen.eu:en16931:2017#compliant#urn:fdc:nen.nl:nlcius:v1.0
urn:cen.eu:en16931:2017#compliant#urn:fdc:nen.nl:nlcius:v1.0#conformant#urn:fdc:nen.nl:gaccount:v1.0
```

The lesson is worth keeping: *"not in any artefact we hold"* is a statement about the fetch list, not about
the world. The rules are published as pre-compiled XSLT, which this library reads as data — see
[Peppol PINT](peppol-pint.md) for how.

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
