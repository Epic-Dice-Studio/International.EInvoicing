# The Peppol post-award documents that are not invoices

## Scope and version

Three Peppol documents that are not invoices.

| | What it answers |
|---|---|
| **Invoice Response** — `urn:fdc:peppol.eu:poacc:trns:invoice_response:3` | *What happened to the invoice?* In process, accepted, rejected, under query, conditionally accepted, paid. |
| **Message Level Response** — `urn:fdc:peppol.eu:poacc:trns:mlr:3` | *Did the message arrive and parse at all?* One layer below the business question. |
| **Despatch Advice** — `urn:fdc:peppol.eu:poacc:trns:despatch_advice:3` | *What actually left the warehouse?* The document an invoice is reconciled against. |

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

## What is not here

**Ordering.** The Order, Order Response, Order Change, Order Cancellation and Order Agreement are five more
transactions with their own models — a bigger piece than this one, and the next in the family.

## Prior art

Reading OpenPEPPOL's corpus found two defects in this library's own rule engine, both of the same shape — a
rule set that loads, reports that it ran, and judges nothing:

- **`AddRulesFromFile` read a compiled artefact as source Schematron**, finding no patterns and building an
  empty rule set. It now recognises a stylesheet by what it is rather than by its file name.
- **`*:name` — a local name in any namespace** — was an expression the XPath engine refused outright, and
  `not(@*:schemaLocation)` is the first rule in every Peppol rule set.
