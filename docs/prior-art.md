# Prior art, and how to mine it

Mature e-invoicing libraries have already met the documents that break implementations. Their test corpora
and, above all, their issue trackers are a map of the edge cases this library has not hit yet — cheaper to
read than to rediscover in production.

This page is the standing task: **before declaring a format or a country done, spend an hour in the trackers
below.** Record what you find here, as a row, whether or not it turns into work.

For what those projects *do* rather than what breaks in them — and for the gaps that comparison opens — see
[how this library compares](comparison.md).

There is a third thing to mine, and it is the cheapest of the three: **their test corpora**. A mature
library's `test/resources` folder is a list of the documents that broke it — sub-invoice lines, gross prices,
insurance without VAT, two bank accounts on one invoice — and reading the file names takes minutes. What that
found, on 31 August 2026, is in *What their corpora exercise* below.

## What their corpora exercise, and what it found here

Read on **31 August 2026** from `demodata/`, `documentation/.../Samples/` and `src/test/resources/` in
ZUGFeRD-csharp, mustangproject and Securibox.FacturX. Their sample sets are business cases, not synthetic
XML: partial invoices, travel expenses, rent, public transport, a physiotherapist's exempt invoice, insurance
billed with insurance tax rather than VAT, gross prices, foreign currency, a negative amount due, SEPA
pre-notification, two bank accounts, sub-invoice lines, and a shelf of malformed PDFs.

Two of those turned into defects here within the hour, and both are the kind no rule set catches:

| What their corpus exercised | What it found here | Outcome |
|---|---|---|
| Every VAT category, not only standard rate (`Physiotherapeut`, `Innergemeinschaftliche_Lieferungen`, `Haftpflichtversicherung`, `valid_with_VAT_O`) | **Every invoice this library wrote in its own tests was standard-rated** — one category out of nine. Writing one per category found that *not subject to VAT* could not be produced at all: EN 16931 forbids a rate there rather than requiring zero, and both `WithVat` and the computed breakdown always wrote one. | Fixed: `VatCategoryCodes.ForbidsRate`, a rate-less `WithVat(category)`, and a breakdown that leaves the rate unset. Nine categories are now written and judged in both syntaxes. |
| `multiple-payment-means.cii.xml` — two accounts on one invoice | Both writers put two accounts inside **one** payment-means block, and both readers took only the first. EN 16931's own examples (`ubl-tc434-example1`, `guide-example1`) repeat the whole block, one account each. A document no schema accepts, that no Schematron rule complains about — and an account silently dropped on the way in. | Fixed in both writers and both readers, pinned by `PaymentMeansTests` including the official example. |

The rest of what their corpora carry, checked and **not** yet ours:

| Case | State here |
|---|---|
| **Sub-invoice lines** (`SubInvoiceLine`, EXTENDED) | Not modelled. Factur-X EXTENDED nests lines; EN 16931 does not. On the roadmap. |
| **Factur-X `XRECHNUNG` and `EREPORTING` profiles** | Not registered — five profiles are, these two are not. Both appear in the ZUGFeRD sample set. |
| **PDF containers that fight back** (password-protected, no embedded file, too small to be a PDF, XMP stored as attributes, XMP that does not parse) | The hostile corpus covers XML, not PDFs. The attachment reader has never been pointed at any of these. |
| **ZUGFeRD 1.x documents** | Not read. On the roadmap, low. |
| **Order-X** | Not carried. On the roadmap. |
| Gross prices, foreign currency, negative amount due, SEPA pre-notification, partial invoices, prepaid amounts | All expressible today; none pinned by a test of ours. Worth a corpus of our own rather than a rule change. |

## Where to look

| Ecosystem | Project | Worth mining for |
|---|---|---|
| Java | [ZUGFeRD/mustangproject](https://github.com/ZUGFeRD/mustangproject) | The reference for Factur-X and ZUGFeRD. Its issues are a decade of real-world CII edge cases: profile quirks, PDF containers, rounding disputes. |
| Java | [phax/phive-rules](https://github.com/phax/phive-rules) | Which rule set version applies to which document, across many countries. Already used here to verify the CDAR structure. |
| Java | [phax/ph-schematron](https://github.com/phax/ph-schematron) | How a mature Schematron engine handles the awkward parts. Direct comparison for our XPath engine. |
| Java | [itplr-kosit/validator](https://github.com/itplr-kosit/validator) | The German reference validator. Its issues show where implementations and the norm disagree. |
| PHP | [horstoeko/zugferd](https://github.com/horstoeko/zugferd) | Profile-driven builder ergonomics, and a long tail of reported document oddities. |
| Python | [akretion/factur-x](https://github.com/akretion/factur-x), [pretix/drafthorse](https://github.com/pretix/drafthorse) | Compact implementations; their issues surface PDF and encoding problems. |
| .NET | [stephanstapel/ZUGFeRD-csharp](https://github.com/stephanstapel/ZUGFeRD-csharp) | The closest neighbour. What its users ask for is what ours will ask for. |
| .NET | [kestlerio/ZUGFeRD-csharp-extended](https://github.com/kestlerio/ZUGFeRD-csharp-extended) | A fork, so its diff shows what the original was missing. |

## What to look for

1. **Documents that broke them.** Attachments on issues are real invoices from real senders, which is exactly
   the corpus no specification provides. Check the licence before committing anything.
2. **Rules everyone gets wrong.** A rule discussed repeatedly across projects is one where the norm is
   ambiguous, not where implementers are careless. Those deserve a test and a note in the standards page.
3. **Features requested repeatedly.** A gap several communities ask for is a gap ours will have too.
4. **Where they disagree with the reference validator.** Those threads usually end with the correct reading
   of the norm, argued out by people who had to ship.
5. **What they refuse to do.** Scope boundaries other projects settled on are evidence about ours.

## Findings

Add a row when you mine something. An entry that led to no change is still worth recording — it says the
ground was covered.

| Date | Source | Finding | What we did |
|---|---|---|---|
| 2026-08-30 | phax/phive-rules | Carries the French `BR-FR-CDV` Schematron, the flux 10 e-reporting rules and the DGFiP lifecycle test files, which the DGFiP does not publish in a redistributable form. | Verified the CDAR structure and the French status codes against them, and built the e-reporting support on the flux 10 rules. Nothing redistributed; see `docs/standards/country-fr.md`. |
| 2026-08-30 | stephanstapel/ZUGFeRD-csharp [#772](https://github.com/stephanstapel/ZUGFeRD-csharp/issues/772) | A control character in a description makes writing fail with "hexadecimal value 0x07, is an invalid character" — a message naming neither the field nor anything a caller can act on. Their answer was to trim those characters. | **Was a defect here too.** `XmlCharacters.Sanitize` now drops what XML cannot carry, in every writer; accents, symbols and characters outside the basic plane are untouched. Tests in `HostileTextTests`. |
| 2026-08-30 | stephanstapel/ZUGFeRD-csharp [#956](https://github.com/stephanstapel/ZUGFeRD-csharp/issues/956), [#398](https://github.com/stephanstapel/ZUGFeRD-csharp/issues/398) | Unit prices fixed to two decimals, which breaks fuel invoices and per-thousand rates. Reported twice, years apart. | Not a defect here — amounts are written with the precision they were given — but it was assumed rather than pinned. Now pinned by `PrecisionTests`. |
| 2026-08-30 | phax/ph-schematron [#141](https://github.com/phax/ph-schematron/issues/141), [#59](https://github.com/phax/ph-schematron/issues/59) | Two recurring failure modes in Schematron engines: concurrent use of one loaded rule set, and rules calling functions the rule set declares in XSLT. | The second was implemented this week for the French and Peppol artefacts. The first is now pinned by `ConcurrencyTests`: four hundred parallel validations of two documents, each one still answering for itself. |
| 2026-08-30 | itplr-kosit/validator [#172](https://github.com/itplr-kosit/validator/issues/172) | Warnings pushing a document into an invalid state, blocking invoices that are conformant. | Not a defect here: `ValidationReport.IsValid` counts errors and fatals only, and `IsComplete` says separately what did not run. Already covered by tests. |
| 2026-08-30 | ZUGFeRD/mustangproject [#640](https://github.com/ZUGFeRD/mustangproject/issues/640), [#660](https://github.com/ZUGFeRD/mustangproject/issues/660), [#1014](https://github.com/ZUGFeRD/mustangproject/issues/1014); ZUGFeRD-csharp [#672](https://github.com/stephanstapel/ZUGFeRD-csharp/issues/672) | Document-level allowances and charges, especially alongside several VAT rates, are where generated documents most often stop validating. Four separate long threads across two ecosystems. | Noted as the first place to look when the totals rules start failing. Our own writers are measured against the official corpora, which cover it; a hostile case of our own is worth adding when document-level allowances get a guide. |
| 2026-08-30 | akretion/factur-x [#41](https://github.com/akretion/factur-x/issues/41), [#55](https://github.com/akretion/factur-x/issues/55) | Profile and version detection guessing wrong — a 2.0.1 document read as 1.0, and a profile inferred by scanning the document. | Evidence for the choice already made: profiles here are resolved from the declared identifier (BT-24), never guessed, and an unknown one is reported rather than approximated. |
| 2026-08-30 | itplr-kosit/validator [#88](https://github.com/itplr-kosit/validator/issues/88) | Encoding: what a document declares and what it actually is. The single most-discussed issue in that tracker. | Ours is handled by reading through `XmlReader` rather than by decoding text ourselves, so the declaration is honoured and a mismatch is a parse diagnostic. Worth a hostile-corpus document when that corpus grows. |
| 2026-08-30 | ZUGFeRD-csharp [#584](https://github.com/stephanstapel/ZUGFeRD-csharp/issues/584); mustangproject [#993](https://github.com/ZUGFeRD/mustangproject/issues/993) | German *Skonto* — an early-payment discount encoded as structured text inside the free-text payment terms (BT-20). Repeatedly got wrong, in both ecosystems. | **Closed.** `DeSkonto` reads and writes the statements, and `WithSkonto` puts them on an invoice. The expression is the one in the shipped `common.sch`, and the tests compare against that file rather than against a copy of it — so a statement this library reads is one `BR-DE-18` accepts. |
| 2026-08-30 | mustangproject [#12](https://github.com/ZUGFeRD/mustangproject/issues/12), [#139](https://github.com/ZUGFeRD/mustangproject/issues/139); ZUGFeRD-csharp [#834](https://github.com/stephanstapel/ZUGFeRD-csharp/issues/834); akretion [#27](https://github.com/akretion/factur-x/issues/27) | PDF/A-3 conformance and XMP metadata disagreeing with the document — validators contradicting each other about the same file. | Bounded by scope: this library attaches CII to a PDF a caller already has rather than producing PDF/A itself. Stated in `docs/standards/facturx.md`; the neighbours' experience is evidence that the boundary is the right one. |
| 2026-09-03 | OpenPEPPOL/poacc-upgrade-3 | The published `.sch` for every post-award transaction `include`s a `target/generated/T*-basic.sch` that OpenPEPPOL's build produces and does not commit — so the artefact as published carries only half its rules, and nothing says so. | **Two defects of our own, both found here.** `AddRulesFromFile` read the compiled artefact as source Schematron, found no patterns, and built a rule set that ran and judged nothing; it now recognises a stylesheet by what it is. And `*:name` — a local name in any namespace — was refused outright by the XPath engine, which is fatal because `not(@*:schemaLocation)` is the first rule in every Peppol rule set. Pinned by `WildcardNameTests`. The compiled form is what `build/fetch-specs.sh poacc` fetches, and why. |

