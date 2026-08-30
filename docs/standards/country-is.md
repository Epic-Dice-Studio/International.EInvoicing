# Iceland

> Recorded state: August 2026. Verify against Fjársýsla ríkisins before relying on dates.

## The mandate

Electronic invoicing to the public sector has been mandatory since **2019**, over Peppol. Iceland does not
publish a national CIUS: it exchanges **Peppol BIS Billing 3.0**, with Icelandic rules that travel inside the
Peppol rule set.

## Scope for this library

| Capability | Package | Status |
|---|---|---|
| Peppol BIS Billing 3.0 | `.Peppol` | done |
| Icelandic national rules (`IS-R-001` … `IS-R-010`) | inside the Peppol rule set | done |
| Kennitala, scheme 0196 | `.Countries.Iceland` | done |
| Peppol transmission | — | permanently out of scope |

## What is specifically Icelandic

- **Kennitala** — ten digits: eight of identity, a modulo 11 check digit, and a century marker. This library
  checks the check digit and deliberately does not interpret the date part, since a business kennitala adds
  40 to the day and a library has no business deciding which of the two a caller meant.
- **`IS-R-002` and `IS-R-004` are fatal**: both parties need a legal entity identifier carrying scheme
  `0196`. That is the rule `IcelandicEInvoicing.Describe` exists to satisfy, and a test removes the scheme
  from a document this library wrote to confirm the rule really is what rejects it.

## Official sources

| Source | Use it for |
|---|---|
| <https://www.fjs.is> | Icelandic public-sector invoicing. |
| <https://docs.peppol.eu/poacc/billing/3.0/> | The profile and the Icelandic rules inside it. |
| <https://ec.europa.eu/digital-building-blocks> (eInvoicing in Iceland) | Consolidated national requirements. |
