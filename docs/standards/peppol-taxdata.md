# Peppol tax data documents

> Recorded state: August 2026, from the artefacts under `specs/national/peppol-taxdata/`.

## What it is

A reporting mandate has two halves. The invoice goes to the buyer; a **tax data document** goes to the tax
authority. OpenPeppol publishes that second document — an envelope, `pxs:TaxData`, carrying its own terms
(`TDT-`, `TDG-`) and then the invoice it reports.

It exists per jurisdiction, and the jurisdictions are more alike than they look:

| Jurisdiction | Namespace | Identifier (TDT-001) | Assertions | Carried |
|---|---|---|---|---|
| **Slovakia** | `urn:peppol:schema:sk-taxdata:1.0` | `urn:peppol:taxdata:sk-1` | 88 | ✅ |
| **ViDA** (EU) | `urn:peppol:schema:vida-taxdata:1.0` | `urn:peppol:taxdata:vida-1` | 87 | ✅ |
| **United Arab Emirates** | `urn:peppol:schema:taxdata:1.0` | `urn:peppol:taxdata:ae-1` | 59 | ❌ — see below |
| **Oman** | `urn:peppol:schema:om-taxdata:1.0` | `urn:peppol:taxdata:om-1` | 67 | ❌ — see below |

Slovakia's rules and ViDA's differ by **one assertion out of eighty-eight**, plus a namespace and an
identifier. That is why one writer serves both, and why `PeppolTaxDataJurisdiction` is data rather than code.

```csharp
PeppolTaxData report = new()
{
    Jurisdiction = PeppolTaxDataJurisdiction.ViDA,   // or .Slovakia
    Uuid = "…",
    IssuedAt = DateTimeOffset.Now,
    TaxDataTypeCode = "S",
    DocumentScope = "D",
    ReporterRole = "C2",
    Authority = new PeppolTaxAuthority { Id = "…" },
    ReportingParty = new PeppolTaxDataEndpoint { Id = "…", SchemeId = "0158" },
    ReceivingParty = new PeppolTaxDataEndpoint { Id = "…", SchemeId = PeppolTaxDataEndpoint.ServiceProviderScheme },
    ReportedDocument = invoice,
    ReportedDocumentUuid = "…",
};

string xml = new PeppolTaxDataWriter().WriteToString(report);
```

## Reading one back

The receiver's side — the tax authority's, or a service provider checking what it is about to send:

```csharp
ParseResult<PeppolTaxData> result = new PeppolTaxDataReader(options, profiles).Read(xml);

PeppolTaxData report = result.Value!;
EInvoice reported = report.ReportedDocument!;   // the projection, read as the invoice it projects
```

`PeppolTaxDataReader.LooksLikeTaxData(xml)` tells one from an invoice by its root element.

**The reported document is read by the UBL invoice reader**, not by a second mapping written beside it. The
projection renames exactly three elements — `pxs:DocumentTypeCode`, `pxs:MonetaryTotal` and
`pxs:DocumentLine` — and is otherwise UBL as published, so the reader translates those and delegates. A
business term the invoice reader maps is a term a tax authority gets back, without anyone remembering to add
it twice.

A jurisdiction this library does not carry still reads: the envelope is the same everywhere, only the code
lists differ. What is lost is the checking of those lists, and that is reported as `EIV1042` rather than
passed off as a document nobody had to judge.

## The reported document is a projection

Every rule describing it is written as *"MUST NOT contain elements other than…"*. The writer emits the
allowed subset of the invoice and drops the rest — the buyer reference, the payment terms, the due date, the
seller's contact. **An invoice you can send is not a report you can send**, and a writer that passes the
invoice through is the straightforward way to get that wrong.

Reading one back makes the shape of that subset visible, and the sharpest edge is the supplier: the rules
define no `cac:PartyLegalEntity` under `cac:AccountingSupplierParty`, so **the report carries no supplier
name at all** — only their VAT identifier and country. A receiver expecting to learn who sent the invoice
from the report alone will not.

## What the rules will not tell you

Two things are worth knowing before trusting a green run.

**No schema is published beside them.** The element order this library writes is the one the rules themselves
enumerate, in the order they enumerate it. That is evidence, not proof.

**A rule set can match nothing at all.** The jurisdictions are the same rules in different namespaces, so the
ViDA rule set finds no context whatsoever in a Slovak document — and a Schematron engine that reports what it
found will report *nothing found*, which looks exactly like *nothing wrong*. This library reports such a run
as **not run**, with the reason, so `ValidationReport.IsComplete` is false and the caller can tell the two
apart. It was a real silent pass here until the tax data documents made it visible.

## Why the Gulf ones are not carried

The Emirati and Omani documents are a **second dialect**, not another set of three strings:

- both require a `pxs:SourceDocument` beside the reported one, and a reporter's representative;
- the UAE requires jurisdiction-specific content — the invoice total in AED (BTAE-20) inside
  `pxs:CustomContent` — and its own transaction type codes;
- Oman requires the date and time the original document was received (TDOM-04, TDOM-05);
- both use different code lists throughout: `S R W F` against `S R D`, `D IP INP` against `D IC INTL`,
  `01 02` against `C2 C3`.

They also sit on top of invoice models this library does not carry yet — the BTAE and BTOM business terms —
which is the same reason the Emirati and Omani *invoices* are not done. Doing the reports without the invoices
would be building the roof first.

## Sources

| Source | Use it for |
|---|---|
| [OpenPeppol](https://docs.peppol.eu/) | The specifications themselves |
| [phive-rules](https://github.com/phax/phive-rules) | The artefacts — `build/fetch-specs.sh national` |
