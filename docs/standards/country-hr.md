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
| CIUS-HR 2025 profile, with its extension | `.Countries.Croatia` | done — `HrProfiles.CiusHrUbl` |
| The 74 published CIUS-HR assertions | `.Countries.Croatia` | run once fetched — `AddCroatianRulesFrom` |
| Business process code (BT-23), `P1`–`P12` or `P99:` | `.Countries.Croatia` | done — `HrBusinessProcess` |
| Time of issue, and the operator's identity (HR-BT-2, 4, 5) | — | **not written** — see below |
| The advanced electronic seal | — | out of scope: this library does not sign |
| Fiscalisation messages to the tax administration | — | permanently out of scope: no network I/O |
| KPD classification code per line | caller | set it on `Item.ClassificationCodes` |

**The CIUS was not missing, it was unfetched.** It had been recorded here as "published nowhere this
repository can read", which was a statement about the fetch list rather than about the world: the publisher's
rules are aggregated by `phive-rules` as compiled XSLT, which this library reads, and which the fetch script
already pulled for four other countries. The identifier —
`urn:cen.eu:en16931:2017#compliant#urn:mfin.gov.hr:cius-2025:1.0#conformant#urn:mfin.gov.hr:ext-2025:1.0` —
came out of the same file as the assertions. It is one identifier and not two: CIUS-HR never travels without
its extension, and the rules test BT-24 for both at once.

Run `build/fetch-specs.sh national` and pass `specs/national/eracun/schematron` to `AddCroatianRulesFrom`.

**What an invoice this library writes still fails**, and it is exactly three assertions out of seventy-four:

| Rule | Wants | Why it is not written |
|---|---|---|
| `HR-BR-2` | `cbc:IssueTime` (HR-BT-2) | EN 16931 has no time of issue, so the model has nowhere to hold one |
| `HR-BR-37` | `cac:SellerContact/cbc:Name` (HR-BT-4) | the operator who issued the invoice; not a business term of the norm, and not the seller contact BG-6 maps to |
| `HR-BR-9` | `cac:SellerContact/cbc:ID` (HR-BT-5) | that operator's OIB, in the same place |

All three are ordinary UBL elements in ordinary UBL positions — no Croatian XML extension is involved, which
is worth knowing before anyone plans one. A [write pipeline step](../guides/hook-into-generation.md) can put
them in today; carrying them in the model is a decision still to be made.

**The KPD list lives inside the rule.** `HR-BR-CL-2` carries all 3 359 codes, so a plausible code that is not
one of them is refused as firmly as a missing one. The list is not redistributable, which is why this library
does not ship it as a code list; it is in the artefact you fetch.

## Why Croatia is not a cheap country to add

It looked like one from the outside — Peppol, EN 16931, a national CIUS — and it is not. The CIUS itself is
now carried, but two of the three things the mandate requires are outside what a document library does at
all:

- **A signature is not a document field.** The seal has to be produced with a certificate the invoicing
  system holds, and this library has taken the position that signing belongs to the caller. That is the same
  decision Italy and Spain will force, and it is still open — see the [roadmap](../roadmap.md).
- **The fiscalisation messages are transport.** Two reports, from two parties, on two schedules, to a tax
  administration. This library performs no network I/O at all, by design.

What is left — a valid invoice with both OIBs, in the right syntax, satisfying EN 16931, Peppol BIS and
seventy-one of the seventy-four Croatian assertions — is what `.Countries.Croatia` does, and it is genuinely
the hard half to get right.

## What is specifically Croatian

- **OIB** — eleven digits, the last a check digit under ISO/IEC 7064 MOD 11,10. Required for **both** parties,
  which EN 16931 does not ask for. The VAT number is the same digits with `HR` in front.
- **KPD** — every invoice line carries a Croatian classification code derived from CPA, under list `CG`, and it must be one of the 3 359 the rule enumerates.
- **Bank account details** are mandatory, where EN 16931 leaves them optional.

## Official sources

| Source | Use it for |
|---|---|
| <https://porezna-uprava.gov.hr> | The mandate, the fiscalisation rules, and the HR-FISK documentation. |
| <https://www.fina.hr> | FINA, the state intermediary. |
| <https://docs.peppol.eu/poacc/billing/3.0/> | The profile the CIUS restricts. |
| <https://ec.europa.eu/digital-building-blocks> (eInvoicing in Croatia) | Consolidated national requirements. |
