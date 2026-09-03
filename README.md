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
| UBL 2.1 — Application Response <sub>OASIS UBL 2.1</sub> | ✅ | ✅ | ✅ | `International.EInvoicing.Ubl` |
| UBL 2.1 — Despatch Advice <sub>OASIS UBL 2.1</sub> | ✅ | ✅ | ✅ | `International.EInvoicing.Ubl` |
| UBL 2.1 — Order <sub>OASIS UBL 2.1</sub> | ✅ | ✅ | ✅ | `International.EInvoicing.Ubl` |
| UBL 2.1 — Order Response <sub>OASIS UBL 2.1</sub> | ✅ | ✅ | ✅ | `International.EInvoicing.Ubl` |
| UN/CEFACT CDAR <sub>generic</sub> | ✅ | ✅ | ✅ | `International.EInvoicing.Cdar` |

> **UBL 2.1 — Invoice** — Reading and writing the EN 16931 core, with everything else kept verbatim as extension data. Round-tripped against the 45 UBL invoices of the official XRechnung test suite without losing an element. Validation comes with the rule engine.

> **UBL 2.1 — Credit Note** — In UBL a credit note is not an invoice with a different code: it has its own root element and renames three things inside it. Read, written and round-tripped against the official EN 16931 credit note, and the type code (BT-3) decides which root a document built in code is written under.

> **UN/CEFACT CII** — Reading and writing the EN 16931 core, with everything else kept verbatim as extension data on the node that contained it. Round-tripped against the 41 CII invoices of the official XRechnung test suite without losing an element.

> **UBL 2.1 — Application Response** — What happened to a document rather than what is owed for it: the Peppol Invoice Response and Message Level Response. Fills the same lifecycle model the French CDAR messages do, so one model serves both syntaxes. Read, round-tripped and schema-checked against OpenPEPPOL's own thirteen use cases and two published examples.

> **UBL 2.1 — Despatch Advice** — What actually left the warehouse, which is what an invoice is reconciled against: delivered and outstanding quantities, serial numbers and lots, pallets and packages, and how the consignment travels. Read, round-tripped, schema-checked and judged by Peppol's own rules against all six documents OpenPEPPOL publishes.

> **UBL 2.1 — Order** — What the buyer asked for, and the document the despatch advice and the invoice are answered against: lines with quantities and prices, delivery terms and windows, whether a short delivery is acceptable. Read, round-tripped, schema-checked and judged by Peppol's own rules against all seven documents OpenPEPPOL publishes, with nothing left unmapped.

> **UBL 2.1 — Order Response** — The seller's answer to an order: accepted, rejected, or accepted on other terms — a different quantity, a later date, or a substitute product. Read, round-tripped, schema-checked and judged by Peppol's own rules against all six documents OpenPEPPOL publishes. The advanced response and the order agreement are the same document under other profiles and need no reader of their own; the agreement restates the whole order, certificates and specification documents included.

> **UN/CEFACT CDAR** — The generic message, which is what makes the fallback real: a national profiling this library does not know still parses, with its codes uninterpreted and the downgrade reported. Validation runs any Schematron rule set published for it, the French BR-FR-CDV included.

### Profiles

| | Read | Write | Validate | Package |
|---|---|---|---|---|
| XSD schema validation (UBL 2.1, CII D22B) <sub>UBL 2.1 · CII D22B</sub> | ✅ | ✅ | ✅ | `International.EInvoicing.Validation.Xsd` |
| EN 16931 (core invoice model) <sub>1.3.x artefacts</sub> | 📋 | 📋 | ✅ | `International.EInvoicing.Validation.En16931` |
| Factur-X / ZUGFeRD — MINIMUM → EXTENDED <sub>1.07.3 / 2.3.3</sub> | ✅ | ✅ | ✅ | `International.EInvoicing.FacturX` |
| Factur-X hybrid PDF <sub>CII payload</sub> | ✅ | ✅ | ⛔ | `International.EInvoicing.FacturX.PdfSharp` |
| Peppol BIS Billing <sub>3.0</sub> | ✅ | ✅ | ✅ | `International.EInvoicing.Peppol` |
| XRechnung (CIUS + Extension) <sub>3.x</sub> | ✅ | ✅ | ✅ | `International.EInvoicing.Countries.Germany` |
| Peppol tax data document (SK, ViDA) <sub>taxdata sk-1, vida-1</sub> | ✅ | ✅ | ✅ | `International.EInvoicing.Peppol` |
| Peppol post-award — Order, Response, Cancellation, Despatch Advice, Invoice Response, MLR <sub>Order 3, Order Response 3 (simple, advanced, agreement), Order Cancellation 3, Despatch Advice 3, Invoice Response 3.1, MLR</sub> | ✅ | ✅ | ✅ | `International.EInvoicing.Peppol` |

> **XSD schema validation (UBL 2.1, CII D22B)** — The OASIS and UN/CEFACT schemas, embedded and offline, as rule sets like any other. They judge what no business rule looks at — element order and cardinality are normative in both syntaxes — and they earned their keep twice: they caught the shape this library shipped (two bank accounts in one cac:PaymentMeans) and then, on the official corpora, fifteen EN 16931 terms that were read by nothing and written by nothing. Both corpora now round-trip with their shape intact and nothing unmapped.

> **EN 16931 (core invoice model)** — The published Schematron artefacts are executed as data. Measured against the 23 official example documents and the 80 CIUS documents of the XRechnung test suite: all accepted.

> **Factur-X / ZUGFeRD — MINIMUM → EXTENDED** — All five profiles, and their rules now run once fetched — including MINIMUM and BASIC WL, which say in their own specification that they are not EN 16931 invoices, and which nothing judged before. Running them found that this library was writing @currencyID on CII amounts that forbid it.

> **Factur-X hybrid PDF** — Embeds the CII payload into a PDF you already produce, and extracts it back, with the Factur-X XMP metadata. Rendering a PDF and converting one to PDF/A-3 are out of scope: those are properties of the document you start from.

> **Peppol BIS Billing** — The profiles for both syntaxes, the EN 16931 electronic address scheme list taken from the artefacts this library ships, and participant identifiers. The Peppol rules declare no licence and are therefore fetched, not packaged — one call loads all four once they are: AddPeppolRulesFrom(directory). The engine agrees with every case of Peppol's own unit corpus, 227 of 227 for UBL and 127 of 127 for CII.

> **XRechnung (CIUS + Extension)** — Profiles for both syntaxes and the published rule sets, embedded. Measured against all 86 documents of the official KoSIT test suite.

> **Peppol tax data document (SK, ViDA)** — The document a reporting mandate sends to the tax authority beside the invoice, as OpenPeppol specifies it per jurisdiction. Slovakia's rule set and the EU's ViDA one differ by one assertion out of 88, by a namespace and by an identifier, so one writer serves both and both are measured. The Gulf ones are a second dialect — the Emirati and Omani documents require a source document, a reporter's representative and content of their own — and are not carried yet. Reading one back is a receiver's job. Read as well as written: the reported document is handed to the UBL invoice reader after three element renames, so a business term the invoice reader maps is one a tax authority gets back. Reading it back showed what the projection actually omits — the supplier has no name in a tax data document, only a VAT identifier.

> **Peppol post-award — Order, Response, Cancellation, Despatch Advice, Invoice Response, MLR** — The chain an invoice sits at the end of: an Order says what was asked for, a Despatch Advice what was sent, an Invoice Response what happened to the invoice, and a Message Level Response whether the message arrived at all. Peppol's own rules run from the compiled artefacts, each scoped to the transaction it governs; the rules and the corpus are fetched rather than shipped, since OpenPEPPOL declares no licence. Order Change is not here.

### Countries

| | Read | Write | Validate | Package |
|---|---|---|---|---|
| France — invoicing (CIUS FR, Factur-X) <sub>DSE 3.x</sub> | ✅ | ✅ | ✅ | `International.EInvoicing.Countries.France` |
| France — lifecycle statuses (CDAR) <sub>DSE 3.x</sub> | ✅ | ✅ | ✅ | `International.EInvoicing.Countries.France` |
| France — legal identifiers (SIREN, SIRET, VAT) | ✅ | ✅ | ✅ | `International.EInvoicing.Countries.France` |
| France — e-reporting (flux 10) <sub>PPF flux 10 v1.0</sub> | ✅ | ✅ | ✅ | `International.EInvoicing.Countries.France` |
| Germany — XRechnung, ZUGFeRD, Leitweg-ID, Skonto <sub>XRechnung 3.x</sub> | ✅ | ✅ | ✅ | `International.EInvoicing.Countries.Germany` |
| Greece — Peppol BIS, AFM, six-segment invoice number <sub>BIS 3.0</sub> | ✅ | ✅ | ✅ | `International.EInvoicing.Countries.Greece` |
| Belgium — Peppol BIS, KBO/BCE, structured communication <sub>BIS 3.0</sub> | ✅ | ✅ | ✅ | `International.EInvoicing.Countries.Belgium` |
| Norway — EHF 3.0, organisasjonsnummer <sub>EHF 3.0</sub> | ✅ | ✅ | ✅ | `International.EInvoicing.Countries.Norway` |
| Sweden — Peppol BIS, organisationsnummer <sub>BIS 3.0</sub> | ✅ | ✅ | ✅ | `International.EInvoicing.Countries.Sweden` |
| Denmark — Peppol BIS, CVR, allowed payment means <sub>BIS 3.0</sub> | ✅ | ✅ | ✅ | `International.EInvoicing.Countries.Denmark` |
| Netherlands — NLCIUS and Peppol BIS, KvK and OIN <sub>NLCIUS v1.0</sub> | ✅ | ✅ | ✅ | `International.EInvoicing.Countries.Netherlands` |
| Romania — CIUS-RO (e-Factura) <sub>CIUS-RO 1.0.1</sub> | ✅ | ✅ | ✅ | `International.EInvoicing.Countries.Romania` |
| Serbia — SRBDT (SEF) <sub>srbdt 2022</sub> | ✅ | ✅ | ✅ | `International.EInvoicing.Countries.Serbia` |
| Portugal — CIUS-PT <sub>CIUS-PT 2.1.1</sub> | ✅ | ✅ | ✅ | `International.EInvoicing.Countries.Portugal` |
| Iceland — Peppol BIS, kennitala <sub>BIS 3.0</sub> | ✅ | ✅ | ✅ | `International.EInvoicing.Countries.Iceland` |
| Italy — Peppol BIS, partita IVA <sub>BIS 3.0</sub> | ✅ | ✅ | ✅ | `International.EInvoicing.Countries.Italy` |
| Slovakia — Peppol BIS, tax data document <sub>TDD sk-1 1.0.0</sub> | 🚧 | ✅ | ✅ | `International.EInvoicing.Countries.Slovakia` |
| Croatia — Peppol BIS, CIUS-HR, OIB <sub>CIUS-HR 2025</sub> | ✅ | ✅ | ✅ | `International.EInvoicing.Countries.Croatia` |
| Australia — Peppol PINT (A-NZ), ABN <sub>PINT @aunz-1</sub> | ✅ | ✅ | ✅ | `International.EInvoicing.Countries.Australia` |
| New Zealand — Peppol PINT (A-NZ), NZBN <sub>PINT @aunz-1</sub> | ✅ | ✅ | ✅ | `International.EInvoicing.Countries.NewZealand` |
| Singapore — Peppol PINT (InvoiceNow), GST <sub>PINT @sg-1</sub> | ✅ | ✅ | ✅ | `International.EInvoicing.Countries.Singapore` |
| Malaysia — Peppol PINT (MyInvois), BRN and TIN <sub>PINT @my-1</sub> | ✅ | ✅ | ✅ | `International.EInvoicing.Countries.Malaysia` |
| Japan — Peppol PINT, qualified invoice <sub>PINT @jp-1</sub> | ✅ | ✅ | ✅ | `International.EInvoicing.Countries.Japan` |
| Rest of the world | 🔬 | 🔬 | 🔬 | — |

> **France — invoicing (CIUS FR, Factur-X)** — The conformant extension urn.cpro.gouv.fr:1p0:extended-ctc-fr, the invoicing case (BT-23) and the three mentions French law requires, with the SIREN of both parties checked before it is written. An invoice built with ForFrance() satisfies EN 16931, BR-FR-Flux2 and EXTENDED-CTC-FR in both syntaxes, measured on every build. The rules for all five profiles now run once fetched — including MINIMUM and BASIC WL, which are not EN 16931 invoices and which nothing judged before.

> **France — lifecycle statuses (CDAR)** — Every status, to a trading partner and to the public portal, measured on each build against the DGFiP's own BR-FR-CDV rules and their eleven sample messages. The artefacts are fetched, not shipped: run build/fetch-specs.sh france.

> **France — legal identifiers (SIREN, SIRET, VAT)** — SIREN, SIRET and VAT numbers with their check digits, including the La Poste establishments that satisfy a different rule from Luhn.

> **France — e-reporting (flux 10)** — The transactions and payments transmissions, built, written and read back. The DGFiP publishes no sample transmissions, so what is measured is that every document this library builds satisfies the published flux 10 rules.

> **Germany — XRechnung, ZUGFeRD, Leitweg-ID, Skonto** — XRechnung profiles for both syntaxes, the Leitweg-ID with its check digit, and the published rule sets running against the official test suite. Early-payment discounts, which Germany encodes inside BT-20's free text where BR-DE-18 judges them by regular expression, are read and written as DeSkonto rather than by hand.

> **Greece — Peppol BIS, AFM, six-segment invoice number** — The AFM with the weighted checksum Peppol enforces on scheme 9933, and the six-segment invoice number GR-R-001 requires — supplier AFM, issue date, branch, myDATA document type, series, number — plus the MARK reference. myDATA reporting itself is a transmission and out of scope.

> **Belgium — Peppol BIS, KBO/BCE, structured communication** — Built on International.EInvoicing.Peppol, which the 2026 mandate is: the KBO/BCE enterprise number with its modulo 97 check, the structured communication, and the UBL.BE profile GLOBALUBL.BE judges — whose identifier came out of the rule set itself. The Belgian rules run once fetched; UBL.BE's own document-reference structure is not helped with yet.

> **Norway — EHF 3.0, organisasjonsnummer** — EHF 3.0, the national CIUS of Peppol BIS, and the organisation number whose modulo 11 check is measured against the rule Peppol publishes for scheme 0192. The Norwegian rules travel inside the Peppol rule set.

> **Sweden — Peppol BIS, organisationsnummer** — Peppol BIS Billing, and the organisation number whose Luhn check is measured against the rule Peppol publishes for scheme 0007. The Swedish rules travel inside the Peppol rule set.

> **Denmark — Peppol BIS, CVR, allowed payment means** — Peppol BIS Billing over NemHandel, the CVR number in the schemes Peppol reserves for it, and the payment means codes DK-R-005 allows — code 30 is valid EN 16931 and refused in Denmark. OIOUBL and NemHandel BIS 4 are not carried; see the roadmap.

> **Netherlands — NLCIUS and Peppol BIS, KvK and OIN** — NLCIUS and its G-account extension, with the Dutch rules that judge them; Peppol BIS with the Dutch rules inside it; and the legal entity identifier NL-R-003 and NL-R-005 reject an invoice for omitting — scheme 0106 or 0190 on both parties.

> **Romania — CIUS-RO (e-Factura)** — The national CIUS the e-Factura mandate exchanges, with the 244 assertions Romania publishes on top of EN 16931 — including BR-RO-100, which requires a Bucharest address to name its sector rather than the city. Rules fetched, not shipped.

> **Serbia — SRBDT (SEF)** — The SRBDT CIUS and its conformant extension, with the 134 assertions Serbia publishes — including RSR-05, which requires the tax point date code EN 16931 leaves optional. Rules fetched, not shipped.

> **Portugal — CIUS-PT** — The national CIUS, with the largest artefact here — over two thousand assertions, since CIUS-PT bundles the EN 16931 UBL rules with its own. It requires a delivery address, and numbers written to two decimals. Rules fetched, not shipped. SAF-T and ATCUD are separate obligations and out of scope.

> **Iceland — Peppol BIS, kennitala** — Peppol BIS Billing, and the kennitala with its modulo 11 check, written where IS-R-002 and IS-R-004 look for it.

> **Italy — Peppol BIS, partita IVA** — What Italy exchanges over Peppol: the partita IVA with the check Peppol publishes for scheme 0211, measured against it in both directions, and the full postal address IT-R-002 to IT-R-004 require. FatturaPA and the SDI are a separate project — their own syntax, and a qualified signature this library does not produce.

> **Slovakia — Peppol BIS, tax data document** — The 2027 mandate has two halves: a Peppol BIS invoice, and a tax data document reported to the financial administration within fifteen minutes. The second lives in International.EInvoicing.Peppol, since Slovakia's rules and the EU's ViDA ones are the same document; SlovakEInvoicing builds it from the invoice it reports — a projection, not a copy, since its rules forbid every element they do not name — and it satisfies all 88 assertions. Reading one back is a receiver's job and is not there yet. There is no Slovak CIUS published, and no publisher's rule for the ICO check digit, so this library invents neither.

> **Croatia — Peppol BIS, CIUS-HR, OIB** — The OIB both parties must carry under the Fiskalizacija 2.0 mandate, checked against ISO/IEC 7064 MOD 11,10, and CIUS-HR 2025 with its extension — profile and rules both, once the artefacts are fetched. An invoice this library writes satisfies all 74 Croatian assertions, including the three terms EN 16931 does not define: the time of issue and the operator who issued it, written by AddCroatianOperator. The advanced electronic seal and the fiscalisation reporting stay out: a signature and a transport.

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

**[How this library compares](docs/comparison.md)** puts it beside the alternatives, with sources: in March
2026 the .NET incumbent shipped its last open-source release and moved validation to a paid product, which
leaves this library as the only maintained open-source way to validate an e-invoice against the published
rules in .NET. The same page lists, without flattery, what the others do that this one does not.

**[The roadmap](docs/roadmap.md)** says what comes next and why, and carries the full country catalogue —
some fifty of them, grouped by what each would cost to add. The short version: two multipliers first, because
both change what every country below costs. **EN 16931-1:2026** was published in May 2026 and the 2017 edition
this library is built on was formally withdrawn; the revision is not backward compatible. And **Peppol PINT**
is the specification everywhere Peppol was adopted outside Europe — the UAE, Malaysia, Singapore, Japan,
Australia and New Zealand, probably the United Kingdom — which our Peppol package does not yet speak.
After those: a shared reporting model — the French flux 10, the Slovak tax data document and the ViDA one
beside it are the same envelope with different terms — with Hungary and Greece on top of it; then Italy and
Spain, each blocked on the signature decision; then Poland; then, once the model question in the roadmap is
answered, the clearance countries outside Europe.

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

## Testing your integration

```
dotnet add package International.EInvoicing.Testing
```

Sample documents that pass EN 16931, a round-trip harness, a corpus of documents that fight back, and
assertions that say what actually happened. Framework-free.
See [the guide](docs/guides/testing.md).

## From the command line

```
dotnet tool install --global International.EInvoicing.Cli --prerelease

einvoice validate invoice.xml            # against every rule set that applies, and it says which ran
einvoice inspect  invoice.pdf            # what is it, what profile, what did reading it report
einvoice convert  invoice.xml --to cii   # to the other syntax, with a report of what did not cross
```

Exit codes are `0` conforming, `1` rejected, `2` could not run — kept apart because a script that treats
"I had no rules for this" as success is a pipeline that passes while checking nothing.
See [the tool's README](src/International.EInvoicing.Cli/README.md).

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
