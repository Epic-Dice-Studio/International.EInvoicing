# Factur-X / ZUGFeRD — hybrid PDF/A-3 invoices

## Scope and version

Factur-X (France) and ZUGFeRD (Germany) are the same standard published by two organisations: a PDF/A-3
document with a CII XML file embedded in it. Humans read the PDF, machines read the XML, and both are
guaranteed to be the same invoice.

We target **Factur-X 1.07.3 / ZUGFeRD 2.3.3**.

## Profiles

Five, cumulative:

| Profile | Contains | Typical use |
|---|---|---|
| MINIMUM | Header data and totals only — not a complete invoice under EN 16931 | Accounting hand-off in France |
| BASIC WL | Header plus VAT breakdown, no lines | Same, with tax detail |
| BASIC | Adds invoice lines | The common minimum for exchange |
| EN 16931 (COMFORT) | Full EN 16931 conformance | The interoperable default |
| EXTENDED | Adds elements beyond EN 16931 | Bilateral, sector-specific needs |

The profile is declared in `BT-24` (`ExchangedDocumentContext/GuidelineSpecifiedDocumentContextParameter/ID`)
and selects both the mapping and the rule set.

## Official sources

| Source | Use it for |
|---|---|
| <https://fnfe-mpe.org> | Specification, profile XSD and Schematron, official samples. |
| <https://www.ferd-net.de> | The German publication of the same standard. |
| ISO 19005-3 | PDF/A-3 conformance. Sold by ISO. |
| ISO 16684-1 | XMP metadata. Sold by ISO. |

## Artefacts

`specs/facturx/` — profile schemas and official samples (redistributable). The specification PDF is not
committed.

## Model mapping

Factur-X is CII plus a container. `International.EInvoicing.FacturX` owns the profile definitions and the
hybrid orchestration; `International.EInvoicing.Cii` owns the XML. The PDF side sits behind an abstraction so
the choice of PDF library stays the consumer's, with a PDFsharp implementation shipped as the default.

The embedded file must be named exactly `factur-x.xml` (ZUGFeRD 2.x also accepts `zugferd-invoice.xml` for
compatibility), declared with the `Alternative` relationship, and described in the XMP metadata with the
Factur-X extension schema naming the profile.

## Validation

XSD of the profile, then the profile Schematron, then EN 16931 rules for profiles that claim conformance,
then PDF/A-3 conformance of the container.

## Pitfalls

- **A PDF/A-3 file is not just a PDF with an attachment.** Missing XMP metadata, a wrong relationship, or a
  non-conforming colour space all break conformance while the XML looks perfect.
- **The XML and the PDF must agree.** Nothing enforces it technically; a generator that renders totals
  separately from the XML will eventually disagree with itself. Render from the same model.
- **MINIMUM and BASIC WL are not EN 16931 invoices** and must not be validated as if they were. In France
  they are legally usable only in specific cases.
- **Incoming PDFs are hostile input.** Extraction never executes embedded JavaScript, never follows external
  references, and bounds the decoded attachment size.

## Reference implementations

- [mustangproject](https://github.com/ZUGFeRD/mustangproject) — the reference implementation.
- [horstoeko/zugferd](https://github.com/horstoeko/zugferd) — excellent profile/builder ergonomics.
- [akretion/factur-x](https://github.com/akretion/factur-x) — compact, readable PDF handling.
