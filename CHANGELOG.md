# Changelog

All notable changes to this project are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the project adheres to
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

- **An invoice that is not subject to VAT could not be written at all.** EN 16931 forbids a VAT rate on
  category `O` rather than requiring zero — `BR-O-05`, `BR-O-06`, `BR-O-07` — and both `WithVat` and the
  computed breakdown always wrote one. `VatCategoryCodes.ForbidsRate` names the distinction, `WithVat(category)`
  writes a category without a rate, and the breakdown leaves it unset. Found by writing one invoice per VAT
  category and putting each in front of the official rules: **every invoice this library wrote in its own
  tests until now was standard-rated**, one category out of nine.
- **Two bank accounts on one invoice produced a document no schema accepts.** Both writers put every account
  inside a single payment-means block; UBL and CII each allow one account per block and repeat the block,
  which is what EN 16931's own examples do. Both readers, symmetrically, took only the first block and
  silently dropped the rest. Fixed in both directions, and pinned against the official example that carries
  two. No Schematron rule catches either half of this, which is the argument for the schema validation now on
  the roadmap.
- **The tax data document is Peppol's, not Slovakia's** — so it moved to `International.EInvoicing.Peppol`,
  and **ViDA came with it**. OpenPeppol publishes one per jurisdiction, and Slovakia's rule set differs from
  the EU's ViDA one by a single assertion out of eighty-eight, a namespace and an identifier:
  `PeppolTaxDataJurisdiction` is that difference, as data. One writer serves both, and both are measured
  against their own published rules. `SlovakEInvoicing.TaxDataFor` is unchanged for callers.
- **A rule set that matched nothing said the document was valid.** The jurisdictions are the same rules in
  different namespaces, so the ViDA set finds no context at all in a Slovak document — and an engine that
  reports what it found reports *nothing found*, which reads exactly like *nothing wrong*. A rule set that
  claims no node is now reported as **not run**, with the reason, so `IsComplete` is false and a caller can
  tell "judged and clean" from "never judged". Nothing else in the repository changed behaviour: every
  shipped and fetched rule set still matches what it is pointed at.
- **Slovakia, which turned out not to be a CIUS country.** The 2027 mandate has two halves, and the second
  is a document rather than a summary: a **tax data document** each party's service provider sends to the
  financial administration within fifteen minutes of the invoice, with an identifier, a structure and 88
  published assertions of its own. `PeppolTaxData`, `PeppolTaxDataWriter` and `PeppolTaxDataValidator` build it, write it and judge it; `SlovakEInvoicing.TaxDataFor` fills in what follows from the invoice and leaves the authority
  and the endpoints — the network's business — to the caller. A document this library writes satisfies all 88,
  with four negative controls proving the rules ran. The reported document is a **projection** of the invoice,
  not a copy: its rules forbid every element they do not name, so an invoice you can send is not a report you
  can send. No schema is published beside the rules, so the element order is the rules' own enumeration —
  evidence, not proof, and said out loud in `docs/standards/country-sk.md`. There is no Slovak CIUS published
  and no publisher's rule for the IČO check digit, so this library invents neither.
- **`instance of`, in the Schematron engine.** Without it the Slovak rule set did not load at all — the rules
  build the path they report a failure at by walking the ancestors and asking each one whether it is an
  element — and a rule set that fails to load judges nothing.
- **Croatia's CIUS was not missing, it was unfetched** — and finding that out cost one line. CIUS-HR 2025
  had been recorded as blocked on an identifier "published nowhere this repository can read". The publisher's
  rules are aggregated by `phive-rules` as compiled XSLT, which this library has read since Factur-X, and the
  fetch script already pulled four other national modules from it. Adding `eracun` to that list yielded the
  identifier *and* all 74 assertions from the same file. `HrProfiles.CiusHrUbl` is that identifier — one, not
  two, because CIUS-HR never travels without its extension — and `AddCroatianRulesFrom` runs the rules.
  An invoice this library writes now satisfies **all 74**. Three of them wanted `cbc:IssueTime` and the
  operator's name and OIB in `cac:SellerContact` — ordinary UBL elements that EN 16931 does not define, so the
  model has nowhere to hold them: `AddCroatianOperator` writes them into the document as it is produced, and
  the canonical model stays the norm. A test still names those three, so the day the set changes it fails.
- **`HrBusinessProcess`** — BT-23 is mandatory in Croatia and restricted to `P1`–`P12` or `P99:` and the
  buyer's own designation. The shape is checked; what the twelve mean is in a specification no artefact here
  carries, so no labels are invented for them.
- **The docs site had no API reference at all.** docfx forces `TargetFramework=net10.0` over every project in
  `src/`, and the CLI is a dotnet tool pinned to net8.0 alone, so its project references resolved against a
  framework it was never restored for: twenty errors, the metadata step dead, and every published page of API
  documentation missing. The tool is excluded from that step, and the build emits 365 API pages where it
  emitted none. The fourteen link warnings were dead links on the site, and now point at GitHub.
- **German *Skonto*, structured rather than hand-rolled.** An early-payment discount is a number your
  accounting system needs, and Germany keeps it inside BT-20's free text, where `BR-DE-18` judges it with a
  regular expression — the percentage with exactly two decimals, the keywords in capitals, no stray space, and
  a line break after the last statement. `DeSkonto` reads and writes one, `invoice.SkontoTerms()` returns what
  a document states and `invoice.WithSkonto(...)` puts them on one, keeping whatever the note already said and
  replacing any statement it already claimed. The expression is not transcribed: the tests read it out of the
  shipped `common.sch`, so a statement this library accepts is one the rule accepts, and an invoice carrying
  what it writes is put in front of the German rules in both syntaxes.
- **Seven more code lists a caller can pick from**, each read out of the shipped EN 16931 artefact and
  compared against it on every build: `CurrencyCodes` (ISO 4217), `CountryCodes` (ISO 3166-1),
  `IcdSchemeCodes` (ISO 6523, judging four different identifiers), `ItemClassificationSchemeCodes`,
  `AllowanceReasonCodes`, `ChargeReasonCodes` and `VatExemptionReasonCodes`. Reading them from the artefact
  rather than transcribing them is the point: the country list carries `XI` for Northern Ireland and `1A` for
  Kosovo, neither of which is in ISO 3166-1, and a transcribed list would reject both.
- **A document's declared encoding is honoured.** The bytes were decoded as UTF-8 whatever the document said,
  so a sender whose database is Latin-1 and whose template says UTF-8 — the single most discussed issue in the
  German validator's tracker — had `Müller` arrive as `M?ller`: a document that validates, arrives, and is
  wrong in the one field a human reads. The declaration and any byte-order mark are now obeyed, a mismatch is
  reported as [EIV5002](docs/diagnostics/EIV5002.md) with ISO-8859-1 as the fallback, and an encoding this
  library does not carry a package for is named in [EIV5003](docs/diagnostics/EIV5003.md) rather than
  silently assumed.
- **Three limits were declared and never enforced.** `MaxElementDepth`, `MaxAttachmentBytes` and
  `MaxAttachmentCount`/`MaxDocumentLines` were documented reassurance a reader relied on and nothing checked.
  Depth is now refused after loading — LINQ-to-XML survives the parse, but every consumer afterwards recurses
  — an attachment is measured *before* it is decoded, and a document carrying more than the limits allow says
  so as [EIV4004](docs/diagnostics/EIV4004.md) rather than handing back a silently truncated invoice.
- **The hostile corpus grew** to cover all of it: a mis-declared encoding, an encoding we do not decode,
  nesting a thousand deep, an oversized attachment, an empty amount.
- **Property-based tests on rounding**, and they found a real defect on the first run. Three hundred generated
  invoices per syntax — quantities to three decimals, prices to four, base quantities that are not one, so the
  multiplication lands off two decimals more often than on — written and judged by the official EN 16931
  artefact. The seed is fixed, and printed in every failure so a case found here can be pinned as its own test.
- **An amount assigned as a plain `decimal` was written to UBL with no `currencyID`.** UBL makes the attribute
  mandatory on every amount and BR-CL-03 requires an ISO 4217 code, so those documents were rejected by the
  schema before any rule ran. Both writers now fall back to the document currency, BT-5, when the field itself
  carries none. A hundred and four of the first three hundred generated invoices failed on this.
- **`International.EInvoicing.Testing`**, so an integrator can test *their* profile with our tools.
  `SampleInvoices` builds documents EN 16931 actually accepts — thirty-odd terms, which is where an afternoon
  goes; `RoundTrip.Check` proves nothing was lost, by element census rather than by text, because byte
  equality is not promised and should never be asserted; `HostileDocuments` is the corpus that defends "reading
  never throws"; and `Expect` carries the evidence into the failure message. Framework-free — the assertions
  throw, which every runner understands.
- **A truncated file was reported as "not a document this library recognises".** It is malformed XML, and
  saying otherwise sends the reader checking profile identifiers for an hour before noticing the file ends
  mid-element. The facade now tells the two apart, and `DiagnosticCodes.MalformedDocument` names the first.
- **A command-line tool**, `International.EInvoicing.Cli`. `einvoice validate` checks a file or a whole
  directory against every rule set that applies and prints which ones ran; `inspect` says what a document is
  and what reading it reported; `convert` carries it to the other syntax with the loss report; `profiles` and
  `rules` say what the build knows. Exit codes separate *rejected* from *could not run*, and a document
  declaring a specialisation of EN 16931 that only the base judged is told so, because a validator that says
  "valid" when it means "I had no rules for this" is worse than none.
- **Every allowance or charge left a duplicate of `cbc:ChargeIndicator` in extension data.** It is read as a
  flag rather than through a field, and nothing marked it as mapped — so the catch-all swept it up and the
  writer emitted it a second time. Found by running the new CLI over the Peppol examples.
- **The writers re-emitted the other syntax's extension elements.** An invoice read from UBL carries UBL
  elements the model has no field for; writing those into a CII document produced something no receiver would
  accept. They now stop at the boundary, and `Convert` reports them as the cost of the crossing.
- **The write pipeline.** `AddWriteStep` puts your own logic inside generation — numbering, house rounding, a
  signature, an element one customer demands — with ASP.NET Core's middleware shape: work before `next`, after
  it, or decline to call it. The steps are wrapped *around the writer* rather than called by the facade, so
  `library.Write`, `library.UblWriter.WriteToString` and a writer resolved out of the container all run them.
  A guarantee with a bypass is not a guarantee. See [the guide](docs/guides/hook-into-generation.md).
- **UBL ↔ CII conversion, with a loss report.** `EInvoicing.Convert` takes an invoice you built or the XML of
  one you received and returns a `ConversionResult`: the converted document, the invoice as it reads back, and
  a list of what the crossing cost. The losses are *found* rather than predicted — the result is read back and
  what it reports is recorded, along with every extension element the source carried, which is syntax-specific
  by definition and has nowhere to go in the other syntax. Converting silently is the dangerous version; the
  report is the feature. `InvoiceNodes.Descendants` and `.Extensions` walk the model for it, written out by
  hand so trimming and ahead-of-time compilation keep working.
- **Code lists a caller can pick from**, not only be judged by. `InvoiceTypeCodes` now carries both lists —
  invoices and credit notes — and `VatCategoryCodes` and `PaymentMeansCodes` join it, each read out of the
  shipped EN 16931 artefact and compared against it on every build, the way the Peppol scheme list already
  was. `VatCategoryCodes.NeedsExemptionReason` answers in one place what five rule families each say about
  their own category.
- **A credit note carrying type 420, 458, 502, 503 or 532 was read as an invoice.** The credit-note list was
  five codes short, and writing the test that compares it to the artefact is what showed it.
- **The Factur-X and Belgian rule sets are wired at last**, closing two cells the support matrix had called
  *planned* since the beginning. Both are published as compiled XSLT, which is why they sat unused; reading
  that came later. `AddFacturXRulesFrom` registers one rule set per profile — including MINIMUM and BASIC WL,
  which say in their own specification that they are not EN 16931 invoices and which therefore nothing
  judged at all before — and `AddBelgianRulesFrom` registers GLOBALUBL.BE.
- **A defect that would have had every ZUGFeRD document rejected.** Running the Factur-X rules over our own
  output showed the CII writer putting `@currencyID` on amounts that forbid it — the tax basis, the
  calculated tax, and every monetary summation total. CII states the currency only on the tax total, which
  may also be given in the accounting currency. Fixed, and every Factur-X profile now accepts what this
  library writes.
- `BeProfiles.UblBe`. GLOBALUBL.BE refuses a document declaring plain Peppol BIS: Belgium has a conformant
  profile of its own, and its identifier was in the rule set all along.
- **Italy, on the Peppol side.** The partita IVA with the check Peppol publishes for scheme 0211 — eleven
  digits, odd positions as themselves, even ones mapped through `0246813579`, the total divisible by ten —
  measured against that rule in both directions, and the full postal address `IT-R-002` to `IT-R-004`
  require. Worth knowing: Peppol's own function returns *true* for any value that does not begin `IT`, so a
  bare partita IVA is never verified by the network. This library checks it either way. FatturaPA remains a
  separate project: its own syntax, and a qualified signature this library does not produce.
- **Greece**, which asks for two things nothing else here does. `GR-R-001` makes BT-1 a **compound key of
  six segments** — supplier AFM, issue date as DD/MM/YYYY, branch, myDATA document type, series, number —
  each checked against the rest of the document, so an ordinary invoice number is refused outright.
  `GrInvoiceNumber.For` builds it and names the rule when it refuses a part. And the AFM has a checksum
  unlike any other here: the first eight digits weighted by descending powers of two, the ninth the sum
  modulo 11 modulo 10. myDATA reporting stays out of scope — it is a transmission, not a document.
- **Portugal.** CIUS-PT, whose artefact is the largest here — over two thousand assertions, since it bundles
  the EN 16931 UBL rules with its own. It requires a delivery address, which EN 16931 leaves optional. SAF-T,
  ATCUD and the QR code are separate obligations that leave the document entirely, and stay out of scope.
- **Amounts, VAT percentages and quantities are now written with at least two decimals.** A decimal's natural
  form writes `1000` for a thousand euros and `23` for a VAT rate — good numbers, poor amounts. Portugal's
  `DT-CIUS-PT-094` and a dozen neighbours reject them outright, and most implementations expect two decimals
  everywhere. More than two are kept, and a field read from a document still writes back its original text,
  so this changes only what the library produces itself.
- **Serbia.** SRBDT, the CIUS the SEF exchanges since 2023, with its conformant extension and the 134
  assertions Serbia publishes. `RSR-05` required the **tax point date code** (BT-8) — which the model carried
  and the UBL writer silently dropped, and the reader never read. UBL keeps it inside `cac:InvoicePeriod` as
  a description code, so a document may carry it with no period dates at all; both directions are fixed.
- **Romania.** CIUS-RO, the national CIUS the e-Factura mandate exchanges, with the 244 assertions Romania
  publishes on top of EN 16931 — the largest national rule set here after Germany's. The identifier is read
  from the artefact, which matters: it carries the *CIUS* version, 1.0.1, which is not the 1.0.9 of the rule
  set that checks it.
- `RoBucharestSector`, for the rule nobody expects: `BR-RO-100` is fatal, and it requires a Bucharest address
  to put the **sector** in the city name — `SECTOR1` to `SECTOR6`. Writing "Bucureşti" there, which every
  other country would want, is what fails.
- **NLCIUS, which this library had said it could not carry.** It was left out because its specification
  identifier was in no artefact the repository held — a statement about the fetch list rather than about the
  world. The identifier is in the Dutch rule set itself. `build/fetch-specs.sh national` now fetches the
  national rule modules, `NlProfiles` carries NLCIUS and its G-account extension, and `AddNlciusRulesFrom`
  runs the rules that come with them. The same source turned out to carry Romania's, Serbia's and Portugal's
  identifiers too.
- **Japan**, whose PINT rules are light but not empty: `aligned-ibrp-052` requires an invoice period or a
  line period, and EN 16931 leaves both optional — so an invoice valid everywhere else is refused there.
  Japan's rules also accept the older `urn:fdc:peppol:jp:billing:3.0` and either family's business process,
  which is unusual enough to be worth knowing.
- **Malaysia**, whose rules want three identifiers EN 16931 treats as optional: the BRN of *both* parties and
  the supplier's TIN, each a fatal rule. `Describe` puts them where those rules look — the BRN as the legal
  registration, the TIN as the tax registration under a scheme other than VAT. Its tax category codes are its
  own too, including high-value goods, low-value goods and tourism tax, which have no European equivalent;
  `MyTaxCategory` carries the list read out of the rule.
- **Singapore**, the first country validated against a jurisdiction's own PINT rules from the start — and
  they had three fatal things to say that EN 16931 never hints at. `S`, the tax category code every European
  example uses, is rejected: Singapore's is `SR`, and `SgTaxCategory` carries the list read out of the rule
  itself. A document UUID is required, which EN 16931 has no term for, so `EInvoice.DocumentUuid` now carries
  one. And the supplier needs a legal entity registration, not just a name and a tax number.
- The Singapore package deliberately has no `Describe`: its rules name no identifier scheme, and this library
  does not guess identifiers.
- **The Peppol PINT rules now run.** This was the largest open item and it looked blocked: OpenPEPPOL
  publishes PINT's artefacts as pre-compiled XSLT 2.0, which neither this library's Schematron engine nor
  .NET's own XSLT processor can execute. The way through was that a compiled Schematron still contains every
  original assertion verbatim — so `CompiledSchematron` reads them rather than translating them, and is
  proved against the one rule set that exists in both forms at the same version. `PeppolPintRules` and
  `AddPeppolPintRulesFrom(directory)` put both layers to work, each scoped to the profiles it governs.
- **The tax scheme is no longer hard-coded to VAT**, which is what running the real A-NZ rules over our own
  output immediately found: `EN 16931`'s bindings assume VAT, Australia and New Zealand require **GST**, and
  four fatal rules say so. `EInvoice.TaxSchemeIdentifier` carries it, the UBL writer and reader respect it,
  and BT-31 is now recognised under whatever the document's scheme is called rather than only under the word
  VAT — so a GST registration no longer lands in the wrong field on the way back in.
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
