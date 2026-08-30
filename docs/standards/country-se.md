# Sweden

> Recorded state: August 2026. Verify against DIGG before relying on dates.

## The mandate

Electronic invoicing to the public sector has been mandatory since **1 April 2019**, over Peppol. Sweden does
not publish a national CIUS: it exchanges **Peppol BIS Billing 3.0** itself, and adds national rules that
travel inside the Peppol rule set. B2B is not mandated.

## Scope for this library

| Capability | Package | Status |
|---|---|---|
| Peppol BIS Billing 3.0 | `.Peppol` | done |
| Swedish national rules (`SE-R-001` … `SE-R-013`) | inside the Peppol rule set | done |
| Organisationsnummer, scheme 0007 | `.Countries.Sweden` | done |
| Peppol transmission | — | permanently out of scope |

## What is specifically Swedish

- **Organisationsnummer** — ten digits whose last is the Luhn check digit of the first nine. The VAT number
  is the same digits with `SE` in front and `01` behind.
- **Payment identifiers.** Bankgiro and Plusgiro are the domestic way to be paid, and the Swedish rules have
  opinions about how they are declared (`SE-R-007` to `SE-R-012`): a Bankgiro or Plusgiro number belongs in
  the payment account identifier with the right scheme, not in a free-text credit transfer.
- **`SE-R-005`** — the F-tax statement, `Godkänd för F-skatt`, is checked as an exact string when present.

## How this library checks the identifier

`SeOrganisationNumber` implements the Luhn check, and a test hands every number it accepts — and a set it
refuses — to **Peppol's own rule** `PEPPOL-COMMON-R049`, run by this library's Schematron engine, and fails
on any disagreement.

## Official sources

| Source | Use it for |
|---|---|
| <https://www.digg.se/e-handel-och-e-faktura> | The Swedish requirements. |
| <https://docs.peppol.eu/poacc/billing/3.0/> | The profile itself. |
| <https://ec.europa.eu/digital-building-blocks> (eInvoicing in Sweden) | Consolidated national requirements. |
