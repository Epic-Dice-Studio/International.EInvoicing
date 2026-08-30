# Roadmap

What is next, why it is next, and what would make it done. The support matrix in the
[README](../README.md) says what works **today**; this page says where the work goes from here.

> Regulatory dates move, and this page will go stale before the code does. Each date below is marked with
> where to check it. Recorded state: August 2026.

## How things get ordered

Four questions, in this order:

1. **Is something wrong?** A document a real integration receives and this library mishandles beats any new
   feature.
2. **Does it unlock more than itself?** Peppol is one package and ten countries. A national format is one
   country.
3. **Is the promise still true?** Every capability claims "extensible without forking", "nothing is lost",
   "honest about its limits". Work that keeps those true outranks work that adds surface.
4. **Is it cheaper now than later?** Locking the public API costs a day before 1.0 and a major version after.

---

## Now — correctness and the multipliers

### 1. UBL credit notes ✅ *done, August 2026*

**The problem.** A credit note in UBL has its own root element and its own line elements —
`CreditNote`, `cac:CreditNoteLine`, `cbc:CreditedQuantity` — and this library reads it as an invoice. Nothing
throws and nothing is lost, but the document comes back with no type code and no lines:

```
kind=Ubl  usable=True  creditNote=False
number=AV-2026-001  type=(none)  lines=0
  EIV2020 Element 'CreditNoteTypeCode' … kept as extension data
  EIV2020 Element 'CreditNoteLine'     … kept as extension data
```

CII is unaffected: there, a credit note is the same document with type code 381.

**Done.** The reader and writer take their four differing element names from `UblDocumentShape`, chosen from
the root when reading and from BT-3 when writing; `DocumentKind.UblCreditNote` tells the two apart before a
document is read; `IsCreditNote` consults the root in UBL and the type code in CII. Measured against the
official EN 16931 credit note: read with its lines, nothing left unmapped, round-tripped, and still accepted
by EN 16931 after being written back.

### 2. `International.EInvoicing.Peppol` ✅ *done, August 2026*

**Why first among the additions.** Belgium, the Netherlands, Norway, Sweden, Denmark, Finland, Ireland,
Iceland, Australia, New Zealand, Singapore and Japan all exchange Peppol BIS Billing 3.0. One package, then a
country is a code list and an identifier rather than a format.

**What it holds.** The CIUS profiles for both syntaxes, the EAS and ICD code lists, participant identifier
parsing and checking, and the rule-set registration for artefacts a caller fetched. Not: SMP lookup, not AS4.

**Done.** `PeppolParticipant` reads `0208:0203201340` with or without the network qualifier and says whether
its scheme is one the shipped code list knows; `PeppolEndpointScheme` carries that list, taken from the
EN 16931 artefacts this library already ships rather than transcribed, with a test comparing the two on every
build. `AddPeppolRulesFrom(directory)` loads all four rule sets Peppol publishes in one call — its own and
its copy of the EN 16931 ones, because both apply. Belgium is built on the package, and its validation entry
no longer says *planned*.

### 3. Cross-checking against an external validator

**The problem.** The engine agrees with every published corpus we can find — 23/23 EN 16931 examples, 86/86
XRechnung documents, 354/354 Peppol unit cases. All of that compares us against expected *results*. Nothing
compares us against another *engine*, so a rule both we and the corpus authors read the same wrong way would
go unnoticed.

**Done when** a nightly workflow runs the KoSIT validator over everything the samples and tests generate and
fails on disagreement, with the disagreements — not just the failures — reported.

### 4. Locking the public API ✅ *done, August 2026*

`Microsoft.CodeAnalysis.PublicApiAnalyzers` and `PackageValidation`, planned from the start and still absent.
The developer-experience pass moved a lot of surface; this is the moment to freeze it, while a rename is a
diff rather than a major version.

**Done.** Every shipping package carries the pair of files, nullability included, and adding or removing a
public member fails the build until it is recorded. The analyzer's own rules found three places publishing
overlapping optional parameters — `SecureXml.CreateReader`, `FrCdar.Collected`, `EInvoicing.Create` — each
now spelled out. See [ADR 0011](adr/0011-public-api-tracking.md).

### 5. Finishing France

The lifecycle and e-reporting are complete and measured. The invoice is not: the profile is registered and
the two thousand French assertions run, but nothing helps a caller *satisfy* them — the SIREN of both
parties, the VAT payment option, the delivery address, public-procurement references.

**Done when** a French invoice can be built from the country package the way a lifecycle message can, and the
result passes `EXTENDED-CTC-FR` and `BR-FR-Flux2` on every build.

---

## Next — the promises, kept

| | Why it matters |
|---|---|
| **UBL ↔ CII conversion, with a loss report** | A French recipient must accept UBL, CII and Factur-X. Converting between them is a real requirement, and doing it silently is the dangerous version — the report is the feature. |
| **The write pipeline** (`IWritePipelineStep`) | "Hook into generation" is a guide that has been marked *planned* since the beginning. Numbering, house rounding, signing, logging — all of it belongs in a step rather than in a fork. |
| **Embedded code lists** | UNTDID 1001/2379/4461/5305/7143, ISO 4217/3166, EAS/ICD. Today they exist only inside Schematron, so the library can punish a wrong code but cannot help pick a right one. |
| **A CLI** — `dotnet einvoice validate invoice.xml` | The reference validator in this space is a Java jar. There is no .NET equivalent. Cheap to build on what already exists, and the most visible thing we could ship. |
| **`International.EInvoicing.Testing`** | The golden corpus, the assertions and the round-trip harness, so an integrator can test *their* profile with our tools. |
| **Property-based tests on rounding** | The `BR-CO` rules comparing totals to sums of lines are exactly where implementations break. Generated cases find what hand-written ones do not. |
| **German structured *Skonto*** | Early-payment discounts encoded inside BT-20's free text. Two long threads in two ecosystems; free text here today. |
| **The rest of the hostile corpus** | Declared encoding against actual encoding — the single most discussed issue in the KoSIT tracker — plus attachment zip-bombs, ten-thousand-deep nesting, malformed PDFs. |
| **Factur-X and Belgian rule sets** | Both say *planned* for validation in the matrix. The engine runs them; they are not wired. |

---

## Countries, in waves

### Wave 1 — the Peppol dividend

Once `.Peppol` exists, these are a code list, a national identifier and a rule set each:

**Netherlands · Norway · Sweden · Denmark · Finland · Ireland · Iceland**, then
**Australia / New Zealand** (A-NZ BIS), **Singapore** (InvoiceNow), **Japan** (Peppol JP).

Ten-plus countries for roughly the cost of one. This is why Peppol is in *Now* and not here.

### Wave 2 — European mandates with formats of their own

Each is a genuine project. Ordered by how much of what we have already applies:

| Country | Format | What makes it work | What makes it hard |
|---|---|---|---|
| **Romania** | RO e-Factura | A CIUS of EN 16931 — our model fits | Its own rule set and upload rules |
| **Hungary** | RTIR | Reporting, not invoicing — reuses the flux 10 shape | Real-time, per transaction |
| **Greece** | myDATA | Same: a reporting model we already have a prototype of | Classification codes with no European equivalent |
| **Italy** | FatturaPA / SDI | Well documented, huge installed base | **XAdES signature required** — a scope decision, see below |
| **Spain** | Facturae + VeriFactu + TicketBAI | Facturae is close to EN 16931 | Three regimes at once, one of them regional (Basque Country) |
| **Portugal** | SAF-T + ATCUD / QR | The invoice part is manageable | Certified series and a QR code leave the XML entirely |
| **Poland** | KSeF | Large market | FA(2)/FA(3) is far from EN 16931; the calendar has already moved twice |

### Wave 3 — outside Europe

Not before 1.0. **Brazil** (NF-e), **Mexico** (CFDI), **India** (IRP/GST), **Saudi Arabia** (ZATCA),
**Türkiye** (e-Fatura), **Malaysia** (MyInvois), **Vietnam**. Different documents, different tax models,
usually a signature and an issuing authority in the loop.

### The thread running through all of it: ViDA

European *Digital Reporting Requirements* are converging on near-real-time reporting. The French flux 10
model — a period, a transmission, a VAT split, payments — is the right prototype for it. Generalising it into
a shared reporting model **before** adding Hungary and Greece saves writing it three times.

---

## Decisions still open

**Electronic signatures.** Italy and Spain need XAdES on the document. The scope line so far has been "the
document, not the transport" — and a signature *is* part of the document, so the line does not settle it.
Deciding costs nothing today and blocks Wave 2 tomorrow.

**How far the model bends for non-European formats.** NF-e and CFDI are not EN 16931 invoices wearing a
different syntax; they are different documents. Either the canonical model grows a way to say "this is not an
EN 16931 invoice", or those countries get their own model the way e-reporting did. E-reporting suggests the
second answer is the honest one.

**Publishing 1.0.** Everything above is easier before it and dearer after. The gate should be: no known
correctness gap, the public API locked, and one country complete end to end. France is closest.

---

## Not doing, on purpose

Sending documents — no AS4 client, no access point API, no Chorus Pro connector. Producing the PDF a human
reads. Both are stated in the [README](../README.md) and neither is a maybe: the library performs no network
I/O at all, which is what lets it run in a browser and makes it auditable.
