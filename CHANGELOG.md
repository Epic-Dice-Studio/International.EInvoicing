# Changelog

All notable changes to this project are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the project adheres to
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

- **Factur-X, Belgium and France have document corpora at last.** Factur-X — the format both France and
  Germany run on — had none: every Factur-X test in this repository used an invoice written in this
  repository, which measures the library against its own idea of one. There are now 58 published documents
  across all five profiles, 36 published Belgian invoices, and the DGFiP's own worked examples.
- **A profile that extends EN 16931 is judged by its own rule set, not by EN 16931 as well.** Registering
  both rejects invoices the publishers call valid: 8 of the 58 Factur-X documents and 17 of the 36 Belgian
  ones. Factur-X EXTENDED allows grouped lines, where a heading's amount is the sum of its children, and
  EN 16931's BR-CO-10 adds the headings to the details and finds the total twice over; `GLOBALUBL.BE`
  bundles the EN 16931 rules and *adapts* several, and the unmodified originals re-impose what Belgium
  relaxed. Both are pinned as tests, and whether `AddDefaults()` should refuse to attach EN 16931 to a
  document whose declared profile derives from it is on the roadmap.
- The French examples come from the published specification rather than a repository: the DGFiP ships an
  `Exemples` folder inside `specifications-externes-v3.0.zip` — an invoice in both syntaxes, the lifecycle
  messages and the three e-reporting flows. v3.1 and v3.2 dropped it.
- **Hybrid PDFs are checked against the shapes they actually arrive in**: a CSV beside the XML, other files
  listed first, several attachments of assorted types, a second XML that is a supporting document, both
  known payload names present at once, a name matched whatever its case, and an invoice filed only in the
  EmbeddedFiles name tree or named only by `/UF`.
- **Order-X hybrid PDFs can be opened.** An order is filed as `order-x.xml`, and a reader handed only the
  invoice names found nothing in a perfectly good order. `OrderXAttachment` says the name, and the PDF
  FNFE-MPE publishes is read end to end.

- **EN 16931's own unit cases now run** — 281 documents, each named after the rule it exercises and each
  declaring whether that rule should fire. Until now this library measured itself against the standard's
  *examples*, which are all conformant: a corpus of conformant documents can only show an engine is not too
  strict, never that it is not too lax, and too lax is the direction that lets a bad invoice through.
  277 pass; three are listed in the test as known and unexplained.
- The cases ship in the same repository as the artefacts and are fetched at the same tag, which is what makes
  them readable as a verdict. A negative corpus from a *different* version proves nothing: a rule identifier
  outlives the rule's wording, so a document written to break a rule in one release can satisfy the same
  rule's later wording, and the disagreement says nothing about the engine.
- `SchematronRuleSet.RuleIdentifiers` says which rules a rule set carries, so "this document satisfies the
  rule" can be told from "this rule set has no such rule". The two look identical in a report.

- **CII documents are validated against D16B, not D22B.** EN 16931's CII syntax binding names D16B, and so do
  XRechnung, Factur-X and Peppol — every CII profile this library implements. The two revisions share their
  namespaces, so the D22B schemas attached silently to D16B documents and rejected values D16B allows: a
  conforming XRechnung invoice with an allowance reason code of `TAC` came back invalid. `AddCiiSchema()` is
  now D16B; `AddCiiSchema(CiiSchemaVersion.D22B)` is there for a document written against the later revision.
- **The engine is now compared against another engine.** `build/fetch-specs.sh kosit` brings the KoSIT
  validator — the reference implementation German authorities run — and the cross-check tests compare
  acceptance and rule-by-rule findings over the official XRechnung corpus. That is what found the schema
  defect above; no corpus of expected results could have.
- Four documents remain where this library rejects what the reference accepts, all EN 16931 code-list or
  calculation rules. They are listed by name in the test so a new disagreement still fails the build.

- **ZUGFeRD 1.0, for reading** — `International.EInvoicing.Zugferd1`. The 2013 German hybrid invoice,
  replaced in 2019 and still sitting in archives. CII from before CII settled: FeRD's own document namespace
  and versions 12 and 15 of the data types, so it needs its own reader rather than a flag on the CII one.
- Reading only, on purpose. What an archive needs is a way forward, not a way to make more of a retired
  format; `EInvoicing.Convert` writes it out as ZUGFeRD 2, Factur-X, CII or UBL. What a migration must decide
  for itself is the conformance claim: the reader reports what the 2013 document said, and this library will
  not invent a new one on the way out.
- All four reference documents are read with nothing dropped, they satisfy FeRD's own schema and rule set,
  and the migration forward is judged against mustangproject's own ZUGFeRD 2 conversion of one of them —
  same totals, same parties, same lines.
- **`InvoiceNodes.Descendants` had stopped reaching two nodes**, which meant `Convert` under-reported
  conversion losses and `inspect` under-counted extensions on a line's price adjustments and an item's
  classifications. Its own remark promised a test that would catch this; there wasn't one, and now there is.

- **Unmapped content is written back where it was read from in CII and CDAR too.** The UBL half shipped
  first; `CiiInvoiceWriter` and `CdarWriter` now route through `AnchoredDocument`, which is where the
  anchoring lives rather than in a copy per syntax.
- CII needed a more precise anchor than UBL did. A model node there is not always one element — a CII invoice
  fills itself from the document context, the exchanged document and three header sections at once, and
  `ram:ID` appears in most of them — so the sibling it followed would have placed an extension after the
  first `ram:ID` written anywhere in the node. `ExtensionElement.ParentName` records the element it sat in,
  and the two together are an address rather than a guess.

- **Order-X**, the Franco-German order — `International.EInvoicing.OrderX`. Same publishers as Factur-X,
  same CII family, one document earlier in the chain, and a different UN/CEFACT message: the Cross Industry
  Order, on version 128 of the same data types, so nothing that reads an invoice reads it. The order and the
  order change are read, written, round-tripped element for element and in sequence, and judged by FNFE-MPE's
  own schema and its 124 assertions against the one document they publish.
- The schemas and rules are **fetched, not shipped** — FNFE-MPE and FeRD publish Order-X behind a
  registration. `build/fetch-specs.sh order-x` fills `specs/order-x`; `AddOrderXSchemaFrom` and
  `AddOrderXRulesFrom` put them to work, one rule set per profile so a BASIC document is judged by BASIC.
- `CiiValueReader` is **public**, because the Cross Industry Invoice is not the only message in its family
  and anyone teaching this library another one should not have to write value reading again. Its `udt`/`qdt`
  lookups match on local name, since the data-type namespace carries a version, and it now reads a moment as
  well as a day — Order-X states the issue time as `CCYYMMDDHHMM`.
- **An item classification is a code and a name.** UBL puts the name on the code, CII beside it, and this
  library kept only the code — in `Item`, `OrderItem` and `DespatchItem`, in both syntaxes, from the
  beginning. `ItemClassification` carries both. A `CodeField` still converts implicitly, so a caller who has
  only a code writes what they wrote before.
- Three more losses the census caught: a gross price may carry **several** per-unit allowances and the model
  held one amount, so `LinePrice.Adjustments` is the full account and `Discount` is their total; a delivery
  event may state **both** a preferred date and an acceptable window, so `OrderDelivery.RequestedAt` is kept
  apart from the window; and a contact's department and function code had nowhere to go.
- **The Order-X order response is not done.** It fills `OrderResponse` rather than `Order`, and there is no
  published reference document for it — tracked on the roadmap.

- **Unmapped content is written back where it was read from, in UBL.** An element nobody maps used to be kept
  verbatim and flushed at the end of the node holding it. UBL's element order is normative, so content in the
  wrong place is content a receiver's parser rejects: keeping it was only half of not losing it. Each
  extension now remembers the mapped sibling it followed, and the writer puts it back after that sibling.
- The reader is the half that makes it work. An extension can only follow a sibling if it is held by the node
  that sibling belongs to, and everything unmapped used to bubble up to the invoice — so a national extension
  written inside a party came back outside it. `UblValueReader` now records which node each element's content
  belongs to, and parties, addresses, contacts, deliveries and allowance charges keep their own.
- Nothing changed in the writers' shape: `UblDocument` grew the `XmlWriter` members the writers were already
  calling, so 163 call sites stayed as they were and 33 signatures changed type.
- **CII is not covered.** Its order is normative too and its writers still flush at end-of-node; the reader
  half there has more nesting to attribute and is tracked separately on the roadmap.

- **The Peppol Order Change, and with it the whole post-award family.** All nine transactions — Order, Order
  Change, Order Cancellation, Order Response (simple, advanced and agreement), Despatch Advice, Invoice
  Response and Message Level Response — are read, written, round-tripped element for element and judged by
  the OASIS schema and Peppol's own rules against the entire published corpus. One element in the lot is
  left unmapped: `cac:Person` on a despatch advice's carrier, which identifies the driver.
- An order change is an **order that amends an earlier one**, so it fills the same model and
  `DocumentResult.Kind` tells them apart — the arrangement an invoice and a credit note already have. It adds
  the sequence number, because two amendments may not arrive in the order they were sent, and a status code
  per line, because a change restates every line and marks only the ones that moved.

- **The Peppol Order Agreement** — the third profile on the `OrderResponse` root, and the fullest: it
  restates the whole order as the parties settled it, with the totals, the VAT breakdown, the allowances, the
  extra parties, and on each item the certificates and the specification document agreed against. Every
  element of the published document is mapped and comes back in place.
- Modelling it rather than keeping it as extension data is the point: **an element of an agreement left
  unmapped is a term of a contract nobody can see.** A buyer who agreed to a certified product and received
  an uncertified one did not get what they agreed to, so `OrderItemCertificate` carries the label, its type,
  and who issued it.

- **The Peppol Order Cancellation**, and the **advanced Order Response**. The cancellation withdraws an order
  and says why — a cancellation the seller cannot explain is one they will query rather than act on. The
  advanced response needed no reader of its own: it is the *same document* as the ordinary order response
  under a profile that answers line by line, so registering the profile and adding the one reference it
  carries — which version of the order the seller answered — was the whole of the work.
- **This is where the shipped schemas end.** `cac:OrderChangeDocumentReference` is not in UBL 2.1; Peppol's
  advanced ordering is built on a later UBL, so one published scenario does not validate against the 2.1
  schema before this library touches it. The round-trip test for those documents therefore asserts that **a
  round trip introduces no schema error the document did not already have** — true of all of them, where
  "no errors" would have meant excluding the document or pretending.

- **The Peppol Order Response** — the seller's answer to an order, and the pre-award twin of the gap the
  Invoice Response closes after the invoice. Read, written, schema-checked and judged by Peppol's own T76
  rules against all six documents OpenPEPPOL publishes, with nothing left unmapped.
- What makes it more than a yes or no is that a seller may accept a line on **other terms** — a different
  quantity, a later date, or a substitute product — and the buyer needs to see which before the goods turn
  up. `SubstitutedItem` is modelled rather than kept as extension data for that reason.
- **Requested and promised are different claims by different parties.** `OrderDelivery` keeps both: a buyer
  asking for Friday and a seller promising Monday is the ordinary case, and one delivery window would lose
  which of them said what.

- **The Peppol Order** — [the standard page](docs/standards/peppol-post-award.md). The first document of the
  post-award chain and the one the other two are answered against: a despatch advice says what was sent of
  it, an invoice says what is owed for it. Read, written, schema-checked and judged by Peppol's own T01 rules
  against all seven documents OpenPEPPOL publishes, with nothing left unmapped and the same elements coming
  back in the same places.
- Its amounts are **anticipated**, not due — `cac:AnticipatedMonetaryTotal` — because an order commits to a
  price and not to a debt. `OrderItem` is a third item type for the same reason `DespatchItem` was a second:
  an order's item is being chosen from a catalogue, so it carries the manufacturer's article number and the
  specification the buyer is ordering against.
- `PartialDeliveryAccepted` is the term that joins the three documents: a line the buyer will not take in
  part makes an outstanding quantity on the despatch advice a failure rather than a note.
- **The despatch advice was writing `cac:AdditionalDocumentReference` in the wrong place**, and its schema
  check had been passing without noticing. The DespatchAdvice schema was never fetched, so the validator had
  no global declaration to match its root against and judged nothing — the same vacuum a document type falls
  into whenever its schema is missing. Fetching the Order schema brought the despatch one with it, the check
  became real, and it found the defect immediately.

- **Grouped invoice lines** — `ParentLineIdentifier`, `LineStatusReasonCode` and `LineStatusCode` on
  `InvoiceLine`, read and written on the CII side. EN 16931 has no term for a line hierarchy and Factur-X
  EXTENDED does, and it is expressed by **reference rather than by nesting**: the lines stay a flat list and
  each child names its parent's line number. Reading one without those terms gives every line and no
  structure — and because a `GROUP` heading's amount is already the sum of the lines beneath it, totalling
  every line counts those amounts twice. `LineStatusReasonCodes.IsCharged` says which lines to add up, and
  treats a line that says nothing as one to charge for, which is what every EN 16931 invoice means.

- **A hybrid XRechnung now says `XRECHNUNG`**, and used to say `EN 16931`. The Factur-X conformance level
  was answered by falling through to EN 16931 for anything that was not MINIMUM, BASIC WL, BASIC or
  EXTENDED — so a German hybrid invoice was stamped with a profile it is not written against, and a receiver
  told EN 16931 does not apply the German rules. That mattered little while the metadata was an object
  nothing pointed at; it matters now that [it is the document's own](docs/diagnostics/EIV4011.md). A profile
  Factur-X publishes no conformance level for is refused at write time rather than guessed.
- **[EIV4011](docs/diagnostics/EIV4011.md) reads both XMP serialisations.** A simple property may be a child
  element or an attribute of the `rdf:Description`, and Adobe's own tooling writes the attribute form.
  Reading only elements made such a container look like one saying nothing about an invoice — so a block
  claiming the wrong profile passed unremarked, which is the one thing the check exists to stop.
- The hostile PDF corpus now asks each document what its metadata claims as well as what it carries. The two
  walk different parts of the file, so a document that survives one can still break the other.

- **Reading a tax data document** — [the standard page](docs/standards/peppol-taxdata.md). It was the one
  place this library did not read back what it writes, and that parity is what lets an integrator test their
  own output and a receiver use the same library as the sender. `PeppolTaxDataReader` closes it, for
  Slovakia, ViDA, and a jurisdiction this library does not carry — the envelope is the same everywhere, and
  the loss of the code-list checking is reported rather than passed off.
- The reported document is read **by the UBL invoice reader**, not by a second mapping written beside it: the
  projection renames three elements and is otherwise UBL as published, so the reader translates those and
  delegates. A business term the invoice reader learns is one a tax authority gets back.
- Reading one back is also the first time the projection's shape was visible from the outside, and it has a
  sharp edge worth knowing: **a tax data document carries no supplier name.** The rules define no
  `cac:PartyLegalEntity` under the supplier, so the report identifies who sent the invoice by VAT identifier
  and country alone.

- **The Peppol Despatch Advice** — what actually left the warehouse, which is the document an invoice is
  reconciled against. An invoice says what is owed and an order says what was asked for; only this says what
  was sent, and receiving ten of an ordered twelve is the whole reason `cbc:OutstandingQuantity` exists.
  Read, written, schema-checked and judged by Peppol's own rules against all six documents OpenPEPPOL
  publishes, with exactly one element in the whole corpus left unmapped — `cac:Person` on the carrier, kept
  verbatim and reported.
- It does **not** reuse the invoice's `Item`. UBL calls both `cac:Item`, but an invoice's item is what is
  being charged for and a despatched item is a physical thing in a box: which serial numbers went out, which
  lot they came from, whether the box is dangerous to carry. `DespatchItem` carries the second, so the
  invoice model does not grow a logistics vocabulary no invoice uses.
- Three UBL writers were carrying three copies of the same element helpers, and three readers three copies
  of the same extension-data keeper. They are now one `UblDocument` and one `UblExtensions`, which is how
  the despatch advice writer got element order right for free.

- **The Peppol documents that are not invoices, starting with the answer a receiver owes a sender** —
  [the standard page](docs/standards/peppol-post-award.md). The **Invoice Response** says what happened to the
  invoice — in process, accepted, rejected, under query, conditionally accepted, paid — and the **Message
  Level Response** answers the question underneath it: did the message arrive and parse at all. Both are a
  UBL `ApplicationResponse`, so both are read and written by one reader and one writer, and both fill
  `LifecycleStatusMessage` — the same model the French CDAR messages fill, because a lifecycle status is a
  semantic statement and CDAR and `ApplicationResponse` are two syntaxes for it. `Read` works out which
  arrived; `Write(status, DocumentSyntax.Ubl)` chooses which to send.
- Measured against OpenPEPPOL's own corpus rather than documents written for the occasion: all fifteen
  published examples and use cases read with nothing left unmapped, written back with the same elements in
  the same places, accepted by the OASIS schema, and accepted by Peppol's own rules with zero errors.
  `PeppolResponseCodes` is compared against the publisher's `UNCL4343-T111` subset on every build the
  artefacts are present for.
- **Two defects in this library's own rule engine, both found by pointing it at those rules**, and both the
  same shape — a rule set that loads, reports that it ran, and judges nothing. `AddRulesFromFile` read a
  compiled artefact as source Schematron, found no patterns, and built an empty rule set; it now recognises
  a stylesheet by what it is rather than by its file name. And `*:name` — a local name in any namespace —
  was an expression the XPath engine refused outright, which matters because `not(@*:schemaLocation)` is the
  first rule in every Peppol rule set. Both affect any caller who points the engine at a published compiled
  artefact, which is how Croatia, PINT and the tax data documents are validated.

- **The readable copy of an invoice, and the documents it carries, in one call each** —
  [the guide](docs/guides/attachments.md). `result.Rendition` is the PDF a hybrid invoice arrived in, which
  used to go out of scope with the stream and leave a caller holding an invoice they could not show anybody;
  `invoice.SupportingDocuments` are the BG-24 attachments, decoded, with their media type and file name;
  `invoice.SupportingDocumentLinks` are the ones the invoice only points at. Three types rather than one,
  because a caller who treats a delivery note as the invoice's readable copy has mixed up two different
  things — and a BT-124 link has no `OpenRead()` on purpose: fetching it is network I/O this library does
  not do, and the decision to open a URI from a third party stays the caller's.
- **A hybrid PDF is now checked against its own metadata** — [EIV4011](docs/diagnostics/EIV4011.md). The XMP
  repeats the profile and the name of the embedded file so a reader can tell what it holds without opening
  it; when that disagrees with the XML inside, a receiver who trusts the metadata and one who reads the
  payload hold different documents and both are confident. Nothing else in the chain notices: no Schematron
  rule looks at a PDF, and neither does a schema. Both the Factur-X and ZUGFeRD namespaces are read, and
  metadata that says nothing about an invoice stays silent rather than becoming noise.
- **And it found that our own container did not carry that block — now it does.** PDFsharp writes its own
  XMP as it saves and points the catalogue at it whatever was there, so the Factur-X metadata this library
  wrote sat in the file as an object nothing referenced: every hybrid PDF it produced was affected. It is now
  written after the save as a **PDF incremental update**, superseding the object the catalogue points at with
  what the backend wrote plus the Factur-X block, and leaving every existing byte offset where it is. Read
  back from the catalogue — by this library and by an unrelated PDF engine — the profile is there.
- **The PDF/A extension schema for the `fx` properties**, which the specification requires and this library
  omitted. PDF/A allows no metadata property it cannot describe, so a container carrying the Factur-X
  namespace without it was refused by a conformance checker with every other rule satisfied.
- **A conformance level is the source document's to claim.** Attaching XML to a PDF does not make it PDF/A
  ([ADR 0010](docs/adr/0010-no-pdf-rendering.md)), so a document that declared none is still given none — and
  one that declared PDF/A-3 keeps saying so, where the backend regenerating the metadata used to drop the
  declaration on the way out.
- **The playground converts between syntaxes**, with the loss report beside the result — UBL to CII and back,
  from a pasted document or any of the samples. What did not cross is listed by name and by where it was,
  because a silent conversion is the dangerous kind. A Factur-X PDF is opened under *Look inside one* first:
  the page reads PDFs and writes none. Every sample the site offers is converted both ways on every commit,
  so a sample that stops crossing fails the build rather than the demo.
- **The CII schemas, and the eight terms that half lost.** `AddCiiSchema()` puts the UN/CEFACT D22B schemas
  — embedded, offline — beside the UBL ones, and running the official CII corpus through a read-then-write
  found the same disease as on the UBL side: seven of fifteen examples came back in a shape the schema
  refuses, each because a term was unmapped. **BT-7** was read only at document level while CII files it
  inside the VAT breakdown, and as a `DateString` rather than a `DateTimeString`. **BT-18** was read as an
  attachment instead of the object the invoice is about. **BT-71** was lost when the delivery location was
  identified by GLN. **BT-111**, **BT-128** and its scheme, the basis quantity stated on both prices, and the
  tax scheme on a document-level allowance — all now read and written.
- **Two shape defects of our own on the CII side**: `SellerOrderReferencedDocument` was written after
  `BuyerOrderReferencedDocument` where the schema declares the reverse, and `SpecifiedProcuringProject` was
  written without the name D22B requires — so any invoice carrying BT-11 was refused by a schema and by no
  rule.
- **Seven EN 16931 terms the UBL side lost, in both directions.** BT-15 and BT-16 (receipt and despatch
  advice references), BT-17 (tender or lot), BT-89 and BT-91 (the direct debit's mandate and debited
  account), BT-111 (the tax in the accounting currency) and BT-128 (the line's object identifier) were read
  by nothing and written by nothing, though the model has held them all along and CII read two of them. A
  caller who set one got a document without it; a document that carried one was read with the field empty;
  and converting CII to UBL dropped what CII had understood. **BT-89 and BT-91 were missing on the CII side
  too**, and BT-128 was written by neither — all now read and written in both syntaxes.
- **An attachment was written twice.** BT-125 was read into the model *and* kept as extension data, so every
  rewrite carried the bytes a second time — megabytes for a scanned delivery note, and a cardinality the
  schema refuses.
- **The consequence of all of the above**, and how it was found: an element nobody maps is kept verbatim and
  written back at the end of its node, which UBL does not allow. Six of the seventeen official EN 16931
  examples came out of a read-then-write schema-invalid. All seventeen now come back valid **and carrying
  nothing this library failed to understand** — which is what the corpus test now demands.
- **Schema validation, offline** — `International.EInvoicing.Validation.Xsd`. The OASIS UBL 2.1 schemas are
  embedded and register as a rule set like any other: `builder.AddDefaults().AddUblSchema()`. It judges what
  no business rule looks at, since element order and cardinality are normative in UBL and no Schematron
  assertion reads either. Proof it was needed: it rejects the two-accounts-in-one-`cac:PaymentMeans` document
  this library produced until yesterday, which all 955 EN 16931 assertions and Peppol's had accepted.
- **And it found the mapping gaps above on its first run**, by way of their symptom: extension data is
  written back at the end of its node, which in UBL means after elements that must follow it. Six official
  examples came out of a read-then-write in an order the schema rejects — every one of them because a term
  was unmapped. With the terms mapped there is nothing left to misplace, and the corpus is clean. Anchoring
  extension data where it was read from is still worth doing for genuinely foreign elements, and is now a
  small remainder rather than the story.
- **Reading a PDF could throw, which the rest of this library promises never to do.** An empty file, a file
  that is not a PDF, a bare `%PDF-1.7` header, a truncated document, one byte changed in the trailer — eight
  of fifteen hostile cases came out as an exception rather than as "there is no invoice in this file", and
  `EInvoicing.Read` propagated it to the caller. Only PDFsharp's own `PdfReaderException` was caught, and a
  malformed document reaches whatever failure the code happens to hit first. The reader now answers `null` for
  everything the PDF is or is not, `IPdfAttachmentReader` says so as a contract, and a hostile PDF corpus —
  the neighbours' list: no attachment, encrypted, truncated, damaged, oversized — holds it to that.
- **`ancestor-or-self` and `descendant-or-self` dropped the node itself** in the Schematron engine's XPath:
  both were mapped onto their plain forms. The compiled EN 16931 artefacts use the first to build the path of
  a failure and OpenPeppol's tax data rules to count the elements above a node, so nothing changed verdicts
  today — but an engine that answers a different question from the one asked is a defect before anyone
  notices.
- **A path did not yield a node-set.** Two lines share one parent, and walking up from both reached that
  parent twice, so `count()` over such a path over-counted and `sum()` would have double-added. Nodes are now
  folded once, in document order; sequences of values still repeat, because `sum(tokenize(...))` depends on
  it.
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
