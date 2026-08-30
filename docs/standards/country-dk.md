# Denmark

> Recorded state: August 2026. Verify against Erhvervsstyrelsen before relying on dates.

## The mandate

Electronic invoicing to the public sector has been mandatory since 2005 — Denmark was first in Europe — over
**NemHandel**. The domestic format has been **OIOUBL 2.1**, with Peppol BIS Billing used for cross-border
exchange and increasingly domestically.

**This is changing.** In March 2026 the Danish Business Authority cancelled the planned OIOUBL 3.0 and
committed instead to **NemHandel BIS 4**, an adaptation of Peppol BIS 4, as the single accepted domestic
format by mid-2029. Peppol BIS 4 is built on EN 16931-1:2026 and merges BIS Billing with PINT; neither is
published yet.

## Scope for this library

| Capability | Package | Status |
|---|---|---|
| Peppol BIS Billing 3.0 | `.Peppol` | done |
| Danish national rules (`DK-R-002` … `DK-R-017`) | inside the Peppol rule set | done |
| CVR, SE and P numbers — schemes 0184, 0198, 0096 | `.Countries.Denmark` | done |
| Allowed payment means (`DK-R-005`) | `.Countries.Denmark` | done |
| OIOUBL 2.1 | — | not carried; its own syntax, see the roadmap |
| NemHandel BIS 4 | — | blocked on Peppol BIS 4 and EN 16931-1:2026 |
| NemHandel transmission | — | permanently out of scope |

## What is specifically Danish

- **CVR number** — eight digits, optionally prefixed `DK`. Peppol checks the shape and not the modulo 11
  check digit a CVR also carries, and this library follows it: rejecting a number the receiving access point
  would have accepted is a worse failure than letting a typo through.
- **The payment means trap.** Code `30`, plain credit transfer, is valid EN 16931 and is **refused** between
  two Danish parties by `DK-R-005`, a fatal rule. The allowed set is 1, 10, 31, 42, 48, 49, 50, 58, 59, 93
  and 97; `DkPaymentMeans.SepaCreditTransfer` is the ordinary answer. `DkPaymentMeans.All` is read out of the
  rule itself, and a test compares the two.
- **`DK-R-014`** — a Danish supplier must declare its party identification with a scheme identifier, never
  bare.

## Official sources

| Source | Use it for |
|---|---|
| <https://nemhandel.dk> | NemHandel, and the BIS 4 transition. |
| <https://www.oioubl.info> | OIOUBL, while it lasts. |
| <https://docs.peppol.eu/poacc/billing/3.0/> | The profile and the Danish rules inside it. |
| <https://ec.europa.eu/digital-building-blocks> (eInvoicing in Denmark) | Consolidated national requirements. |
