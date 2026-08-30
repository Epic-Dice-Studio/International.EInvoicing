# Peppol PINT

> Recorded state: August 2026. Verify against <https://docs.peppol.eu/> before relying on versions.

## What it is, and why it matters here

**PINT — Peppol International — is the other half of Peppol**, and until now this library only knew the first
half. Peppol BIS Billing 3.0 is a strict CIUS of EN 16931 and was built for Europe: European VAT, European
identifier schemes, European rules. Every jurisdiction that adopted Peppol *outside* Europe runs on PINT
instead, which is built the other way round — a common core, with one **specialisation** per jurisdiction
that adds its tax handling, its identifiers and its mandatory fields.

That distinction is easy to miss and expensive to get wrong. A caller invoicing in Singapore or the UAE who
reached for `PeppolProfiles.BillingUbl` was writing a European profile identifier onto a document the
receiving jurisdiction validates against PINT.

## The identifiers

The specialisation is carried after an `@` in the specification identifier (BT-24):

| Jurisdiction | Specification identifier |
|---|---|
| Common core | `urn:peppol:pint:billing-1` |
| European Union | `urn:peppol:pint:billing-1@eu-1` |
| United Arab Emirates | `urn:peppol:pint:billing-1@ae-1` |
| UAE, self-billing | `urn:peppol:pint:selfbilling-1@ae-1` |
| Australia and New Zealand | `urn:peppol:pint:billing-1@aunz-1` |
| Japan | `urn:peppol:pint:billing-1@jp-1` |
| Malaysia | `urn:peppol:pint:billing-1@my-1` |
| Oman | `urn:peppol:pint:billing-1@om-1` |
| Singapore | `urn:peppol:pint:billing-1@sg-1` |

None of those is transcribed from prose. Each is read out of the published rule artefact for its
jurisdiction, and `PeppolPintProfilesTests` fails the build if one stops appearing there.

## The trap: the business process is a different string

BIS Billing numbers its processes — `urn:fdc:peppol.eu:2017:poacc:billing:01:1.0`. **PINT does not**:

```
urn:peppol:bis:billing
urn:peppol:bis:selfbilling
```

A PINT invoice carrying the BIS process identifier is wrong in a way that looks right, and vice versa. That
is why there are two builder methods rather than one with a flag:

```csharp
EInvoiceBuilder.Create(PeppolPintProfiles.BillingSg).ForPeppolPint();   // urn:peppol:bis:billing
EInvoiceBuilder.Create(PeppolProfiles.BillingUbl).ForPeppol();          // urn:fdc:peppol.eu:…:01:1.0
```

## Scope for this library

| Capability | Status |
|---|---|
| The PINT profiles, all jurisdictions | done — `PeppolPintProfiles` |
| The PINT business process | done — `PeppolBusinessProcess.PintBilling` |
| Reading and writing a PINT document | done — PINT is UBL, and the model is EN 16931 |
| **Running the PINT rules** | **not yet** — see below |
| Jurisdiction identifier schemes (ABN, UEN, TIN…) | planned, per country |
| Peppol transmission | permanently out of scope |

**Why the rules do not run.** OpenPEPPOL publishes PINT's validation artefacts as **pre-compiled XSLT**, not
as source Schematron, and this library's engine executes Schematron — deliberately, since that is what makes
validation work in a browser without an XSLT processor ([ADR 0008](../adr/0008-schematron-engine.md)).
`build/fetch-specs.sh pint` puts the artefacts on disk so the identifiers can be checked against them, but a
PINT document is currently read and mapped with its jurisdiction rules reported as **not run** rather than
silently skipped.

Closing that needs one of: the source Schematron, if OpenPEPPOL publishes it; an XSLT processor for the
non-browser build; or translating the compiled rules, which is the option this project would refuse — a rule
set nobody can compare against its publisher's is worse than no rule set.

## No CII

PINT is carried in UBL. OpenPEPPOL publishes no CII binding for it, so neither does this library.

## Official sources

| Source | Use it for |
|---|---|
| <https://docs.peppol.eu/pint/> | The specification and its jurisdiction specialisations. |
| <https://github.com/phax/phive-rules> | The artefacts, aggregated — what `fetch-specs.sh pint` pulls. |
| <https://peppol.org/learn-more/country-profiles/> | Which jurisdiction is on which. |
