# The Peppol post-award documents that are not invoices

## Scope and version

Seven Peppol documents that are not invoices — the chain an invoice sits at the end of.

| | What it answers |
|---|---|
| **Invoice Response** — `urn:fdc:peppol.eu:poacc:trns:invoice_response:3` | *What happened to the invoice?* In process, accepted, rejected, under query, conditionally accepted, paid. |
| **Message Level Response** — `urn:fdc:peppol.eu:poacc:trns:mlr:3` | *Did the message arrive and parse at all?* One layer below the business question. |
| **Despatch Advice** — `urn:fdc:peppol.eu:poacc:trns:despatch_advice:3` | *What actually left the warehouse?* The document an invoice is reconciled against. |
| **Order** — `urn:fdc:peppol.eu:poacc:trns:order:3` | *What did the buyer ask for?* The document the others are answered against. |
| **Order Response** — `urn:fdc:peppol.eu:poacc:trns:order_response:3` | *Will the seller supply it?* Accepted, rejected, or accepted on other terms. |
| **Order Response (advanced)** — `…:order_response_advanced:3` | The same document, answering line by line. |
| **Order Cancellation** — `urn:fdc:peppol.eu:poacc:trns:order_cancellation:3` | *The buyer has withdrawn the order*, and why. |

An Invoice Response is what a receiver **owes** a sender: without it, a supplier who has sent an invoice into
the network knows nothing until the money arrives or does not. It is implemented in
`International.EInvoicing.Peppol`, over the UBL binding in `International.EInvoicing.Ubl`.

## Official sources

| Source | Use it for |
|---|---|
| <https://docs.peppol.eu/poacc/upgrade-3/> | The specification: transaction structure, code lists, business rules. |
| <https://github.com/OpenPEPPOL/poacc-upgrade-3> | Examples, the thirteen use cases, the unit corpus, the code lists. |
| <https://github.com/phax/phive-rules> | The compiled rule sets, which are the runnable ones. |

## Artefacts

`build/fetch-specs.sh poacc`, into `specs/peppol/poacc/` — git-ignored, because OpenPEPPOL declares no
licence permitting redistribution. See [the provenance](../../specs/peppol/PROVENANCE.md).

**The published `.sch` files are not whole.** Each one `include`s a `target/generated/T*-basic.sch` that
OpenPEPPOL's own build produces from its structure spreadsheets and does not commit, so running them as
published silently drops the structural half of every rule set. What is fetched instead is the **compiled**
form, which is complete; `CompiledSchematron` recovers the assertions from it.

## Structure

Both transactions are the same UBL document, `ApplicationResponse`:

| Element | Carries |
|---|---|
| `cbc:CustomizationID` | Which of the two this is. `PEPPOL-T111-R003` requires the Invoice Response one verbatim. |
| `cbc:ID`, `cbc:IssueDate`, `cbc:IssueTime` | The response's own identity and moment. |
| `cac:SenderParty`, `cac:ReceiverParty` | Who is answering, and who is being answered. Both mandatory. |
| `cac:DocumentResponse` | One per document reported on. |
| `…/cac:Response/cbc:ResponseCode` | The status, from the UNCL 4343 subset. |
| `…/cac:Response/cac:Status` | Why, and what is expected next. |
| `…/cac:DocumentReference` | Which document: its identifier, issue date, type code, version. |
| `…/cac:LineResponse` | What happened to individual lines, when it differs. |

### The status codes

`PeppolResponseCodes`, checked against OpenPEPPOL's own `UNCL4343-T111` subset on every build the artefacts
are present for:

| Code | Meaning |
|---|---|
| `AB` | Message acknowledged — readable, and submitted for processing. |
| `IP` | In process. Nothing is promised yet. |
| `UQ` | Under query. Something is unclear; the invoice is **not** rejected. |
| `CA` | Conditionally accepted, subject to the clarification given. |
| `RE` | Rejected — this invoice will not be processed further. |
| `AP` | Approved. The next step is payment. |
| `PD` | Paid — the payment has been initiated. |

The distinction that costs money is `AP` against `PD`: one is the buyer's approval, the other says the money
has been sent. A receiver that treats `IP` as acceptance has told the supplier something the buyer did not
say.

### Why and what next

`cac:Status` repeats rather than nesting, and the `listID` on `cbc:StatusReasonCode` tells the two apart:
`OPStatusReason` is **why**, `OPStatusAction` is **what the sender wants done**. A rejection that asks for a
reissue carries two of them. A status with no `listID` is read as a reason, which is what every document in
Peppol's corpus means by it.

`PEPPOL-T111-R001` makes a clarification mandatory for `CA`, `UQ` and `RE`: rejecting an invoice without
saying why leaves the supplier nothing to act on.

## Model mapping

The canonical model is **`LifecycleStatusMessage`** — the same one the French CDAR messages fill. That is not
a convenience: a lifecycle status is a semantic statement, and CDAR and `ApplicationResponse` are two
syntaxes for it, exactly as UBL and CII are two syntaxes for an invoice.

| Model | Invoice Response | CDAR |
|---|---|---|
| `Identifier`, `IssuedAt` | `cbc:ID`, `cbc:IssueDate` + `cbc:IssueTime` | `ram:ID`, `ram:IssueDateTime` |
| `SpecificationIdentifier` | `cbc:CustomizationID` | the guideline context parameter |
| `Sender`, `Recipients` | `cac:SenderParty`, `cac:ReceiverParty` | `ram:SenderTradeParty`, `ram:RecipientTradeParty` |
| `References[].ProcessConditionCode` | `cac:Response/cbc:ResponseCode` | `ram:ProcessConditionCode` |
| `References[].StatusDetails` | one per `cac:Status` | `ram:StatusDetail` |
| `References[].LineStatuses` | `cac:LineResponse` | — |

Reading a profile this library does not know still works: the document parses, the codes come back
uninterpreted, and the downgrade is reported rather than hidden.

## Validation

XSD first — element order is normative in UBL and no business rule looks at it — then Peppol's own rules:

```csharp
EInvoicing library = EInvoicing.Create(builder => builder
    .AddDefaults()
    .AddPeppol()
    .AddUblSchema()
    .AddPeppolPostAwardRulesFrom("specs/peppol/poacc/rules"));
```

Each rule set is registered against the transaction it governs. Both documents share a root element and
differ in what they mean, so a rule set let loose on the other's documents reports failures that are not in
them — twelve of them, on OpenPEPPOL's own example.

## The despatch advice

An invoice says what is owed and an order says what was asked for; only the despatch advice says what was
sent. Receiving ten of an ordered twelve is the whole reason `cbc:OutstandingQuantity` exists — and
`PEPPOL-T16-R007` warns when one appears without a reason beside it, because a buyer told goods are missing
and not told why has nothing to act on.

Its model is `DespatchAdvice`, and it does **not** reuse the invoice's `Item`. UBL calls both `cac:Item`, but
an invoice's item is what is being charged for and a despatched item is a physical thing in a box: which
serial numbers went out, which lot they came from, whether the box is dangerous to carry. `DespatchItem`
carries the second so the invoice model does not grow a logistics vocabulary no invoice uses.

| Model | UBL |
|---|---|
| `Number`, `IssuedAt` | `cbc:ID`, `cbc:IssueDate` + `cbc:IssueTime` |
| `DespatchParty`, `DeliveryParty`, `BuyerParty`, `SellerParty`, `OriginatorParty` | the five role wrappers, each holding a `cac:Party` |
| `Shipment` | `cac:Shipment` — weight, volume, carrier, tracking, when it left, when it is expected |
| `Lines[].DeliveredQuantity` / `OutstandingQuantity` / `OutstandingReason` | what arrived, what did not, and why |
| `Lines[].Item.Instances` | `cac:ItemInstance` — serial numbers, lots, best-before dates |
| `Lines[].Packaging.HandlingUnits` | the line's own `cac:Shipment`: pallets, boxes, packages |

Every element of all six documents OpenPEPPOL publishes is mapped, bar one: `cac:Person` on the carrier,
which identifies the driver. It is kept verbatim, written back, and reported as `EIV2020`.

## The order

The first document of the chain. A despatch advice says what was sent of it and an invoice says what is owed
for it, so a buyer who can read all three can check the second two against the first.

Its model is `Order`, and its amounts are **anticipated** rather than due —
`cac:AnticipatedMonetaryTotal`, not `cac:LegalMonetaryTotal`. An order commits to a price, not to a debt.

`OrderItem` is a third item type, for the same reason `DespatchItem` was a second one: an order's item is
being chosen from a catalogue, so it carries the manufacturer's article number and the specification the
buyer is ordering against, neither of which an invoice or a despatch advice has any use for.

| Model | UBL |
|---|---|
| `Number`, `SalesOrderNumber` | `cbc:ID`, `cbc:SalesOrderID` — the buyer's number and the seller's for the same order |
| `Buyer`, `Seller`, `Originator`, `Invoicee` | the four role wrappers |
| `Delivery` | `cac:Delivery` — where, when, who receives, and how urgently |
| `DeliveryTermsCode` | `cac:DeliveryTerms/cbc:ID` — the Incoterm |
| `Lines[].PartialDeliveryAccepted` | `cbc:PartialDeliveryIndicator` — whether a short delivery is acceptable |
| `Lines[].Item.ManufacturerIdentifier` | `cac:ManufacturersItemIdentification` — the number that outlives a seller's catalogue |
| `Totals` | `cac:AnticipatedMonetaryTotal` |

**`PartialDeliveryAccepted` is the term that joins the three documents**: a line the buyer will not take in
part makes an outstanding quantity on the despatch advice a failure rather than a note.

Every element of all seven documents OpenPEPPOL publishes for the order is mapped, and each is written back
with the same elements in the same places, accepted by the OASIS schema and by Peppol's own T01 rules.

## The order response

Without it a buyer who has sent an order knows nothing until goods arrive or do not — the pre-award twin of
the gap the Invoice Response closes after the invoice.

What makes it more than a yes or no is that a seller may accept a line on **other terms**, and the buyer
needs to see which before the goods turn up:

| Model | What it answers |
|---|---|
| `ResponseCode` | The answer as a whole. |
| `Lines[].StatusCode` | What is happening to this line — accepted, changed, rejected. |
| `Lines[].Quantity` | How much the seller will actually supply. |
| `Lines[].Delivery.PromisedFrom` / `PromisedUntil` | When the seller undertakes to deliver. |
| `Lines[].SubstitutedItem` | What the seller offers instead, when they cannot supply what was ordered. |

**Requested and promised are different claims by different parties**, so `OrderDelivery` keeps both: a buyer
asking for Friday and a seller promising Monday is the ordinary case, and one delivery window would lose
which of them said what.

The parts an order response shares with an order — a party, an item, a price, a delivery window — are read
and written by the order's own reader and writer, so the two documents cannot drift apart in how they state
the same thing.

## The cancellation, and the advanced response

A cancellation withdraws an order and says why. `Reason` is not decoration: a cancellation the seller cannot
explain to their warehouse is one they will query rather than act on, which is why the transaction makes it
mandatory.

The **advanced response** needed no reader of its own. It is the *same document* as the ordinary order
response — same root, same shape — under a profile that answers line by line, so registering the profile and
adding the one reference it carries that the simple one does not (`cac:OrderChangeDocumentReference`, which
says *which version* of the order the seller answered) was the whole of the work.

### Where the shipped schemas end

`cac:OrderChangeDocumentReference` **is not in UBL 2.1**, which is the version this library embeds. Peppol's
advanced ordering is built on a later UBL, so one of the published scenarios does not validate against the
2.1 schema as it stands — before this library touches it.

That is why the round-trip test for these documents asserts something different from the others: **a round
trip introduces no schema error the document did not already have**. Asserting "no errors" would have meant
either excluding the document or pretending it was clean; asserting that we do not make a document worse is
true of all of them, and is what a caller actually needs to know.

## What is not here

**Order Change** and **Order Agreement**. The change is its own document; the agreement is the order response
restating the whole order, and carries the totals, allowances, tax and extra parties that the simple response
does not — eighteen elements this library does not yet map, each currently kept as extension data and
reported.

## Prior art

Reading OpenPEPPOL's corpus found two defects in this library's own rule engine, both of the same shape — a
rule set that loads, reports that it ran, and judges nothing:

- **`AddRulesFromFile` read a compiled artefact as source Schematron**, finding no patterns and building an
  empty rule set. It now recognises a stylesheet by what it is rather than by its file name.
- **`*:name` — a local name in any namespace** — was an expression the XPath engine refused outright, and
  `not(@*:schemaLocation)` is the first rule in every Peppol rule set.
