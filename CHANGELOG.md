# Changelog

All notable changes to this project are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the project adheres to
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

- **New Zealand**, which shares Australia's PINT specialisation — one Peppol authority across the Tasman, so
  the document is the same and only the identifier differs. The NZBN is issued as a GS1 location number,
  which is why Peppol routes it under scheme 0088 rather than a national one; it is checked before it is
  written, and measured against Peppol's own `PEPPOL-COMMON-R040`.
- **Australia**, the first country here on PINT rather than BIS Billing — which is the point of the package,
  since the two disagree about the profile identifier *and* the business process, and an invoice with one
  right and the other wrong looks correct. `AustralianEInvoicing` declares both, in AUD, and checks the ABN
  before writing it in scheme 0151: all eleven digits are weighted and the sum must divide by 89, so a
  transposition anywhere is caught. Measured against Peppol's own `PEPPOL-COMMON-R050`, and against the ABN
  the Australian Taxation Office publishes in its developer documentation — an algorithm checked only
  against numbers it generated itself proves nothing.
- **A rule set no longer judges a document it does not govern.** `AddEn16931Rules()` registered the EN 16931
  rules for *every* UBL and CII document, which was invisible while everything this library wrote was an
  EN 16931 invoice and became wrong the moment PINT arrived — its whole purpose is tax systems EN 16931 was
  never written for. Factur-X MINIMUM and BASIC WL were already affected: their own specification says they
  are not EN 16931 invoices. Such a document is now reported as **not checked** rather than failed, which is
  the difference between a caller knowing they have no coverage and believing they have a broken invoice.
  Asking for the rules explicitly still works.
- **Peppol PINT.** The package knew BIS Billing 3.0 and nothing else, so every jurisdiction that adopted
  Peppol outside Europe — the UAE, Malaysia, Singapore, Japan, Australia and New Zealand, Oman — *looked*
  covered and was not. `PeppolPintProfiles` now carries the common core and every published specialisation,
  each identifier read out of the artefact for its jurisdiction rather than transcribed, with a test that
  fails the build if one stops appearing there.
- `PeppolBusinessProcess.PintBilling` and `ForPeppolPint()`, because the two families disagree about BT-23:
  BIS Billing numbers its processes and PINT does not, so an invoice carrying the other family's identifier
  is wrong in a way that looks right.
- `build/fetch-specs.sh pint` fetches the PINT artefacts. They do not yet *run*: OpenPEPPOL publishes them as
  pre-compiled XSLT and this library's engine executes Schematron, so a PINT document is read and mapped with
  its jurisdiction rules reported as not run rather than silently skipped.
- **Croatia**, whose mandate has been live since 1 January 2026. `CroatianEInvoicing` writes the OIB of both
  parties — which EN 16931 never asks for and Croatia always does — checked against ISO/IEC 7064 MOD 11,10
  before it is written, with the scheme on the electronic address rather than on the registration identifier,
  where `BR-CL-11` refuses an EAS code. It deliberately stops there: the advanced electronic seal and the two
  fiscalisation reports are a signature and a transport, and HR-FISK 2.0's own identifier is published
  nowhere this repository can read. The roadmap now says so — Croatia had been listed as a cheap Tier 1
  country, and it is not one.
- `CheckDigit.SatisfiesIso7064Mod11To10`, checked against the worked example the Croatian tax administration
  publishes rather than only against itself.
- **The playground is now a country-first tool rather than a demonstration of one invoice.** Choose a
  country and everything after it follows: the profiles it exchanges, the currency, the identifier schemes,
  the business process, the rules that apply — and the one thing about that country that surprises people.
  It builds invoices, credit notes, French lifecycle statuses and flux 10 e-reporting transmissions, shows
  the C# that would produce each, validates what it produced, and says which rule sets are in the build and
  which are fetched. Reading and validating now go through the library's own facade rather than a
  reimplementation, so the site shows the API it documents.
- The published site links to it: the playground is the first entry in the navigation and on the front page.
- `International.EInvoicing.Playground.Tests` compiles the country descriptions the site runs on and puts
  every country and profile combination in front of the rules on every commit. It found three the site would
  have offered with a defect: an invoice with no seller VAT identifier fails BR-S-02 and BR-CO-26, and a
  German one with no seller contact fails BR-DE-2.
- **The Netherlands and Iceland**, each for one fatal national rule that rejects an otherwise perfect
  invoice: `NL-R-003` and `NL-R-005` require a KvK or OIN scheme on both parties' legal entity identifiers,
  and `IS-R-002` and `IS-R-004` require scheme 0196 on the kennitala. `DutchEInvoicing` and
  `IcelandicEInvoicing` put them where the rules look, and a test strips the scheme back out of a document
  the library wrote to confirm those really are the rules that reject it. NLCIUS is deliberately not carried
  — its published specification identifier is not in any artefact this repository holds.
- **Norway, Sweden and Denmark.** Three more country packages, each with the shortcut object its neighbours
  have: `NorwegianEInvoicing` declaring EHF 3.0, `SwedishEInvoicing` and `DanishEInvoicing` declaring Peppol
  BIS Billing, each in its own currency and with the business process the network requires. Their national
  validation rules need nothing extra fetched — they travel inside the Peppol rule set.
- Their legal identifiers come with them: the Norwegian organisasjonsnummer (modulo 11), the Swedish
  organisationsnummer (Luhn) and the Danish CVR number, each written in the scheme Peppol reserves for it and
  checked before it is written. The checks are not trusted on their own — a test hands every number the
  library accepts, and a set it refuses, to Peppol's own rule for that scheme and fails on any disagreement.
- `DkPaymentMeans`, because `DK-R-005` refuses payment means code 30 — plain credit transfer, perfectly valid
  EN 16931 — between two Danish parties. The allowed codes are read out of the rule itself.
- `CheckDigit.SatisfiesMod11` and `CheckDigit.SatisfiesGs1`, the two schemes the Nordic and GS1 identifiers
  are built on.
- **The edition of EN 16931 a document declares is now something the library names.** CEN published
  EN 16931-1:2026 in May 2026 and withdrew the 2017 edition this library implements. `En16931Edition` reads
  the edition out of the specification identifier, and a document declaring one we do not implement is
  reported as `EIV1044 UnsupportedEdition` — an EN 16931 invoice of an edition we do not carry — rather than
  as an unknown profile. It still parses, everything the two editions share is read normally, the rest is
  kept in extension data, and the validation report names the edition its rules are for. See
  [ADR 0013](docs/adr/0013-en16931-editions.md).
- **EN 16931 artefacts updated to 1.3.16** (April 2026), from 1.3.13 (October 2024) — two years of published
  corrections, including BR-CO-25 on credit notes, BR-CL-01's invoice and credit-note type codes, missing CII
  checks on BT-81 to BT-83, BT-86, BT-123 and BT-128, and the allowance and charge fixes below. The EAS code
  list moved with them: six schemes added (0154, 0158, 0240, 0242, 0244, 0245, 0246, 0248) and 9901 removed.
- Running the new artefacts found a defect in the XPath engine: **a step's predicate was applied to the whole
  flattened result instead of to each node the step started from**. `a/b[1]` means the first `b` of *each*
  `a`; ours meant the first `b` in the document. BR-CO-11 and BR-CO-12 sum `ActualAmount[1]` across every
  document-level allowance, so an invoice with two of them was rejected for arithmetic it had got right.
  Fixed, with the sequence-filtering case (`$digits[$i]`, `tokenize(...)[7]`) kept distinct, since the two
  wear the same brackets and mean different things.
- **One country, one object.** `FrenchEInvoicing`, `GermanEInvoicing` and `BelgianEInvoicing` hold what
  their country expects, so a caller who invoices in one place does not have to learn which profile, which
  business process and which rule sets that place wants. France reads all four of its documents through one
  call — invoice, credit note, lifecycle status and the namespace-less *flux 10* report — and says which
  arrived. Germany parses the Leitweg-ID before writing it, so a routing identifier that would be rejected on
  arrival is rejected at the source. Belgium checks the KBO/BCE number modulo 97 and writes it in the scheme
  Peppol reserves for it. Each exposes `.Library`, so the shortcut is a shortcut and not a fence.
- **Developer experience.** One vocabulary for assembling the library, whether or not there is a container:
  `services.AddEInvoicing(e => e.AddDefaults().AddFrance())` and `EInvoicing.Create(e => e.AddDefaults())`
  take the same calls. Each package method now registers what it needs, so there is no second list of
  `Add…Services()` calls to remember, and `EInvoicing` itself is injectable.
- Rule sets are registered rather than fixed: `AddEn16931Rules()`, `AddXRechnungRules()`,
  `AddRulesFromFile(...)` for artefacts that may not be redistributed, and `IDocumentRuleSet` for rules of
  your own in C#. Validation runs every one that applies and names those that did not.
- `EInvoiceBuilder.From(...)` and `.To(...)` — an invoice goes from a supplier to a customer, and now reads
  that way — with a short form taking a name and a VAT number.
- `WithComputedVatBreakdown()` and `WithComputedTotals()`: the VAT grouped from the lines, and BT-106 to
  BT-115 derived from them. Opt-in, because computing totals behind a caller's back would replace what they
  meant to send with a guess.
- `Write(invoice)` picks the syntax the declared profile is written in, rather than asking for it twice.
- `ReadFile`, `ReadAsync` and `ReadFileAsync`; `TryGetInvoice`, `RequireInvoice`, `Errors`, `Warnings` and
  deconstruction on a read result; `Errors`, `Warnings`, `NotRun`, `Failed(rule)` and `EnsureConforming()`
  on a validation report.
- Peppol BIS Billing 3.0 validation, measured against Peppol's own unit corpus: 227 of 227 UBL cases and
  127 of 127 CII cases agree with the published expected results. The artefacts declare no licence and are
  not shipped — `build/fetch-specs.sh peppol` fetches them, and they load like any other rule set.
- French e-reporting — *flux 10*: the transactions and payments transmissions, with a model, a reader, a
  writer and builders that add the totals up from the VAT split rather than asking for them twice. Measured
  against the DGFiP's published flux 10 rules, which is the only measurement available since no sample
  transmissions are published.
- French lifecycle statuses measured against the DGFiP's own rules: every status, to a trading partner and to
  the public portal, and the eleven published sample messages, checked on each build. The artefacts are
  fetched (`build/fetch-specs.sh france`), not redistributed.
- `DocumentStatusDetail` and `DocumentStatusCharacteristic` on the CDAR model: the reason behind a status, the
  action requested, and the values at issue, read and written rather than kept as extension data.
- `FrCdar.IssuedByBuyer` / `IssuedBySeller`, `FrStatusReason`, `FrRequestedAction`, `FrStatusValueType`, and
  `Collected(FrCollectedAmount)` — what the French rules require of a status, asked for in the builder.
- The French invoice profile `urn:cen.eu:en16931:2017#conformant#urn.cpro.gouv.fr:1p0:extended-ctc-fr`.
- Rule sets may define their own functions in XSLT; the engine runs those definitions rather than
  reimplementing them, which is how the twenty French `custom:` functions work.
- Repository foundations: multi-targeted build (`net8.0`, `net10.0`), central package management,
  deterministic packaging, MinVer versioning from git tags.
- `SecureXml` and `DocumentLimits`: hardened XML reading for untrusted documents.
- Documentation set: standards references, recipes, diagnostic catalogue, architecture decisions.
- CI: build and test matrix, packaging, documentation gates, upstream specification monitoring.

### Changed
- `SecureXml.CreateReader`, `FrCdar.Collected` and `EInvoicing.Create` are spelled out as explicit overloads
  rather than sharing optional parameters. Adding a parameter to a published overload later is a break that
  compiles cleanly and fails in someone else's process; the API analyzer found all three.
- The French lifecycle builder now reads as the sentence it is —
  `FrCdar.FromBuyer(...).SentBy(...).ToSeller(...).About(...).Approved()`. A lifecycle message has three
  parties and it was too easy to fill in the wrong one, so where you start fixes who reports the status,
  the destination fixes the profile, and reporting a status from the wrong kind of party is refused with the
  entry point to use instead. Replaces `ToPartner(...)`/`ToPublicPortal()` as entry points and the
  `IssuedBy*` methods.

### Fixed
- `XmlReaderSettings.Async` was set on every reader this library created while nothing ever called
  `ReadAsync`, which selects the asynchronous-capable path inside `XmlReader` and pays for a capability
  never used.
- The note subject code (BT-21) was lost in UBL, which has no element for it: the code goes inside the note
  as `#AAB#…`, and the writer dropped it while the reader kept the prefix as part of the text. Three of the
  French mandatory mentions are identified by nothing else.
- UBL party names were mapped to the wrong business terms: the writer put BT-27, the legal name, into both
  `cac:PartyName/cbc:Name` (BT-28, the trading name) and `cac:PartyLegalEntity/cbc:RegistrationName`, and the
  reader then left the second unmapped — so a document round-tripped through this library gained two elements
  of extension data and a diagnostic apiece. Found by writing the sample.
- A control character in a caller's text no longer stops a document being written. XML cannot carry those
  characters at all, so they are dropped and everything else — accents, symbols, emoji — is written as it
  was. Found by reading what the neighbouring libraries have had to answer; see `docs/prior-art.md`.
- A Schematron rule context is a match pattern, not a path from the document root. Reading it as a path
  silently matched nothing for every relative context, leaving rules such as BR-29, BR-30, BR-CL-13 and the
  whole French lifecycle set dormant.
- The XPath range operator (`0 to $n`), `reverse`, and `xsl:choose` in a rule set's own functions.
- `castable as` now asks about the type it was given rather than always about a number, and `substring` is
  the window XPath defines rather than an offset and a count — `substring($v, 0, $n)` takes the first
  `n - 1` characters, which is how the Peppol check-digit functions are written.
- Ordering comparisons on dates (`xs:date(a) >= xs:date(b)`), the `text()` node test, and the `replace`,
  `translate`, `xs:string` and `string-to-codepoints` functions, all of which the published rule sets use.
- A validation message now names the rule that failed even when the rule set puts its code in the message
  rather than in an attribute, as the French e-reporting artefacts do.
