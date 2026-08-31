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

### 3. Cross-checking what we write

**The problem.** The engine agrees with every published corpus we can find — 23/23 EN 16931 examples, 86/86
XRechnung documents, 354/354 Peppol unit cases. All of that compares us against expected *results*. Nothing
compares us against another *engine*, so a rule both we and the corpus authors read the same wrong way would
go unnoticed.

**Half done.** Every country package now measures what this library *writes* against the rules of the country
it is for: XRechnung against the German rules, Peppol against Peppol's own, France against the DGFiP's, each
in both syntaxes and against EN 16931 underneath. That found two real gaps on the first run — the Peppol
business process, which EN 16931 does not require and the network does, and an invalid Belgian enterprise
number our own checker would have caught.

**Still to do:** the external comparison. Running the KoSIT validator alongside our engine and failing on
disagreement is the part no corpus can replace, and it needs a Java toolchain in CI.

### 4. Locking the public API ✅ *done, August 2026*

`Microsoft.CodeAnalysis.PublicApiAnalyzers` and `PackageValidation`, planned from the start and still absent.
The developer-experience pass moved a lot of surface; this is the moment to freeze it, while a rename is a
diff rather than a major version.

**Done.** Every shipping package carries the pair of files, nullability included, and adding or removing a
public member fails the build until it is recorded. The analyzer's own rules found three places publishing
overlapping optional parameters — `SecureXml.CreateReader`, `FrCdar.Collected`, `EInvoicing.Create` — each
now spelled out. See [ADR 0011](adr/0011-public-api-tracking.md).

### 5. Finishing France ✅ *done, August 2026*

The lifecycle and e-reporting are complete and measured. The invoice is not: the profile is registered and
the two thousand French assertions run, but nothing helps a caller *satisfy* them — the SIREN of both
parties, the VAT payment option, the delivery address, public-procurement references.

**Done.** `ForFrance()` adds the invoicing case and the three mandatory mentions; `FromFrenchSeller` and
`ToFrenchBuyer` identify both parties by a SIREN checked before it is written. An invoice built that way
satisfies EN 16931, `BR-FR-Flux2` and `EXTENDED-CTC-FR` in both syntaxes, measured on every build.

Writing it uncovered a second defect: UBL has no element for the note subject code, carrying it as a
`#AAB#` prefix, and this library dropped it on the way out and kept it as text on the way in.

### 6. One country, one object ✅ *done, August 2026*

**The problem.** Everything the library can do was reachable, and nothing said where to start. A caller who
only ever invoices in France had to know the profile, the business process, the four documents France
exchanges and which rule sets to register — before the first line of their own code.

**Done.** `FrenchEInvoicing`, `GermanEInvoicing` and `BelgianEInvoicing` hold that knowledge. France reads
all four of its documents through one call and says which arrived, including the *flux 10* report, which
carries no XML namespace and was the one a caller would have missed. Germany parses the Leitweg-ID before
writing it. Belgium checks the enterprise number and writes it in the scheme Peppol reserves for it. Each
exposes `.Library`, so the shortcut is a shortcut and not a fence. See
[the guide](guides/country-shortcuts.md).

### 7. EN 16931-1:2026 — the standard moved under us 🚧 *first half done, August 2026*

**The problem.** CEN published **EN 16931-1:2026** in May 2026 and **formally withdrew the 2017 version**;
the 2017 model stays compliant only for a migration period. Everything this library is built on — the
semantic model, the two syntax bindings, the shipped validation artefacts, every CIUS on top of them — names
the 2017 edition. The revision is a ViDA revision: new terms for the 2030 Digital Reporting Requirements,
invoice coding, multiple orders, early-payment discounts and late-payment charges, wider handling of exempt
supplies and special VAT schemes, an extension methodology, and updated UBL and CII bindings. It is
**not backward compatible**.

**Why it outranks every country below.** A country is one package. This is the model underneath all of
them, and it decides what our public API has to look like — which is exactly the thing we just locked.
Adding fields to `EInvoice` is a minor version; changing what a field means is not.

**Done.** Two things, and they are the two that could be done honestly today.

The shipped artefacts went from **1.3.13** (October 2024) to **1.3.16** (April 2026) — two years of published
corrections, and the EAS code list with them. Running them found a real defect in our XPath engine: a step's
predicate was applied to the whole flattened result rather than to each node the step started from, so
`a/b[1]` meant the first `b` in the document instead of the first `b` of *each* `a`. BR-CO-11 and BR-CO-12
were rewritten in 1.3.16 to sum `ActualAmount[1]` across every document-level allowance, which is exactly the
shape that exposes it: any invoice with two allowances was being rejected for arithmetic it had got right.

And the edition is now something the library can name. `En16931Edition` reads the edition out of the
specification identifier; a document declaring an edition we do not implement is reported as
`EIV1044 UnsupportedEdition` — an EN 16931 invoice of an edition we do not carry — rather than as an unknown
profile, read as far as the 2017 model reaches with everything else kept in extension data, and validated
against rules the report names as the 2017 ones. See [ADR 0013](adr/0013-en16931-editions.md).

**Still to do, and blocked on publication.** The standard text is sold, not published, so the terms the
revision adds cannot be derived from anything this repository may carry; and the 2026 validation artefacts do
not exist yet — the maintainer of the EN 16931 artefacts
[said in April 2026](https://github.com/ConnectingEurope/eInvoicing-EN16931/issues/445) that work on them was
starting. When they land: the 2026 model beside the 2017 one, chosen by the declared profile, with a loss
report for a document carried from one to the other — the same loss report the UBL ↔ CII conversion needs.

*Check: [ec.europa.eu — obtaining a copy of the standard](https://ec.europa.eu/digital-building-blocks/sites/spaces/DIGITAL/pages/467108971/Obtaining+a+copy+of+the+European+standard+on+eInvoicing).*

### 8. `International.EInvoicing.Peppol` — the PINT half 🚧 *the profiles done, August 2026*

**The problem.** `PeppolProfiles` knew BIS Billing 3.0, which is a strict EN 16931 CIUS and was never meant
to leave Europe. Everywhere Peppol has been adopted since — the UAE, Malaysia, Singapore, Japan, Australia
and New Zealand, Oman, and probably the United Kingdom — runs on **Peppol PINT**, a different specification
with a common core and one jurisdiction specialisation each. Those countries looked covered by our Peppol
package. They were not.

**Done.** `PeppolPintProfiles` carries all of them — the common core and the EU, UAE (billing and
self-billing), A-NZ, Japanese, Malaysian, Omani and Singaporean specialisations. Not one identifier is
transcribed from prose: each is read out of the published rule artefact for its jurisdiction, which
`build/fetch-specs.sh pint` puts on disk, and a test fails the build if one stops appearing there.

Doing it surfaced the trap that would have caught every caller: **the PINT business process is a different
string**. BIS Billing numbers its processes (`urn:fdc:peppol.eu:2017:poacc:billing:01:1.0`) and PINT does not
(`urn:peppol:bis:billing`). An invoice carrying the wrong one is wrong in a way that looks right, so
`ForPeppolPint()` exists beside `ForPeppol()` rather than a flag on one method.

**The rules run too, since August 2026.** This looked blocked: OpenPEPPOL publishes PINT's artefacts as
pre-compiled XSLT 2.0 generated by Saxon, which neither our Schematron engine nor .NET's own XSLT processor
can execute. The way through was to notice that a compiled Schematron still contains every original assertion
verbatim, and to read them — not translate them. `CompiledSchematron` does that, and is held to the one rule
set that exists in both forms at the same version: every assertion of EN 16931 1.3.16 must come out
identical from the compiled artefact and from the source this repository ships.

Putting an invoice we wrote in front of the real A-NZ rules then found a defect nothing else had:
**the tax scheme was hard-coded to `VAT`**, and Australia and New Zealand require `GST`. Four fatal rules.
`EInvoice.TaxSchemeIdentifier` now carries it, the readers and writers respect it, and BT-31 is recognised
under whatever the document's scheme is called rather than only under the word VAT. See
[the PINT page](standards/peppol-pint.md).

**And it converges.** OpenPEPPOL's **BIS 4**, built on EN 16931-1:2026, is meant to merge BIS Billing 3.0 and
PINT into one global specification. Denmark has already cancelled OIOUBL 3.0 and committed to **NemHandel
BIS 4** as its only domestic format by 2029 — the first national format retired in its favour.

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
| ~~**Factur-X and Belgian rule sets**~~ | ✅ **done.** Both are published as compiled XSLT, which is why they sat unwired — reading that came later. Wiring Factur-X immediately found that this library wrote `@currencyID` on CII amounts that forbid it, which would have had every ZUGFeRD document rejected. |

---

## Countries — the whole catalogue

Every country worth integrating, grouped by **what it costs us**, because that is what decides the order.
The three done ones are at the top so the tiers read as one list.

> Dates below are the state as recorded **August 2026**. Mandate calendars move — Poland's has moved twice
> — so treat every date as needing a check before it is quoted, and prefer the national portal to this page.
> The primary sources worth keeping open:
> [the Commission's eInvoicing country factsheets](https://ec.europa.eu/digital-building-blocks/sites/spaces/einvoicingCFS/pages/),
> [OpenPEPPOL](https://docs.peppol.eu/), [xeinkauf.de](https://xeinkauf.de/xrechnung/) for Germany,
> [impots.gouv.fr](https://www.impots.gouv.fr/specifications-externes-b2b-de-la-facturation-electronique) for
> France, and [e-invoice.belgium.be](https://e-invoice.belgium.be/) for Belgium.

### Tier 0 — done

| Country | What it needs | State |
|---|---|---|
| **France** | Extended CTC FR, Factur-X, CDAR lifecycle, flux 10 e-reporting, SIREN/SIRET | ✅ complete, `FrenchEInvoicing` |
| **Germany** | XRechnung 3.x CIUS + Extension, Leitweg-ID, ZUGFeRD | ✅ complete, `GermanEInvoicing` |
| **Belgium** | Peppol BIS Billing 3.0, KBO/BCE, structured communication | ✅ complete, `BelgianEInvoicing` |
| **Norway** | EHF 3.0, organisasjonsnummer (mod 11), national rules inside Peppol | ✅ complete, `NorwegianEInvoicing` |
| **Sweden** | Peppol BIS Billing, organisationsnummer (Luhn), national rules inside Peppol | ✅ complete, `SwedishEInvoicing` |
| **Denmark** | Peppol BIS Billing, CVR, the payment means `DK-R-005` allows | ✅ complete, `DanishEInvoicing` |
| **Netherlands** | NLCIUS and Peppol BIS, the KvK/OIN scheme `NL-R-003` demands | ✅ complete, `DutchEInvoicing` |
| **Iceland** | Peppol BIS Billing, kennitala in the scheme `IS-R-002` demands | ✅ complete, `IcelandicEInvoicing` |
| **Croatia** | Peppol BIS Billing, OIB on both parties | 🚧 the invoice, `CroatianEInvoicing` — the seal and the fiscalisation reporting are not ours to do |
| **Australia** | Peppol PINT (A-NZ), ABN, GST | ✅ `AustralianEInvoicing`, validated against the real PINT rules |
| **New Zealand** | Peppol PINT (A-NZ), NZBN, GST | ✅ `NewZealandEInvoicing`, the same |
| **Singapore** | Peppol PINT (SG), GST, Singapore's own category codes | ✅ `SingaporeEInvoicing` |
| **Malaysia** | Peppol PINT (MY), BRN and TIN, its own category codes | ✅ `MalaysianEInvoicing` |
| **Japan** | Peppol PINT (JP), the invoice period its rules require | ✅ `JapaneseEInvoicing` |

France's own calendar: reception for everyone and issuing for large and mid-sized companies on
**1 September 2026**, issuing for the rest on **1 September 2027**. Belgium's B2B mandate started
**1 January 2026**; Germany's issuing obligation starts **1 January 2027** above €800,000 turnover and
**1 January 2028** for everyone.

### Tier 1 — Peppol BIS Billing 3.0 countries · *a code list, an identifier and a rule set each*

`.Peppol` already carries the profiles, the EAS/ICD list and participant parsing. What each of these adds is
its national CIUS rules, its legal identifier and, where it exists, its national profile identifier.

| Country | Profile / national CIUS | Identifier we would add | Mandate state |
|---|---|---|---|
| **Finland** | Peppol BIS, Finvoice 3.0, TEAPPSXML 3.0 | Y-tunnus | B2G mandatory; B2B receiving right since 2020 |
| **Ireland** | Peppol BIS 3.0 | Tax Reference Number | B2B from **1 November 2028**, large corporates first |
| **Lithuania** | Peppol BIS Billing | Company code | B2G since 2017 |
| **Latvia** | Peppol BIS Billing | Registration number | B2G from **January 2026** |
| **Estonia** | Peppol BIS, Estonian e-invoice standard | Registrikood | B2G mandatory; B2B expected ~2027 |
| **Luxembourg** | Peppol BIS Billing | Matricule | B2G mandatory |
| **Austria** | Peppol BIS **and ebInterface** — see Tier 3 | UID | B2G mandatory |
| **Switzerland** | Peppol BIS, plus swissDIGIN / eCH-011 | UID (CHE) | B2G above CHF 5,000; B2B voluntary |
| **Slovakia** | Peppol, five-corner model | IČO | B2B from **1 January 2027** |
| **Slovenia** | e-SLOG, Peppol | Matična številka | B2G mandatory; B2B plans unconfirmed |
| **Cyprus · Malta · Bulgaria · Greece (B2G)** | Peppol BIS | national VAT identifiers | B2G mandatory |

Five of them are done — see Tier 0. Norway, Sweden, Denmark, the Netherlands and Iceland came first for one
reason: everything needed to build them **and check them** was already in the repository. Their national
rules travel inside the Peppol rule set this library loads, and Peppol publishes the check for each of their
legal identifiers, so each country could be measured rather than asserted. That is the template for the rest
of the tier, and it is also the filter: a country whose facts are not in an artefact we hold waits.

**NLCIUS is done**, and how it got done is worth recording. It had been left out because its specification
identifier was "in no artefact this repository carries" — which was a statement about the fetch list, not
about the world. The identifier is in the Dutch rule set itself, which is now fetched. The same source
carries Romania's, Serbia's and Portugal's. Denmark's **OIOUBL 2.1** remains a syntax of its own, and a
separate project.

**Croatia turned out not to belong in this tier at all**, and finding out is worth recording. From the
outside it looked like the cheapest kind of country — Peppol, EN 16931, a national CIUS. In fact
*Fiskalizacija 2.0* requires three things per invoice, and two of them are not documents: an **advanced
electronic seal** produced with a certificate the invoicing system holds, and **two fiscalisation reports**,
one from each party, to the tax administration. This library signs nothing and performs no network I/O, so
what it can do is the third thing — a valid invoice carrying both OIBs — which
[`CroatianEInvoicing`](standards/country-hr.md) now does. The rest belongs to the caller, and the signature
question it raises is the same one Italy and Spain raise, still open below.

**Slovakia** is the genuine Tier 1 remainder: Peppol BIS Billing 3.0 with a Slovak CIUS from
**1 January 2027**, plus an SK tax data document sent to the financial administration within fifteen minutes
— again transport, not document.

A pattern that looked like a wall turned out to be a fetch list. NLCIUS, HR-FISK 2.0 and the Slovak CIUS were
all recorded as "blocked on an identifier no artefact we hold publishes". For NLCIUS that was wrong: the
identifier is in the Dutch rule set, and once the national rule modules were fetched it fell out immediately —
along with Romania's, Serbia's and Portugal's. **HR-FISK 2.0 and the Slovak CIUS are still genuinely
missing**, but the lesson stands: before declaring a fact unobtainable, check whether it is merely
unfetched.

### Tier 2 — Peppol **PINT** jurisdictions · *the profiles exist now; each needs its identifiers and rules*

| Country | Specialisation | Mandate state |
|---|---|---|
| **Australia** | A-NZ PINT | ✅ `AustralianEInvoicing` — B2G mandatory; B2B voluntary, strongly adopted |
| **New Zealand** | the same A-NZ PINT specialisation | ✅ `NewZealandEInvoicing` — the NZBN is a GLN, routed under 0088 |
| **Singapore** | InvoiceNow (SG PINT) | ✅ `SingaporeEInvoicing`, validated against Singapore's own rules |
| **Japan** | JP PINT | ✅ `JapaneseEInvoicing`; qualified-invoice system since 2023 |
| **Malaysia** | MyInvois — PINT MY alongside the national API | ✅ `MalaysianEInvoicing`; phased since 2024, above RM 1 m since **January 2026** |
| **United Arab Emirates** | PINT AE, five-corner DCTCE model | **not a thin one** — see below. Pilot **July 2026**, mandatory **1 January 2027** above AED 50 m, **1 July 2027** below |
| **United Kingdom** | PINT UK, expected | mandatory VAT e-invoicing announced for **April 2029**; roadmap due Budget 2026 |
| **Oman** | PINT OM | **not a thin one either** — 34 business terms of its own (`BTOM-001` … `BTOM-034`) across 79 rules, exactly like the UAE. Needs model work first. |

Malaysia and the UAE also need their national submission rules, which are transport — out of scope here —
but their *documents* are PINT, and the document is what we do.

**The UAE is the exception in this tier, and reading its rules is what showed it.** Where Singapore, Malaysia
and Japan add code lists and mandatory fields that EN 16931 already has terms for, the UAE adds **business
terms of its own** — the `BTAE-xx` series: an authority name, a legal registration identifier *type*, a
passport issuing country, mandatory Incoterms. Our model has no terms for those, so a conforming Emirati
invoice needs model work first, not just a facade. It belongs with Croatia and Italy as a project rather than
with its PINT neighbours as a package. Each of these is now a country package of the
same shape as the Nordic ones: a profile that already exists, a legal identifier, and the jurisdiction rules
once they can be run.

### Tier 3 — EN 16931 relatives with a format of their own · *a reader, a writer and a rule set*

Our model fits; the syntax or the rules do not, quite.

| Country | Format | What we already have that applies | What is genuinely new |
|---|---|---|---|
| **Romania** | ✅ **done** — `RoProfiles`, 244 assertions running | — | — |
| **Austria** | ebInterface 6.x | the model | a second national XML alongside Peppol |
| **Italy** | FatturaPA, via SDI | the model maps to EN 16931 | its own XML tree, `Fattura` type codes, **and a qualified signature** — see below. Spec **v1.9.1** in force since May 2026, with rejections where earlier versions warned |
| **Spain** | Facturae 3.2.x | close to EN 16931 | plus **VeriFactu** (software certification and invoice chaining, from **1 January 2026** for corporates, **1 July 2026** for the self-employed) and **TicketBAI** in the Basque Country — three regimes at once |
| **Poland** | KSeF, FA(3) | little — FA(3) is far from EN 16931 | clearance model; live for large taxpayers **February 2026**, most others **April 2026**, micro **January 2027** |
| **Serbia** | ✅ **done** — `RsProfiles`, 134 assertions running | — | — |
| **Czechia** | ISDOC, Peppol growing | the model | ISDOC is its own schema; no B2B mandate yet |
| **Türkiye** | e-Fatura, **UBL-TR 1.2.1** since February 2026 | the syntax | its own CIUS and portal rules |

### Tier 4 — reporting regimes · *the flux 10 shape, generalised*

These are not invoice formats. They are **transmissions about invoices**, which is exactly the document
French e-reporting already gave us a model for. Building a shared reporting model before writing the second
one saves writing it three times.

| Country | Regime | Shape |
|---|---|---|
| **Hungary** | RTIR / NAV | real time, per invoice, NAV XML — invoices themselves stay UBL, CII, Peppol or PDF |
| **Greece** | myDATA | per document, with classification codes that have no European equivalent |
| **Portugal** | SAF-T (PT) + **ATCUD** and QR code | periodic file, plus certified series and a QR code that leave the XML entirely |
| **Spain** | VeriFactu / SII | invoice chaining and near-real-time ledger reporting |
| **Romania** | SAF-T D406, e-Transport | periodic, alongside e-Factura |
| **The EU, 2030** | ViDA Digital Reporting Requirements | the reason all of the above converge — and the reason EN 16931-1:2026 exists |

### Tier 5 — clearance countries outside Europe · *a different document, not a different syntax*

Each has its own model, its own tax vocabulary, a signature, and an authority in the loop that decides
whether an invoice legally exists. None of them is an EN 16931 invoice in different clothes.

| Region | Countries |
|---|---|
| **Latin America** | **Brazil** (NF-e for goods, NFS-e for services — both being reworked for the IBS/CBS reform), **Mexico** (CFDI 4.0, cleared by a PAC), **Chile** (DTE), **Colombia** (UBL 2.1, cleared by DIAN), **Peru**, **Argentina**, **Ecuador**, **Uruguay**, **Bolivia**, **Paraguay**, **Costa Rica**, **Panama**, **Guatemala**, **El Salvador**, **Dominican Republic** |
| **Asia** | **India** (IRP / GST, a JSON schema — not XML), **China** (fully digitalised e-fapiao), **South Korea** (e-Tax invoice), **Vietnam**, **Indonesia**, **Philippines**, **Thailand** |
| **Middle East & Africa** | **Saudi Arabia** (ZATCA, waves 23 and 24 through 2026, UBL with a cryptographic stamp), **Egypt** (ETA), **Israel** (allocation numbers, threshold down to NIS 5,000 in June 2026), **Kenya** (eTIMS), **Nigeria** (FIRS) |

Latin America is where e-invoicing was invented and where coverage is universal; it is also where our
canonical model fits worst. That is the open decision below, not a scheduling question.

### What that means for the order

1. **EN 16931-1:2026 and PINT** — both are multipliers, both change what the countries below cost, and both
   are cheaper now than after another dozen profiles are built on the 2017 model.
2. **Croatia and Slovakia** — live or dated B2B mandates, Tier 1 cost.
3. **Romania and Italy** — the two European formats with the largest installed base that we do not read.
   Italy needs the signature decision first.
4. **A shared reporting model**, then Hungary and Greece on top of it.
5. **Spain and Poland**, each a project in its own right.
6. Outside Europe, only after the model question below is answered.

---

## Decisions still open

**Electronic signatures.** Italy and Spain need XAdES on the document, and Saudi Arabia a cryptographic
stamp. The scope line so far has been "the
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
