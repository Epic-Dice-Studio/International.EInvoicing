# Slovakia

> Recorded state: August 2026. Verify against the Finančná správa before relying on dates or details.

## The mandate

Slovakia's B2B obligation starts on **1 January 2027**. It has two halves, and only one of them is the
invoice:

1. **The invoice** — Peppol BIS Billing 3.0 in UBL — travels between the parties over the Peppol network,
   in the five-corner model.
2. **A tax data document** about it goes to the financial administration, within fifteen minutes, from the
   service provider of each party.

The second is a document, not a summary: OpenPeppol publishes its identifier, its structure and **88
assertions** that judge it.

## Scope for this library

| Capability | Package | Status |
|---|---|---|
| Peppol BIS Billing 3.0, both syntaxes | `.Peppol` | done |
| The tax data document, built from the invoice it reports | `.Peppol` | done — `PeppolTaxData`, `PeppolTaxDataWriter`, `PeppolTaxDataReader` |
| The 88 published assertions | `.Peppol` | run once fetched — `PeppolTaxDataValidator` |
| Reading a tax data document back | — | not yet: a receiver's job, and no caller has needed it |
| Transmitting either document | — | permanently out of scope: no network I/O |
| A Slovak CIUS of EN 16931 | — | **there is none published**; this library does not invent identifiers |

## The tax data document

It is a `pxs:TaxData` envelope in `urn:peppol:schema:sk-taxdata:1.0`, carrying terms of its own (TDT-, TDG-)
and then the invoice it reports. It is **not a Slovak invention**: the same document is published per
jurisdiction, and Slovakia's rule set differs from the EU's ViDA one by a single assertion out of 88 — so it
lives in `International.EInvoicing.Peppol`, and `PeppolTaxDataJurisdiction` is what changes between them. See
[the tax data page](peppol-taxdata.md).

```csharp
SlovakEInvoicing slovensko = SlovakEInvoicing.Create();

PeppolTaxData report = slovensko.TaxDataFor(invoice, uuid: "…", reportedDocumentUuid: "…");
report.Authority = new PeppolTaxAuthority { Id = "SK-FS", Name = "Finančné riaditeľstvo Slovenskej republiky" };
report.ReportingParty = new PeppolTaxDataEndpoint { Id = "…", SchemeId = "0158" };
report.ReceivingParty = new PeppolTaxDataEndpoint { Id = "…", SchemeId = PeppolTaxDataEndpoint.ServiceProviderScheme };

string xml = slovensko.Write(report);
```

What `TaxDataFor` fills in is what follows from the invoice and the rules; the authority and the endpoints are
the network's business, and are left to the caller rather than guessed.

**The reported document is a projection, not a copy.** Every rule that describes it is written as *"MUST NOT
contain elements other than…"*, so the writer emits the allowed subset and drops the rest — the buyer
reference, the payment terms, the due date, the seller's contact. Passing them through is what makes the
document fail, so an invoice you can send is not a report you can send.

**Two things are stricter than they look.** `cbc:IssueDate` must carry no timezone and `cbc:IssueTime` must
carry one, which is why the model holds a single `DateTimeOffset` and the writer splits it. And the receiving
party's scheme is not merely four digits, as it is for the reporting party: `ibr-tdd-20` requires `0242`, the
Peppol service provider scheme, because the receiver of a tax data document is a service provider rather than
a taxpayer.

## What is measured, and what is only evidence

The rules are fetched, not shipped — `build/fetch-specs.sh national` writes them to
`specs/national/peppol-taxdata/schematron/tdd/sk` — and a document this library writes satisfies all 88 of
them, with four negative controls proving the rules actually ran.

**No schema is published beside them.** So the element *order* this library writes is the one the rules
themselves enumerate, in the order they enumerate it. That is evidence, not proof: if OpenPeppol later
publishes the XSD and it disagrees, the order changes and the rules will not notice. It is recorded here so
that nobody mistakes a passing test for a validated schema.

## Why there is no Slovak identifier type here

Every check digit in this library is measured against the publisher's own rule and a published example.
Peppol publishes such a rule for Norwegian, Danish, Belgian, Italian, Swedish and Icelandic identifiers, and
for GLNs. It publishes none for the Slovak IČO, and no Slovak artefact this repository can fetch carries one.
A check digit implemented from prose and tested against itself proves nothing, so there is no `SkIco` type —
the VAT identifier travels as BT-31 like any other, and EN 16931's own rules judge it.

## Official sources

| Source | Use it for |
|---|---|
| [Finančná správa](https://www.financnasprava.sk/) | The mandate, and the reporting obligation |
| [OpenPeppol tax data specifications](https://docs.peppol.eu/) | The tax data document and its rules |
| [phive-rules](https://github.com/phax/phive-rules) | The artefacts, aggregated — what `fetch-specs.sh national` pulls |
