# Belgium

> Recorded state: August 2026. Verify against the FPS Finance guidance before relying on dates.

## The mandate

Structured electronic invoicing is mandatory for domestic B2B from **1 January 2026**. Belgium chose a
decentralised model built on **Peppol**: Peppol BIS Billing 3.0 over the Peppol network is the default, and
any other format requires both parties to agree and must still comply with EN 16931.

The **Hermes** fallback platform, which bridged non-connected companies, was decommissioned at the end of
2025 — being on Peppol is now the baseline, not an option. E-reporting is announced for 2028.

For the public sector, **Mercurius** remains the entry point.

## Scope for this library

| Capability | Package | Status |
|---|---|---|
| Peppol BIS Billing 3.0 | `.Peppol` | planned |
| Belgian national rules | `.Countries.Belgium` | planned |
| KBO/BCE enterprise number | `.Countries.Belgium` | planned |
| Structured communication (`+++nnn/nnnn/nnnnn+++`) | `.Countries.Belgium` | planned |
| Peppol transmission, Mercurius | — | permanently out of scope |

Belgium is the clearest illustration of the layering rule in `AGENTS.md`: nearly everything Belgian is
Peppol, so it lives in `.Peppol` and is *used* by `.Countries.Belgium`, never copied into it.

## Official sources

| Source | Use it for |
|---|---|
| <https://ec.europa.eu/digital-building-blocks> (eInvoicing in Belgium) | Consolidated national requirements. |
| <https://docs.peppol.eu/poacc/billing/3.0/> | The profile itself. |
| <https://e-invoice.belgium.be> | Federal guidance and Mercurius. |

## What is specifically Belgian

- **KBO/BCE enterprise number** — 10 digits with a modulo-97 check, and the VAT number derives from it.
- **Structured communication** — the `+++nnn/nnnn/nnnnn+++` payment reference, with a modulo-97 check digit.
  It maps to `BT-83` (remittance information) and Belgian receivers depend on it for reconciliation.
- **Language** — invoices circulate in Dutch, French and German; `TextField.LanguageId` is not decoration here.

## Pitfalls

- **"Peppol BIS plus national rules" is two rule sets**, like everywhere else.
- **The structured communication is checksummed.** Emitting a syntactically plausible but invalid reference
  produces invoices that reconcile against nothing.

## In code

```csharp
services.AddEInvoicing(library => library.AddDefaults().AddBelgium());
```

`AddBelgium()` is `AddPeppol()` plus the Belgian identifiers: the mandate is Peppol BIS Billing, not a
Belgian format. Add the rules once you have fetched them —
`AddPeppolRulesFrom("specs/peppol/rules")` — and see the
[Peppol standards page](peppol-bis-3.md).
