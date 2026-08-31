# International.EInvoicing

[![NuGet](https://img.shields.io/nuget/v/International.EInvoicing?logo=nuget&label=NuGet)](https://www.nuget.org/packages/International.EInvoicing)
[![Downloads](https://img.shields.io/nuget/dt/International.EInvoicing?logo=nuget&label=downloads)](https://www.nuget.org/packages/International.EInvoicing)
[![CI](https://github.com/Epic-Dice-Studio/International.EInvoicing/actions/workflows/ci.yml/badge.svg)](https://github.com/Epic-Dice-Studio/International.EInvoicing/actions/workflows/ci.yml)
[![Docs](https://github.com/Epic-Dice-Studio/International.EInvoicing/actions/workflows/docs-check.yml/badge.svg)](https://github.com/Epic-Dice-Studio/International.EInvoicing/actions/workflows/docs-check.yml)
[![CodeQL](https://github.com/Epic-Dice-Studio/International.EInvoicing/actions/workflows/codeql.yml/badge.svg)](https://github.com/Epic-Dice-Studio/International.EInvoicing/actions/workflows/codeql.yml)
[![.NET](https://img.shields.io/badge/.NET-8.0%20%7C%2010.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com)
[![License](https://img.shields.io/github/license/Epic-Dice-Studio/International.EInvoicing?label=licence)](LICENSE)
[![Playground](https://img.shields.io/badge/playground-try%20it%20in%20your%20browser-2ea44f)](https://epic-dice-studio.github.io/International.EInvoicing/demo/)

**Generate, read and validate electronic invoices in .NET — for every country, without fighting the library.**

> **Status: pre-alpha.** Only prereleases are published — every push to `main` publishes
> `0.1.0-preview.1.N` to NuGet, so `dotnet add package International.EInvoicing --prerelease` gets the
> latest. No stable version has been cut. The foundations (build, CI, hardened XML,
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

Invoicing in one country only? There is a shorter way in — one type that already knows what that country
expects:

```csharp
FrenchEInvoicing france = FrenchEInvoicing.Create();

// France exchanges four documents on one channel. This tells you which one arrived.
FrenchDocument document = france.ReadFile(path);        // Invoice, CreditNote, LifecycleStatus, EReport

GermanEInvoicing germany = GermanEInvoicing.Create();
germany.InvoiceToPublicBody("04011000-1234512345-06");  // XRechnung, Leitweg-ID checked before it is written

BelgianEInvoicing belgium = BelgianEInvoicing.Create();
belgium.Invoice().From(seller => belgium.Describe(seller, "0776.914.174", "Epic Dice Studio BV"));
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
| Peppol BIS Billing <sub>3.0</sub> | ✅ | ✅ | ✅ | `International.EInvoicing.Peppol` |
| XRechnung (CIUS + Extension) <sub>3.x</sub> | ✅ | ✅ | ✅ | `International.EInvoicing.Countries.Germany` |

> **EN 16931 (core invoice model)** — The published Schematron artefacts are executed as data. Measured against the 23 official example documents and the 80 CIUS documents of the XRechnung test suite: all accepted.

> **Factur-X / ZUGFeRD — MINIMUM → EXTENDED** — All five profiles registered. MINIMUM and BASIC WL are read and reported as not being EN 16931 invoices (EIV4010) rather than silently accepted.

> **Factur-X hybrid PDF** — Embeds the CII payload into a PDF you already produce, and extracts it back, with the Factur-X XMP metadata. Rendering a PDF and converting one to PDF/A-3 are out of scope: those are properties of the document you start from.

> **Peppol BIS Billing** — The profiles for both syntaxes, the EN 16931 electronic address scheme list taken from the artefacts this library ships, and participant identifiers. The Peppol rules declare no licence and are therefore fetched, not packaged — one call loads all four once they are: AddPeppolRulesFrom(directory). The engine agrees with every case of Peppol's own unit corpus, 227 of 227 for UBL and 127 of 127 for CII.

> **XRechnung (CIUS + Extension)** — Profiles for both syntaxes and the published rule sets, embedded. Measured against all 86 documents of the official KoSIT test suite.

### Countries

| | Read | Write | Validate | Package |
|---|---|---|---|---|
| France — invoicing (CIUS FR, Factur-X) <sub>DSE 3.x</sub> | ✅ | ✅ | ✅ | `International.EInvoicing.Countries.France` |
| France — lifecycle statuses (CDAR) <sub>DSE 3.x</sub> | ✅ | ✅ | ✅ | `International.EInvoicing.Countries.France` |
| France — legal identifiers (SIREN, SIRET, VAT) | ✅ | ✅ | ✅ | `International.EInvoicing.Countries.France` |
| France — e-reporting (flux 10) <sub>PPF flux 10 v1.0</sub> | ✅ | ✅ | ✅ | `International.EInvoicing.Countries.France` |
| Germany — XRechnung, ZUGFeRD, Leitweg-ID <sub>XRechnung 3.x</sub> | ✅ | ✅ | ✅ | `International.EInvoicing.Countries.Germany` |
| Belgium — Peppol BIS, KBO/BCE, structured communication <sub>BIS 3.0</sub> | ✅ | ✅ | ✅ | `International.EInvoicing.Countries.Belgium` |
| Norway — EHF 3.0, organisasjonsnummer <sub>EHF 3.0</sub> | ✅ | ✅ | ✅ | `International.EInvoicing.Countries.Norway` |
| Sweden — Peppol BIS, organisationsnummer <sub>BIS 3.0</sub> | ✅ | ✅ | ✅ | `International.EInvoicing.Countries.Sweden` |
| Denmark — Peppol BIS, CVR, allowed payment means <sub>BIS 3.0</sub> | ✅ | ✅ | ✅ | `International.EInvoicing.Countries.Denmark` |
| Netherlands — Peppol BIS, KvK and OIN <sub>BIS 3.0</sub> | ✅ | ✅ | ✅ | `International.EInvoicing.Countries.Netherlands` |
| Iceland — Peppol BIS, kennitala <sub>BIS 3.0</sub> | ✅ | ✅ | ✅ | `International.EInvoicing.Countries.Iceland` |
| Croatia — Peppol BIS, OIB <sub>BIS 3.0</sub> | ✅ | ✅ | 🚧 | `International.EInvoicing.Countries.Croatia` |
| Australia — Peppol PINT (A-NZ), ABN <sub>PINT @aunz-1</sub> | ✅ | ✅ | ✅ | `International.EInvoicing.Countries.Australia` |
| New Zealand — Peppol PINT (A-NZ), NZBN <sub>PINT @aunz-1</sub> | ✅ | ✅ | ✅ | `International.EInvoicing.Countries.NewZealand` |
| Singapore — Peppol PINT (InvoiceNow), GST <sub>PINT @sg-1</sub> | ✅ | ✅ | ✅ | `International.EInvoicing.Countries.Singapore` |
| Malaysia — Peppol PINT (MyInvois), BRN and TIN <sub>PINT @my-1</sub> | ✅ | ✅ | ✅ | `International.EInvoicing.Countries.Malaysia` |
| Japan — Peppol PINT, qualified invoice <sub>PINT @jp-1</sub> | ✅ | ✅ | ✅ | `International.EInvoicing.Countries.Japan` |
| Rest of the world | 🔬 | 🔬 | 🔬 | — |

> **France — invoicing (CIUS FR, Factur-X)** — The conformant extension urn.cpro.gouv.fr:1p0:extended-ctc-fr, the invoicing case (BT-23) and the three mentions French law requires, with the SIREN of both parties checked before it is written. An invoice built with ForFrance() satisfies EN 16931, BR-FR-Flux2 and EXTENDED-CTC-FR in both syntaxes, measured on every build.

> **France — lifecycle statuses (CDAR)** — Every status, to a trading partner and to the public portal, measured on each build against the DGFiP's own BR-FR-CDV rules and their eleven sample messages. The artefacts are fetched, not shipped: run build/fetch-specs.sh france.

> **France — legal identifiers (SIREN, SIRET, VAT)** — SIREN, SIRET and VAT numbers with their check digits, including the La Poste establishments that satisfy a different rule from Luhn.

> **France — e-reporting (flux 10)** — The transactions and payments transmissions, built, written and read back. The DGFiP publishes no sample transmissions, so what is measured is that every document this library builds satisfies the published flux 10 rules.

> **Germany — XRechnung, ZUGFeRD, Leitweg-ID** — XRechnung profiles for both syntaxes, the Leitweg-ID with its check digit, and the published rule sets running against the official test suite.

> **Belgium — Peppol BIS, KBO/BCE, structured communication** — Built on International.EInvoicing.Peppol, which the 2026 mandate is: the KBO/BCE enterprise number with its modulo 97 check, the structured communication, and Peppol validation once the artefacts are fetched.

> **Norway — EHF 3.0, organisasjonsnummer** — EHF 3.0, the national CIUS of Peppol BIS, and the organisation number whose modulo 11 check is measured against the rule Peppol publishes for scheme 0192. The Norwegian rules travel inside the Peppol rule set.

> **Sweden — Peppol BIS, organisationsnummer** — Peppol BIS Billing, and the organisation number whose Luhn check is measured against the rule Peppol publishes for scheme 0007. The Swedish rules travel inside the Peppol rule set.

> **Denmark — Peppol BIS, CVR, allowed payment means** — Peppol BIS Billing over NemHandel, the CVR number in the schemes Peppol reserves for it, and the payment means codes DK-R-005 allows — code 30 is valid EN 16931 and refused in Denmark. OIOUBL and NemHandel BIS 4 are not carried; see the roadmap.

> **Netherlands — Peppol BIS, KvK and OIN** — Peppol BIS Billing, and the legal entity identifier NL-R-003 and NL-R-005 reject an invoice for omitting — scheme 0106 or 0190 on both parties. NLCIUS is not carried: its published identifier is not in any artefact this repository holds.

> **Iceland — Peppol BIS, kennitala** — Peppol BIS Billing, and the kennitala with its modulo 11 check, written where IS-R-002 and IS-R-004 look for it.

> **Croatia — Peppol BIS, OIB** — The OIB both parties must carry under the Fiskalizacija 2.0 mandate, checked against ISO/IEC 7064 MOD 11,10. The HR-FISK 2.0 CIUS is not carried — its identifier is published nowhere this repository can read — and neither is the advanced electronic seal or the fiscalisation reporting, which are a signature and a transport.

> **Australia — Peppol PINT (A-NZ), ABN** — The A-NZ PINT profile and business process — both different strings from Peppol BIS — the ABN with the modulo 89 check measured against Peppol's rule for scheme 0151, and GST rather than VAT, which four fatal A-NZ rules require. Validated against the PINT base and jurisdiction rules once they are fetched.

> **New Zealand — Peppol PINT (A-NZ), NZBN** — The same A-NZ PINT specialisation Australia uses, the NZBN — a GS1 location number, routed under scheme 0088 — measured against Peppol's GLN rule, and GST. Validated against the PINT base and jurisdiction rules once they are fetched.

> **Singapore — Peppol PINT (InvoiceNow), GST** — The SG PINT profile and process, SGD and GST, and the Singaporean tax category codes read out of BR-CL-17-GST-SG — 'S' is rejected there. Validated against Singapore's own base and jurisdiction rules once they are fetched. No identifier scheme is asserted: Singapore's rules name none.

> **Malaysia — Peppol PINT (MyInvois), BRN and TIN** — The MY PINT profile and process, the BRN of both parties and the supplier's TIN that three fatal rules require, and Malaysia's own tax category codes read out of aligned-ibrp-cl-01-my — 'S' is not among them. Validated against Malaysia's own base and jurisdiction rules once they are fetched.

> **Japan — Peppol PINT, qualified invoice** — The JP PINT profile and process, JPY, and the invoice period aligned-ibrp-052 requires where EN 16931 leaves it optional. Validated against Japan's own base and jurisdiction rules once they are fetched.

> **Rest of the world** — Some fifty countries, catalogued in the roadmap below by what each would cost to add: a rule set and an identifier for the Peppol ones, a reader and a writer for the national formats, a different document entirely for the clearance countries.

### Transport

| | Read | Write | Validate | Package |
|---|---|---|---|---|
| Peppol AS4, French PA APIs, Chorus Pro, Mercurius | ⛔ | ⛔ | ⛔ | — |
| PDF rendering and PDF/A conversion | ⛔ | ⛔ | ⛔ | — |

> **Peppol AS4, French PA APIs, Chorus Pro, Mercurius** — This library produces and reads documents. Sending them is your access point's job.

> **PDF rendering and PDF/A conversion** — A hybrid invoice starts from the PDF you produce for humans. Building that PDF, and making it PDF/A-conforming, belongs to your reporting or PDF library.

**Legend** — ✅ Implemented · 🚧 In progress · 📋 Planned · 🔬 Researching · ⛔ Out of scope

<!-- coverage:end -->

**[The roadmap](docs/roadmap.md)** says what comes next and why, and carries the full country catalogue —
some fifty of them, grouped by what each would cost to add. The short version: two multipliers first, because
both change what every country below costs. **EN 16931-1:2026** was published in May 2026 and the 2017 edition
this library is built on was formally withdrawn; the revision is not backward compatible. And **Peppol PINT**
is the specification everywhere Peppol was adopted outside Europe — the UAE, Malaysia, Singapore, Japan,
Australia and New Zealand, probably the United Kingdom — which our Peppol package does not yet speak.
After those: Croatia and Slovakia, whose B2B mandates are live or dated and cost a rule set each; Romania and
Italy; a shared reporting model for Hungary and Greece built on the French flux 10 shape; then Spain and
Poland; then, once the model question in the roadmap is answered, the clearance countries outside Europe.

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
