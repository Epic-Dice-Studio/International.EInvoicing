# Germany

> Recorded state: August 2026. Verify against the current BMF guidance before relying on dates.

## The mandate

Since **1 January 2025**, every German business must be able to **receive** structured electronic invoices for
domestic B2B. **Issuing** becomes mandatory on **1 January 2027** for businesses above €800,000 annual
turnover, and on **1 January 2028** for everyone else. During the transition, paper and unstructured PDF
remain acceptable only with the recipient's agreement.

An "electronic invoice" is defined as a structured, machine-processable format complying with EN 16931 — a PDF
image is explicitly not one.

## Accepted formats

- **XRechnung** (UBL or CII) — see [xrechnung.md](xrechnung.md)
- **ZUGFeRD 2.1+** hybrid PDF/A-3, profiles EN 16931 and above — see [facturx.md](facturx.md)
- **Peppol BIS Billing 3.0** — see [peppol-bis-3.md](peppol-bis-3.md)
- EDI formats, where both parties agree and EN 16931 data can be derived

ZUGFeRD MINIMUM and BASIC WL do **not** satisfy the mandate; they are accounting aids, not invoices.

## Scope for this library

| Capability | Package | Status |
|---|---|---|
| XRechnung CIUS and Extension (UBL + CII) | `.Countries.Germany` | planned |
| ZUGFeRD profiles | `.FacturX` + `.Countries.Germany` | planned |
| Leitweg-ID | `.Countries.Germany` | planned |
| Transmission (Peppol, ZRE/OZG-RE portals) | — | permanently out of scope |

## Official sources

| Source | Use it for |
|---|---|
| <https://xeinkauf.de/xrechnung/> | XRechnung specification. |
| <https://github.com/itplr-kosit> | Schematron, test suite, reference validator. |
| <https://www.ferd-net.de> | ZUGFeRD specification and samples. |
| <https://ec.europa.eu/digital-building-blocks> | Country page, public-sector routing. |

## Pitfalls

- **Public sector routing needs the Leitweg-ID** (`BT-10`), with a check digit. B2B usually does not.
- **Both syntaxes circulate.** Supporting only UBL or only CII means rejecting valid German invoices.
- **The KoSIT validator is the arbiter.** When our engine and KoSIT disagree, we are wrong until proven
  otherwise — that is why CI runs it.
