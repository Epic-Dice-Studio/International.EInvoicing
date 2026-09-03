# Roadmap

What is next, why it is next, and what would make it done. The support matrix in the
[README](https://github.com/Epic-Dice-Studio/International.EInvoicing/blob/main/README.md) says what works **today**; this page says where the work goes from here.

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

**Done, September 2026.** The external comparison runs. "It needs a Java toolchain in CI" turned out to be
`actions/setup-java` — one step, not a blocker, and worth remembering before accepting the next one.
`tests/International.EInvoicing.CrossCheck.Tests` runs the KoSIT validator over the official XRechnung
corpus, compares acceptance *and* which rules each engine fires, and skips when the JVM or the artefacts are
absent.

**It paid for itself on the first run.** This library was validating every CII document against the **D22B**
schemas, where EN 16931's CII syntax binding — and XRechnung, and Factur-X, and Peppol — name **D16B**. The
two revisions share their namespaces, so the wrong schema attached silently and rejected values the right one
allows: a conforming XRechnung invoice with an allowance reason code of `TAC` came back invalid. No corpus
could have caught it, because our CII corpus and our CII schema were never run against each other. `D16B` is
now the default, `AddCiiSchema(CiiSchemaVersion.D22B)` is there for a document that needs it.

**The four disagreements it first reported were the comparison's fault, not the engine's.** KoSIT's
acceptance is decided by its scenario's own `acceptMatch`, and the XRechnung scenarios accept a document that
broke EN 16931 rules: `05.01a-INVOICE_ubl` states a payable amount thirty euros above its total, and the
reference reports `BR-CO-16` as an error *and accepts the document*. Comparing that against this library's
`IsValid` — "nothing was reported as an error" — compared two different questions.

Compared on what is comparable, the two engines agree completely: **over all eighty-six documents they report
exactly the same rules at error level, and every rule the reference fires this library fires too.** Both
directions are checked, because a rule we fire and the reference does not is a document rejected for
something the authorities would have passed.

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
| ~~**UBL ↔ CII conversion, with a loss report**~~ | ✅ **done.** `EInvoicing.Convert` returns the document *and* what the crossing cost. The losses are found rather than predicted: the result is read back, and what it reports is recorded along with every extension element the source carried — syntax-specific by definition, with nowhere to go on the other side. It deliberately does not diff the two models, because every mapped term survives by construction. See [the guide](guides/convert-between-syntaxes.md). |
| ~~**The write pipeline** (`IWritePipelineStep`)~~ | ✅ **done.** `AddWriteStep` runs your own logic inside generation, in ASP.NET Core's middleware shape. The steps wrap the writer rather than being called by the facade, so taking `library.UblWriter` and using it directly runs them too — a guarantee with a bypass is not a guarantee. See [the guide](guides/hook-into-generation.md). A **read** pipeline does not exist: what arrives is described by diagnostics rather than transformed, and nothing has yet asked for more. |
| ~~**Embedded code lists**~~ | ✅ **done.** Invoice and credit-note types, VAT categories, payment means, the EAS schemes, and now ISO 4217, ISO 3166-1, ISO 6523 ICD, item classification, allowance and charge reasons, and VATEX — each read from the shipped artefact and compared to it on every build. Doing it found the credit-note list five codes short, so a credit note carrying 420, 458, 502, 503 or 532 was read as an invoice; and it kept `XI` and `1A` in the country list, which a transcription from ISO 3166-1 would have dropped. Unit codes (UN/ECE Rec 20, some 1500 of them) are the one list still out. |
| ~~**A CLI** — `einvoice validate invoice.xml`~~ | ✅ **done.** `International.EInvoicing.Cli`, a `dotnet tool`: `validate`, `inspect`, `convert`, `profiles`, `rules`. Exit codes keep *rejected* and *could not run* apart, and a document that declares a specialisation only the base judged is said out loud. Building it found two real defects — `cbc:ChargeIndicator` was never marked as mapped, so every allowance or charge left a duplicate in extension data, and the writers re-emitted the *other* syntax's extension elements, which put UBL elements inside converted CII documents. |
| ~~**`International.EInvoicing.Testing`**~~ | ✅ **done.** Conforming samples, a round-trip harness that checks by element census rather than by text, the hostile-document corpus, and assertions that carry the evidence into the failure message. Framework-free. Writing it found that a truncated file was reported as *unrecognised* rather than *malformed*. See [the guide](guides/testing.md). |
| ~~**Property-based tests on rounding**~~ | ✅ **done.** Three hundred generated invoices per syntax, judged by the official artefact, with a fixed seed printed in every failure. It found on the first run that an amount assigned as a plain `decimal` reached UBL with no `currencyID` — mandatory there, and BR-CL-03 requires an ISO 4217 code, so those documents died at the schema before a rule ran. No property-based framework was pulled in: the generator is twenty lines, and every failing case is one invoice you can print. |
| ~~**German structured *Skonto***~~ | ✅ **done.** `DeSkonto`, `SkontoTerms()` and `WithSkonto()` in `Countries.Germany`. An early-payment discount is a number your accounting system needs, and Germany puts it inside BT-20's free text, where `BR-DE-18` judges it with a regular expression: the percentage needs exactly two decimals, the keywords capitals, and the last statement a trailing line break — the one a hand-rolled writer forgets. The expression is read out of the shipped `common.sch` in the tests rather than transcribed, so what this reads is what the rule accepts, and an invoice carrying the statements it writes is put in front of the German rules in both syntaxes. |
| ~~**The rest of the hostile corpus**~~ | ✅ **done**, and it earned its keep. Declared encoding against actual encoding found that everything was decoded as UTF-8 regardless, so a Latin-1 sender's `Müller` arrived as `M?ller`. Deep nesting and oversized attachments found three limits that `DocumentLimits` declared and nothing enforced — documented reassurance a reader relied on. All of it is in `International.EInvoicing.Testing`, so an integrator can point it at their own reader. |
| ~~**XSD schema validation**~~ | ✅ **done** for UBL, and it paid for itself on the first run. `AddUblSchema()` puts the OASIS schemas — embedded, offline — into the report as a rule set like any other. It catches what no business rule looks at, starting with the shape this library had shipped the day before: two bank accounts inside one `cac:PaymentMeans`. It also found a defect nobody had a symptom for: **extension data is re-emitted at the end of its node**, and element order is normative in UBL, so a document carrying a term the model has no field for comes back in an order the schema rejects — six of the twenty-three official examples. Nothing this library *builds* is affected, only what it reads back and rewrites. CII is not covered: the UN/CEFACT D22B package is an archive this repository does not carry. |
| ~~**Anchor extension data where it was read from**~~ | ✅ **done for UBL, September 2026.** An unmapped element used to be written back at the end of its node, which UBL's normative order does not allow. It now remembers the mapped sibling it followed, and the writer flushes it there. The 163 call sites turned out not to need touching: `UblDocument` grew the `XmlWriter` shape the writers already spoke, so routing through it changed the type in 33 signatures and nothing else. The reader is the half that made it work — an element is anchored only if the node holding it is the node its sibling belongs to, so `UblValueReader` now records which node each element's content belongs to, and a party's or an address's extensions stay inside the party or the address instead of bubbling to the invoice. Proved by disabling ownership and watching exactly the two tests that should fail, fail. **Not done for CII**, where the order is normative too and the writers still flush at end-of-node; the reader half there is a different shape and is its own entry below. |
| ~~**XSD schema validation** (original entry)~~ | The cheapest gap the [comparison](comparison.md) found. A document can be schema-invalid and still pass every Schematron rule that does not happen to look at it — and the UBL 2.1 and CII D22B schemas are already under `specs/`, unused. Every Java tool does this; so does the one .NET Factur-X library that validates at all. |
| ~~**The Factur-X metadata we write is not the document's own**~~ | ✅ **done, September 2026.** PDFsharp generates its own XMP while saving and points the catalogue at it whatever was there, so the Factur-X block used to end up in the file as an object nothing referenced — a receiver reading the document's metadata, which is what the specification tells it to do, saw a producer string and no profile. It is now written after the save, as a **PDF incremental update**: the object the catalogue already points at is superseded by one holding what the backend wrote *plus* the Factur-X block, and every existing byte offset stays where it is. Poppler and this library's own reader both find the profile in the catalogue. Two things came with it: the **PDF/A extension schema** describing the four `fx` properties, which the specification requires and this library omitted, and the **conformance level of the source document**, which the backend used to discard — a caller who starts from the PDF/A-3 file Factur-X asks for gets one back that still says so. What this still does not do is *make* a PDF PDF/A ([ADR 0010](adr/0010-no-pdf-rendering.md)), so a document that claimed nothing is given nothing. |
| ~~**The Factur-X container checks that are ours**~~ | ✅ **done for what arrives.** [EIV4011](diagnostics/EIV4011.md): the XMP says which profile and which file name the PDF carries, and a document whose metadata claims EN 16931 over MINIMUM XML is read as two different invoices by two receivers who each believe they are right. Nothing else in the chain looks — no Schematron rule reads a PDF, and neither does a schema. Both the Factur-X and the ZUGFeRD namespaces are read. Full PDF/A conformance stays out: that is veraPDF's specification and veraPDF's implementation. |
| ~~**Getting the readable copy, and the supporting documents, out**~~ | ✅ **done, September 2026.** Three types, named apart because the naming is the point: `InvoiceRendition` is *the invoice, readable* — for a hybrid invoice the PDF it arrived in, which used to be dropped once the XML was out of it; `SupportingDocument` is *something else, attached* (BG-24 as BT-125), decoded, with its media type, its file name and a stream; `SupportingDocumentLink` is *something else, not attached* (BT-124), which deliberately has no way to open it, because fetching a URI off a third party's invoice is network I/O this library does not do and a decision that stays the caller's. A BG-24 entry with neither bytes nor address is a reference by number alone and is in neither list, where it belongs. See [the guide](guides/attachments.md).
| ~~**The Peppol documents that are not invoices**~~ | ✅ **done, September 2026 — all nine transactions.** Order, Order Change, Order Cancellation, Order Response (simple, advanced and agreement), Despatch Advice, Invoice Response and Message Level Response: read, written, round-tripped element for element, and judged by the OASIS schema and Peppol's own rules against the entire published corpus. One element in the lot is unmapped — `cac:Person` on a despatch advice's carrier — kept verbatim and reported. Four models carry them: `LifecycleStatusMessage` for the two responses (shared with the French CDAR messages), `Order` for the order and its change, `OrderResponse` for the three response profiles, `DespatchAdvice` and `OrderCancellation` for their own. Three item types, because an invoice's item is what is being charged for, a despatched item is a thing in a box and an ordered item is a thing in a catalogue. See [the standard page](standards/peppol-post-award.md). |
| ~~**Reading a tax data document**~~ | ✅ **done, September 2026.** `PeppolTaxDataReader`, for Slovakia, ViDA and a jurisdiction this library does not carry — the envelope is the same everywhere, and losing the code-list checking is reported rather than hidden. The reported document goes to the **UBL invoice reader** after three element renames, so a term that reader maps is a term a tax authority gets back rather than one somebody has to remember to add twice. Doing it made the projection's shape visible from the outside for the first time: a tax data document carries **no supplier name**, only their VAT identifier and country. |
| ~~**The cases their corpora exercise and ours did not**~~ | ✅ **done, September 2026.**  Reading the neighbours' `test/resources` folders cost an hour and found two defects — every VAT category but standard was unwritten and untested, and two bank accounts on one invoice produced a shape no schema accepts. Both are fixed. ✅ **`XRECHNUNG` and the hostile containers are done, September 2026.** Registering XRECHNUNG found a defect rather than a gap: the conformance level fell through to `EN 16931` for anything unrecognised, so a hybrid XRechnung was stamped with a profile it is not written against — harmless while the metadata was an orphan, load-bearing now that it is the document's own. A profile with no published level is refused rather than guessed. The hostile corpus already covered the broken containers and now asks each one what its metadata claims as well as what it carries, and **XMP as attributes** — which Adobe's own tooling writes, and which used to read as *says nothing* — is read. ✅ **Sub-invoice lines are done too, September 2026 — and the entry above was wrong about them.** Factur-X EXTENDED does not *nest* sub-lines: it keeps the lines flat and expresses the hierarchy by reference, with `ram:ParentLineID` naming the parent and `ram:LineStatusReasonCode` saying whether a line is a `GROUP` heading, a `DETAIL` or `INFORMATION`. So the reader was already getting every line; what it lost was the structure, and with it the arithmetic — a group heading's amount is the sum of its children, so adding every line up double-counts. All three terms are now read and written. The **`EREPORTING` profile** turned out not to apply: `urn.cpro.gouv.fr:1p0:ereporting` is a *business process* identifier on an invoice reported inside a flux 10 document, not a conformance level a hybrid PDF can claim. The neighbours list it beside MINIMUM and EXTENDED because their model has one enumeration for both; this library has the flux 10 document and its own model, so there is nothing to register. **That closes this entry.** See [prior art](prior-art.md). |
| ~~**Anchor extension data in CII**~~ | ✅ **done, September 2026.** `CiiInvoiceWriter` and `CdarWriter` route through `AnchoredDocument`, which is where the anchoring lives now rather than in a third copy of it — Order-X moved onto it too. CII needed something UBL did not: a model node is not always one element. A CII invoice fills itself from the document context, the exchanged document and three header sections, and `ram:ID` appears in most of them, so the sibling alone would have put an extension after the first `ram:ID` written anywhere in the node. `ExtensionElement.ParentName` says which element it sat in, and the two halves together are a precise address. UBL keeps its own writer, which reaches the same rule structurally — every UBL model node is exactly one element, so "same parent" is "a direct child of the node's element" — and that is documented on both sides rather than left to be rediscovered. Proved by dropping the parent from the anchor and watching both tests fail. |
| **Order-X** | ✅ **the order and the order change are done, September 2026.** `International.EInvoicing.OrderX`: the Cross Industry Order, which is CII but not the Cross Industry Invoice — a different UN/CEFACT message on version 128 of the same data types, so every element in it is a different name. Read, written, round-tripped element for element *and in sequence*, and judged by FNFE-MPE's own COMFORT schema and its 124 assertions against the one document they publish. The schema found a defect the rules did not: `TaxTotalAmount` **requires** `@currencyID` where every other amount forbids it. Twenty terms of model work came first, and the census found four losses — a gross price with two per-unit allowances, a delivery event stating both a preferred date and an acceptable window, a line whose only delivery term was a date, and an item classification's name, which this library had been dropping in three item types and both syntaxes since the beginning. ✅ **The order response (type code 231) is done too, September 2026.** It fills `OrderResponse` rather than `Order`, and FNFE-MPE publishes no reference response — so the fixture is *their* order with the three things a response changes: the type code, a status on the document, and a status and agreed quantity on each line. The content is theirs; only the answer is ours. That is weaker than a published example and is said plainly in the test, but it does establish that reader and writer are inverse over a realistic document and that what comes out satisfies the publisher's own schema and their 124 assertions. The three writers now share `OrderXCommon`, since an order and a response differ in what they say and not in how a party or a price is written. |
| ~~**ZUGFeRD 1.x, for reading**~~ | ✅ **done, September 2026.** `International.EInvoicing.Zugferd1`, reading only — writing it will never be worth doing. It was indeed a mapping job with no ambiguity: the vocabulary is the CII one, and most of what separates 1.0 from D22B is two renames (`ram:ApplicablePercent` for the tax rate, `ram:ID` for a referenced document's identifier) and longer section names. What made it worth doing properly is the corpus: mustangproject carries four reference documents, FeRD's schema and rule set, **and its own ZUGFeRD 2 conversion of one of them**, so the migration forward is judged against somebody else's answer rather than against itself — same totals, same parties, same lines, first run. The one thing a migration cannot be given: what the document now claims to conform to. Their converter rewrites it to EN 16931 silently; this library reports what the 2013 document said and leaves the claim to the caller. |
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
| **Croatia** | Peppol BIS Billing, CIUS-HR 2025 + extension, OIB on both parties | ✅ the invoice, `CroatianEInvoicing` — all 74 published assertions satisfied; the seal and the fiscalisation reporting are not ours to do |
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
| **Slovakia** | ✅ Peppol BIS + the tax data document, `SlovakEInvoicing` | none: no publisher's rule for the IČO check digit | B2B from **1 January 2027** |
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

**Croatia cost more than the tier, but less than it looked.** From the outside it looked like the cheapest
kind of country — Peppol, EN 16931, a national CIUS — and the CIUS is now carried, with all 74 of its
assertions satisfied by an invoice this library writes. What it added on top was three terms EN 16931 does not
define, written by a country write step rather than pushed into the canonical model. What stays outside is
larger:
*Fiskalizacija 2.0* requires three things per invoice, and two of them are not documents: an **advanced
electronic seal** produced with a certificate the invoicing system holds, and **two fiscalisation reports**,
one from each party, to the tax administration. This library signs nothing and performs no network I/O, so
what it can do is the third thing — a valid CIUS-HR invoice carrying both OIBs, the operator who issued it and
the time it was issued — which [`CroatianEInvoicing`](standards/country-hr.md) now does. The rest belongs to
the caller, and the signature question it raises is the same one Italy and Spain raise, still open below.

**Slovakia is done**, and it turned out not to be a CIUS country at all. There is no Slovak CIUS published;
what is Slovak is the **tax data document** each party's service provider sends to the financial
administration within fifteen minutes of the invoice. The transmission is transport and out of scope, but the
document is not, and OpenPeppol publishes 88 assertions that judge it —
`phive-rules-peppol-taxdata`, which holds the same for the UAE, Oman and **ViDA** beside it. A document
`.Countries.Slovakia` writes satisfies all 88. Two things are worth carrying forward: the reported document is
a *projection* of the invoice, so an invoice you can send is not a report you can send; and no schema is
published beside the rules, so the element order is evidence rather than proof. Reading one back is a
receiver's job and is not there yet.

A pattern that looked like a wall turned out to be a fetch list, twice. NLCIUS, HR-FISK 2.0 and the Slovak
CIUS were all recorded as "blocked on an identifier no artefact we hold publishes". For NLCIUS that was wrong:
the identifier is in the Dutch rule set, and once the national rule modules were fetched it fell out
immediately — along with Romania's, Serbia's and Portugal's. **It was wrong for Croatia too**: the same
aggregator carries `phive-rules-eracun`, whose compiled XSLT holds the CIUS-HR identifier and all 74 of its
assertions. One line in the fetch script, and a country that had been called expensive was mostly done. The
lesson has now paid for itself twice: before declaring a fact unobtainable, check whether it is merely
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

**The holes come before the next country.** Twenty countries is enough breadth to have learned what is
missing in the middle, and the [comparison with the alternatives](comparison.md) named it: schema validation
that every other library has, and the Peppol documents that are not invoices. A twenty-first country makes
neither of those less true, and both of them make every country after it cheaper. So:

0. **Close the gaps** — in this order, and before any new country:
   1. ~~**XSD schema validation.**~~ ✅ done for **both syntaxes** — the OASIS UBL 2.1 and UN/CEFACT D22B
      schemas, embedded and offline. Between them they found fifteen EN 16931 terms that were read by nothing
      and written by nothing, and three shape defects of our own.
   1a. ~~**The terms the two syntaxes lost**~~ ✅ done — BT-7, BT-15, BT-16, BT-17, BT-18, BT-71, BT-89,
      BT-91, BT-111 and BT-128, plus an attachment written twice and a project reference the schema refused.
      Both official corpora now round-trip with their shape intact and nothing unmapped.
   2. ~~**The Peppol documents that are not invoices**~~ ✅ done — all nine transactions.
   3. ~~**The Factur-X container checks that are ours**~~ ✅ done for incoming documents — and it found that
      **our own** container carried PDFsharp's metadata rather than the Factur-X block. Fixed with it: the
      block goes in after the save, as a PDF incremental update.
   3a. ~~**Getting the readable copy and the supporting documents out**~~ ✅ done — `Rendition`,
      `SupportingDocuments` and `SupportingDocumentLinks`, named apart so that taking one for another is a
      compile error rather than a misunderstanding.
   4. ~~**Reading a tax data document**~~ ✅ done — the reporting documents now have the parity the
      invoices have.
   5. ~~**The hostile corpus for PDF containers**~~ ✅ done, and it earned its keep immediately: eight of
      fifteen hostile PDFs came out of the reader as an exception rather than as "no invoice here", through
      `EInvoicing.Read` and into the caller.
   6. **Order-X**, once the Peppol order family has settled the shape.

1. **EN 16931-1:2026 and PINT** — both are multipliers, both change what the countries below cost, and both
   are cheaper now than after another dozen profiles are built on the 2017 model.
2. ~~**Croatia and Slovakia**~~ — done, and neither was the country it looked like: Croatia's CIUS was
   unfetched rather than unpublished, and Slovakia has no CIUS at all, only a tax data document.
3. **Romania and Italy** — the two European formats with the largest installed base that we do not read.
   Italy needs the signature decision first.
4. **A shared reporting model** — half done, and the half that was done was cheap. The Peppol tax data
   document is one document with a jurisdiction attached: Slovakia and **ViDA** are carried by one writer in
   `.Peppol`, differing by a namespace, an identifier and one assertion out of eighty-eight. The Gulf ones are
   a second dialect and wait on the Emirati and Omani invoice models. The French flux 10 is a third vocabulary
   entirely, and merging it with these would be forcing a resemblance that is not there. Hungary and Greece go
   on top of whichever shape they turn out to share.
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

**Sending documents** — no AS4 client, no access point API, no Chorus Pro connector. **Producing the PDF a
human reads** — neither generating a PDF/A-3 from an ordinary PDF nor rendering an invoice for the eye. Both
are stated in the [README](https://github.com/Epic-Dice-Studio/International.EInvoicing/blob/main/README.md)
and neither is a maybe: the library performs no network I/O at all, which is what lets it run in a browser and
makes it auditable, and it writes no PDF, which is what keeps a PDF engine out of the dependency list of every
consumer.

Three more, each of which a competitor sells and each declined for a reason rather than for lack of time:

- **Full PDF/A conformance checking.** It is a specification of its own with a reference implementation —
  veraPDF — that is better at it than we would be. What *is* ours is whether the Factur-X container says
  about itself what the XML inside it says; that is in the table above.
- **Visualisation.** The published stylesheets are XSLT 2.0, and hosting a general XSLT 2.0 processor is the
  exact dependency this library exists to avoid: its Schematron engine runs the *rules* natively precisely so
  that no Saxon-through-IKVM is needed. Rendering is a different problem, and the answer to it is a different
  tool.
- **A REST server or container image.** The `einvoice` CLI covers the scriptable case, and everything else is
  a deployment shape rather than a library.
