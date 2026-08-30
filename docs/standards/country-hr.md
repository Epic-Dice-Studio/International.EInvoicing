# Croatia

> Recorded state: August 2026. Verify against the Porezna uprava before relying on dates or details.

## The mandate

**Fiskalizacija 2.0** made structured e-invoicing mandatory for domestic B2B between VAT-registered,
Croatian-established taxpayers on **1 January 2026**. It is a continuous-transaction-control regime, not just
an invoicing format: the invoice travels one way and a fiscalisation message travels another.

Three things happen for every invoice:

1. **The invoice** — UBL 2.1, EN 16931-compliant, restricted by the **HR-FISK 2.0** CIUS — is exchanged over
   a five-corner Peppol-style network through certified intermediaries.
2. **The issuer reports it** to the tax administration immediately.
3. **The recipient reports it too**, within five working days.

## Scope for this library

| Capability | Package | Status |
|---|---|---|
| Peppol BIS Billing 3.0, both syntaxes | `.Peppol` | done |
| OIB, with its ISO/IEC 7064 MOD 11,10 check | `.Countries.Croatia` | done |
| OIB on both parties, as the mandate requires | `.Countries.Croatia` | done |
| HR-FISK 2.0 CIUS profile and rules | — | **not carried** — see below |
| The advanced electronic seal | — | out of scope: this library does not sign |
| Fiscalisation messages to the tax administration | — | permanently out of scope: no network I/O |
| KPD classification code per line | caller | set it on `Item.ClassificationCodes` |

**Why HR-FISK 2.0 is absent.** Its published specification identifier is not in any artefact this repository
carries. This library does not guess identifiers: a wrong value in BT-24 makes every document it writes
wrong, and makes documents it should read look unknown. Registering it from your own code takes a few lines
and wins over anything built in — see [add a profile](../recipes/add-a-profile.md). The same applies to the
HR-FISK Schematron, which loads like any other rule set once you have it.

## Why Croatia is not a cheap country to add

It looked like one from the outside — Peppol, EN 16931, a national CIUS — and it is not. Two of the three
things the mandate requires are outside what a document library does at all:

- **A signature is not a document field.** The seal has to be produced with a certificate the invoicing
  system holds, and this library has taken the position that signing belongs to the caller. That is the same
  decision Italy and Spain will force, and it is still open — see the [roadmap](../roadmap.md).
- **The fiscalisation messages are transport.** Two reports, from two parties, on two schedules, to a tax
  administration. This library performs no network I/O at all, by design.

What is left — a valid invoice with both OIBs, in the right syntax, satisfying EN 16931 and Peppol BIS — is
what `.Countries.Croatia` does, and it is genuinely the hard half to get right.

## What is specifically Croatian

- **OIB** — eleven digits, the last a check digit under ISO/IEC 7064 MOD 11,10. Required for **both** parties,
  which EN 16931 does not ask for. The VAT number is the same digits with `HR` in front.
- **KPD** — every invoice line carries a six-digit Croatian classification code, derived from CPA.
- **Bank account details** are mandatory, where EN 16931 leaves them optional.

## Official sources

| Source | Use it for |
|---|---|
| <https://porezna-uprava.gov.hr> | The mandate, the fiscalisation rules, and the HR-FISK documentation. |
| <https://www.fina.hr> | FINA, the state intermediary. |
| <https://docs.peppol.eu/poacc/billing/3.0/> | The profile the CIUS restricts. |
| <https://ec.europa.eu/digital-building-blocks> (eInvoicing in Croatia) | Consolidated national requirements. |
