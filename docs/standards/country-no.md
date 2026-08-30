# Norway

> Recorded state: August 2026. Verify against DFØ and Anskaffelser.no before relying on dates.

## The mandate

Norway has required electronic invoicing to the public sector since 2019, in **EHF** — *Elektronisk
handelsformat* — exchanged over the Peppol network. B2B is not mandated but adoption is high, and EHF is what
Norwegian trading partners expect.

## What EHF is

EHF 3.0 is a **CIUS of Peppol BIS Billing 3.0**, which is itself a CIUS of EN 16931. Its specification
identifier carries all three, in that order:

```
urn:cen.eu:en16931:2017#compliant#urn:fdc:peppol.eu:2017:poacc:billing:3.0#compliant#urn:www.difi.no:ehf:ver3.0
```

That identifier is not transcribed here from a specification: it appears verbatim in Peppol's own published
unit corpus (`specs/peppol/unit-UBL-PEPPOL/PEPPOL-EN16931-R004.xml`), which this repository fetches.

## Scope for this library

| Capability | Package | Status |
|---|---|---|
| EHF 3.0 profile, both syntaxes | `.Countries.Norway` | done |
| Peppol BIS Billing 3.0 | `.Peppol` | done |
| Norwegian national rules (`NO-R-001`, `NO-R-002`) | inside the Peppol rule set | done |
| Organisasjonsnummer, scheme 0192 | `.Countries.Norway` | done |
| Peppol transmission | — | permanently out of scope |

## What is specifically Norwegian

- **Organisasjonsnummer** — nine digits with a modulo 11 check, weights 3, 2, 7, 6, 5, 4, 3, 2. A remainder
  that leaves a check digit of 10 means no valid number exists, so those are refused rather than rounded.
  The VAT number is the same digits with `NO` in front and `MVA` behind.
- **The national rules travel inside the Peppol rule set.** There is no separate Norwegian Schematron to
  fetch: `AddPeppolRulesFrom(directory)` brings `NO-R-001` and `NO-R-002` with everything else.

## How this library checks the identifier

Not by trusting the transcription. `NoOrganisationNumber` implements the modulo 11 check, and a test hands
every number it accepts — and a set it refuses — to **Peppol's own rule** `PEPPOL-COMMON-R041`, run by this
library's Schematron engine, and fails on any disagreement.

## Official sources

| Source | Use it for |
|---|---|
| <https://anskaffelser.no/verktoy/standarder/elektronisk-handelsformat-ehf> | The EHF specifications. |
| <https://docs.peppol.eu/poacc/billing/3.0/> | The profile EHF restricts. |
| <https://ec.europa.eu/digital-building-blocks> (eInvoicing in Norway) | Consolidated national requirements. |
