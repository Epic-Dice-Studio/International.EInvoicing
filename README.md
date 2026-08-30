# International.EInvoicing

**Generate, read and validate electronic invoices in .NET — for every country, without fighting the library.**

> **Status: pre-alpha.** Nothing is published on NuGet yet. The foundations (build, CI, hardened XML,
> documentation, normative artefacts) are in place; the model and the first syntax are being built.
> The table below is the honest state of things and is generated from the code's own coverage file.

UBL, UN/CEFACT CII, Factur-X / ZUGFeRD, XRechnung, Peppol BIS, French CDAR lifecycle messages — one canonical
model, one set of extension points, one package per thing you actually need.

```csharp
EInvoicing einvoicing = EInvoicing.CreateDefault();

// Read: you do not say what arrived.
if (einvoicing.Read(stream).TryGetInvoice(out EInvoice? received))
{
    Console.WriteLine(received.Number.Value);
}

// Write: from the supplier, to the customer, totals worked out from the lines.
EInvoice invoice = EInvoiceBuilder.Create(KnownProfiles.En16931Ubl)
    .WithNumber("FA-2026-001")
    .IssuedOn(new DateOnly(2026, 9, 1))
    .InCurrency("EUR")
    .From("Fournisseur SARL", "FR32100000009")
    .To("Client SA", "FR44200000008")
    .AddLine(line => line.WithItem("Conseil").WithNetAmount(1000m).WithVat("S", 20m))
    .WithComputedVatBreakdown()
    .WithComputedTotals()
    .Build();

// Check: says what ran as well as what failed, and throws only if you ask it to.
einvoicing.Validate(einvoicing.Write(invoice)).EnsureConforming();
```

Start with [getting started](docs/guides/getting-started.md), or run the sample and watch every feature go
past:

```bash
dotnet run --project samples/International.EInvoicing.Samples
```

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
| UBL 2.1 — Invoice <sub>OASIS UBL 2.1</sub> | ✅ | ✅ | ✅ | `International.EInvoicing.Ubl` |
| UBL 2.1 — Credit Note <sub>OASIS UBL 2.1</sub> | ✅ | ✅ | ✅ | `International.EInvoicing.Ubl` |
| UN/CEFACT CII <sub>D22B</sub> | ✅ | ✅ | ✅ | `International.EInvoicing.Cii` |
| UN/CEFACT CDAR <sub>generic</sub> | ✅ | ✅ | ✅ | `International.EInvoicing.Cdar` |

> **UBL 2.1 — Invoice** — Reading and writing the EN 16931 core, with everything else kept verbatim as extension data. Round-tripped against the 45 UBL invoices of the official XRechnung test suite without losing an element. Validation comes with the rule engine.

> **UBL 2.1 — Credit Note** — In UBL a credit note is not an invoice with a different code: it has its own root element and renames three things inside it. Read, written and round-tripped against the official EN 16931 credit note, and the type code (BT-3) decides which root a document built in code is written under.

> **UN/CEFACT CII** — Reading and writing the EN 16931 core, with everything else kept verbatim as extension data on the node that contained it. Round-tripped against the 41 CII invoices of the official XRechnung test suite without losing an element.

> **UN/CEFACT CDAR** — The generic message, which is what makes the fallback real: a national profiling this library does not know still parses, with its codes uninterpreted and the downgrade reported. Validation runs any Schematron rule set published for it, the French BR-FR-CDV included.

### Profiles

| | Read | Write | Validate | Package |
|---|---|---|---|---|
| EN 16931 (core invoice model) <sub>1.3.x artefacts</sub> | 📋 | 📋 | ✅ | `International.EInvoicing.Validation.En16931` |
| Factur-X / ZUGFeRD — MINIMUM → EXTENDED <sub>1.07.3 / 2.3.3</sub> | ✅ | ✅ | 📋 | `International.EInvoicing.FacturX` |
| Factur-X hybrid PDF <sub>CII payload</sub> | ✅ | ✅ | 📋 | `International.EInvoicing.FacturX.PdfSharp` |
| Peppol BIS Billing <sub>3.0</sub> | 📋 | 📋 | 🚧 | `International.EInvoicing.Peppol` |
| XRechnung (CIUS + Extension) <sub>3.x</sub> | ✅ | ✅ | ✅ | `International.EInvoicing.Countries.Germany` |

> **EN 16931 (core invoice model)** — The published Schematron artefacts are executed as data. Measured against the 23 official example documents and the 80 CIUS documents of the XRechnung test suite: all accepted.

> **Factur-X / ZUGFeRD — MINIMUM → EXTENDED** — All five profiles registered. MINIMUM and BASIC WL are read and reported as not being EN 16931 invoices (EIV4010) rather than silently accepted.

> **Factur-X hybrid PDF** — Embeds the CII payload into a PDF you already produce, and extracts it back, with the Factur-X XMP metadata. Rendering a PDF and converting one to PDF/A-3 are out of scope: those are properties of the document you start from.

> **Peppol BIS Billing** — The engine runs the published Peppol rules and agrees with every case of Peppol's own unit corpus — 227 of 227 for UBL, 127 of 127 for CII. The artefacts themselves declare no licence and are therefore not shipped: fetch them once with build/fetch-specs.sh peppol and load them like any other rule set. Reading and writing the CIUS is not done.

> **XRechnung (CIUS + Extension)** — Profiles for both syntaxes and the published rule sets, embedded. Measured against all 86 documents of the official KoSIT test suite.

### Countries

| | Read | Write | Validate | Package |
|---|---|---|---|---|
| France — invoicing (CIUS FR, Factur-X) <sub>DSE 3.x</sub> | 🚧 | 🚧 | 🚧 | `International.EInvoicing.Countries.France` |
| France — lifecycle statuses (CDAR) <sub>DSE 3.x</sub> | ✅ | ✅ | ✅ | `International.EInvoicing.Countries.France` |
| France — legal identifiers (SIREN, SIRET, VAT) | ✅ | ✅ | ✅ | `International.EInvoicing.Countries.France` |
| France — e-reporting (flux 10) <sub>PPF flux 10 v1.0</sub> | ✅ | ✅ | ✅ | `International.EInvoicing.Countries.France` |
| Germany — XRechnung, ZUGFeRD, Leitweg-ID <sub>XRechnung 3.x</sub> | ✅ | ✅ | ✅ | `International.EInvoicing.Countries.Germany` |
| Belgium — Peppol BIS, KBO/BCE, structured communication <sub>BIS 3.0</sub> | ✅ | ✅ | 📋 | `International.EInvoicing.Countries.Belgium` |
| Rest of the world | 🔬 | 🔬 | 🔬 | — |

> **France — invoicing (CIUS FR, Factur-X)** — The conformant extension urn.cpro.gouv.fr:1p0:extended-ctc-fr is registered and resolves for both syntaxes, and its rule sets load and run from the fetched artefacts. The French mandatory mentions are not yet modelled.

> **France — lifecycle statuses (CDAR)** — Every status, to a trading partner and to the public portal, measured on each build against the DGFiP's own BR-FR-CDV rules and their eleven sample messages. The artefacts are fetched, not shipped: run build/fetch-specs.sh france.

> **France — legal identifiers (SIREN, SIRET, VAT)** — SIREN, SIRET and VAT numbers with their check digits, including the La Poste establishments that satisfy a different rule from Luhn.

> **France — e-reporting (flux 10)** — The transactions and payments transmissions, built, written and read back. The DGFiP publishes no sample transmissions, so what is measured is that every document this library builds satisfies the published flux 10 rules.

> **Germany — XRechnung, ZUGFeRD, Leitweg-ID** — XRechnung profiles for both syntaxes, the Leitweg-ID with its check digit, and the published rule sets running against the official test suite.

> **Belgium — Peppol BIS, KBO/BCE, structured communication** — The Peppol BIS profiles the mandate is built on, the KBO/BCE enterprise number and the structured communication. Peppol validation artefacts are not redistributable; fetch them locally.

> **Rest of the world** — See the roadmap below.

### Transport

| | Read | Write | Validate | Package |
|---|---|---|---|---|
| Peppol AS4, French PA APIs, Chorus Pro, Mercurius | ⛔ | ⛔ | ⛔ | — |
| PDF rendering and PDF/A conversion | ⛔ | ⛔ | ⛔ | — |

> **Peppol AS4, French PA APIs, Chorus Pro, Mercurius** — This library produces and reads documents. Sending them is your access point's job.

> **PDF rendering and PDF/A conversion** — A hybrid invoice starts from the PDF you produce for humans. Building that PDF, and making it PDF/A-conforming, belongs to your reporting or PDF library.

**Legend** — ✅ Implemented · 🚧 In progress · 📋 Planned · 🔬 Researching · ⛔ Out of scope

<!-- coverage:end -->

**[The roadmap](docs/roadmap.md)** says what comes next and why. The short version: UBL credit notes and a
Peppol package first — one is a document real integrations receive and this library mishandles, the other is
ten countries for the cost of one. Then the European mandates with formats of their own (Romania, Hungary,
Greece, Italy, Spain, Portugal, Poland), then the rest of the world.

---

## Scope

**In scope** — the document itself: building it, reading it, validating it against the norms, and the
lifecycle and reporting messages that accompany it in each country.

**Out of scope, permanently** — sending it. No Peppol AS4 client, no access point APIs, no Chorus Pro
connector. This library performs no network I/O at all, which is also what makes it safe to run inside a
browser via WebAssembly and easy to audit.

---

## Try it without installing anything

The [playground](https://epic-dice-studio.github.io/International.EInvoicing/demo/) reads, checks and builds
documents entirely in your browser — the library is compiled to WebAssembly, so **no invoice you open there
reaches a server**.

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
