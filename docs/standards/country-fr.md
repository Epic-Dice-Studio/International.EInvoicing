# France

> Regulatory dates move. Treat this page as a map, and the DGFiP specification package as the territory.
> Recorded state: August 2026.

## The reform in one paragraph

France mandates structured electronic invoicing for domestic B2B, exchanged through **approved platforms**
(*plateformes agréées*, formerly PDP). Every company must be able to **receive** electronic invoices from
1 September 2026. **Issuing** starts on 1 September 2026 for large companies and mid-caps, and on
1 September 2027 for SMEs and micro-enterprises. The public portal (PPF) was scaled back in October 2024 to a
central directory and a collection point for e-reporting; it no longer exchanges invoices itself.

Alongside invoicing, two obligations accompany it: **lifecycle statuses** (CDAR messages) and **e-reporting**
of transactions and payment data.

## Scope for this library

| Capability | Package | Status |
|---|---|---|
| Invoice syntaxes (UBL, CII, Factur-X) | `.Ubl`, `.Cii`, `.FacturX` | planned |
| French CIUS and business rules | `.Countries.France` | planned |
| CDAR lifecycle statuses, French profiling | `.Countries.France` | planned |
| SIREN / SIRET / VAT identifiers | `.Countries.France` | planned |
| E-reporting | — | researching, deferred past 1.0 |
| Transmission to an approved platform | — | permanently out of scope |

## Official sources

| Source | Use it for |
|---|---|
| <https://www.impots.gouv.fr/specifications-externes-b2b> | The authoritative package: specification, annexes, XSD, API definitions. Free. |
| <https://aife.economie.gouv.fr> | Programme status, calendar, platform registry. |
| AFNOR XP Z12-012 / -013 / -014 | Semantic model, CDAR profiling, directory. Sold by AFNOR. |
| <https://fnfe-mpe.org> | Factur-X, and practical French guidance. |

The DGFiP package is **not redistributable** — download it yourself, see `specs/fr-dse/PROVENANCE.md`.

## What is specifically French

- **The minimum accepted formats** are UBL, CII and Factur-X. A receiver must accept all three, which makes
  cross-syntax conversion a real requirement rather than a convenience.
- **Mandatory mentions beyond EN 16931**: SIREN of both parties, VAT payment option (*TVA sur les débits* or
  *sur les encaissements*), delivery address when it differs, and public-procurement references where relevant.
- **Identifiers**: SIREN (9 digits, Luhn), SIRET (14 digits, Luhn), and the French intra-community VAT number
  whose two check digits derive from the SIREN. Validate them, do not merely pattern-match them.
- **Lifecycle statuses**: a set of mandatory statuses and a set of optional ones, with a defined sequence.
  See [cdar.md](cdar.md).

## Pitfalls

- **"Factur-X" is not one thing.** MINIMUM and BASIC WL are not complete EN 16931 invoices; their legal use is
  narrow. Selecting a profile is a business decision, so the library must never pick one silently.
- **The specification is versioned and moving.** Record the version you implemented against in the tests.
- **E-reporting is not invoicing.** It has its own documents, its own periodicity, and it is not covered here
  yet — say so, rather than half-implementing it.
