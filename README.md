# International.EInvoicing

**Generate, read and validate electronic invoices in .NET — for every country, without fighting the library.**

> **Status: pre-alpha.** Nothing is published on NuGet yet. The foundations (build, CI, hardened XML,
> documentation, normative artefacts) are in place; the model and the first syntax are being built.
> The table below is the honest state of things and is generated from the code's own coverage file.

UBL, UN/CEFACT CII, Factur-X / ZUGFeRD, XRechnung, Peppol BIS, French CDAR lifecycle messages — one canonical
model, one set of extension points, one package per thing you actually need.

---

## Why another e-invoicing library

Three promises drive every design decision here.

**1. Extensible without forking.** A profile, a field or a rule you need but we haven't shipped is something
you add from *your* code — register your own reader, writer, profile or rule set and it wins over ours. No
pull request, no waiting for a release.

**2. Nothing is lost, nothing explodes.** Every field keeps the raw text and the XML attributes it had in the
source file, next to the typed value:

```csharp
invoice.IssueDate.Value        // DateOnly? → 2026-08-29
invoice.IssueDate.Raw          // "20260829"
invoice.IssueDate.FormatCode   // "102"  (UNTDID 2379)
```

And readers never throw on a document you received. An unknown profile, an illegal date, an element nobody
mapped — each becomes a diagnostic with a documented fallback, and the document still parses:

```
EIV1042  Warning  UnknownProfile
    expected  urn:cen.eu:en16931:2017#compliant#urn:factur-x.eu:1p0:basic
    found     urn:acme:profile:2p0
    fallback  parsed as generic EN 16931 CII
```

**3. Honest about what it does not do.** A profile we do not support is reported in the parse diagnostics,
marks `ValidationReport.IsComplete` as false, and shows up as unsupported in the table below. A partial
validation is never presented as a success.

---

## Support matrix

<!-- coverage:start -->
### Syntaxes

| | Read | Write | Validate | Package |
|---|---|---|---|---|
| UBL 2.1 — Invoice <sub>OASIS UBL 2.1</sub> | 📋 | 📋 | 📋 | `International.EInvoicing.Ubl` |
| UBL 2.1 — Credit Note <sub>OASIS UBL 2.1</sub> | 📋 | 📋 | 📋 | `International.EInvoicing.Ubl` |
| UN/CEFACT CII <sub>D22B</sub> | 📋 | 📋 | 📋 | `International.EInvoicing.Cii` |
| UN/CEFACT CDAR <sub>generic</sub> | 📋 | 📋 | 📋 | `International.EInvoicing.Cdar` |

### Profiles

| | Read | Write | Validate | Package |
|---|---|---|---|---|
| EN 16931 (core invoice model) <sub>1.3.x artefacts</sub> | 📋 | 📋 | 📋 | `International.EInvoicing.Validation.En16931` |
| Factur-X / ZUGFeRD — MINIMUM → EXTENDED <sub>1.07.3 / 2.3.3</sub> | 📋 | 📋 | 📋 | `International.EInvoicing.FacturX` |
| Factur-X hybrid PDF/A-3 <sub>ISO 19005-3</sub> | 📋 | 📋 | 📋 | `International.EInvoicing.FacturX.PdfSharp` |
| Peppol BIS Billing <sub>3.0</sub> | 📋 | 📋 | 📋 | `International.EInvoicing.Peppol` |
| XRechnung (CIUS + Extension) <sub>3.x</sub> | 📋 | 📋 | 📋 | `International.EInvoicing.Countries.Germany` |

### Countries

| | Read | Write | Validate | Package |
|---|---|---|---|---|
| France — invoicing (CIUS FR, Factur-X) <sub>DSE 3.x</sub> | 📋 | 📋 | 📋 | `International.EInvoicing.Countries.France` |
| France — lifecycle statuses (CDAR) <sub>DSE 3.x</sub> | 📋 | 📋 | 📋 | `International.EInvoicing.Countries.France` |
| France — legal identifiers (SIREN, SIRET, VAT) | 📋 | 📋 | 📋 | `International.EInvoicing.Countries.France` |
| France — e-reporting <sub>DSE 3.x</sub> | 🔬 | 🔬 | 🔬 | — |
| Germany — XRechnung, ZUGFeRD, Leitweg-ID <sub>XRechnung 3.x</sub> | 📋 | 📋 | 📋 | `International.EInvoicing.Countries.Germany` |
| Belgium — Peppol BIS, KBO/BCE, structured communication <sub>BIS 3.0</sub> | 📋 | 📋 | 📋 | `International.EInvoicing.Countries.Belgium` |
| Rest of the world | 🔬 | 🔬 | 🔬 | — |

> **France — e-reporting** — Deferred past 1.0; specification still moving.

> **Rest of the world** — See the roadmap below.

### Transport

| | Read | Write | Validate | Package |
|---|---|---|---|---|
| Peppol AS4, French PA APIs, Chorus Pro, Mercurius | ⛔ | ⛔ | ⛔ | — |

> **Peppol AS4, French PA APIs, Chorus Pro, Mercurius** — This library produces and reads documents. Sending them is your access point's job.

**Legend** — ✅ Implemented · 🚧 In progress · 📋 Planned · 🔬 Researching · ⛔ Out of scope

<!-- coverage:end -->

Roadmap after 1.0: French e-reporting, then Italy (FatturaPA/SDI), Spain (Facturae, VeriFactu, TicketBAI),
Portugal (SAF-T, ATCUD), Poland (KSeF), Romania (RO e-Factura), Hungary (RTIR), Greece (myDATA), the
Peppol-based Nordics and Netherlands, Switzerland, the United Kingdom — then Brazil (NF-e), Mexico (CFDI),
India (IRP), Saudi Arabia (ZATCA), Türkiye, Singapore (InvoiceNow), Australia/New Zealand, Japan, Malaysia
(MyInvois), Vietnam.

---

## Scope

**In scope** — the document itself: building it, reading it, validating it against the norms, and the
lifecycle and reporting messages that accompany it in each country.

**Out of scope, permanently** — sending it. No Peppol AS4 client, no access point APIs, no Chorus Pro
connector. This library performs no network I/O at all, which is also what makes it safe to run inside a
browser via WebAssembly and easy to audit.

---

## Documentation

| | |
|---|---|
| Business guides — extend a format, hook into generation, read a raw value | `docs/guides/` |
| One page per standard: mappings, artefacts, pitfalls | `docs/standards/` |
| Diagnostic catalogue — one page per `EIV` code | `docs/diagnostics/` |
| Recipes — add a format, a country, a profile, a rule | `docs/recipes/` |
| Architecture decisions and their rationale | `docs/adr/` |
| Working agreement for contributors and coding agents | [AGENTS.md](AGENTS.md) |

## Contributing

Read [CONTRIBUTING.md](CONTRIBUTING.md) and [AGENTS.md](AGENTS.md). The short version:

```bash
dotnet build -c Release   # warning-free, warnings are errors
dotnet test  -c Release
```

Adding support for a standard means five test families — parse, round-trip, conformance, rules, diagnostics —
against the official sample files. `AGENTS.md` §4 spells out the definition of done.

## Licence

[MIT](LICENSE). Normative artefacts redistributed under `specs/` keep their own licences; see
[NOTICE](NOTICE) and the `PROVENANCE.md` in each folder.
