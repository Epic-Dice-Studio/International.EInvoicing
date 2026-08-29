# UN/CEFACT CII D22B — Cross Industry Invoice

## Scope and version

CII is the second syntax carrying EN 16931 (binding EN 16931-3-3). It is the syntax of Factur-X / ZUGFeRD, and
one of the two syntaxes accepted for XRechnung and in France.

We target the **D22B** release, which is the version referenced by Factur-X 1.07.x and by the current EN 16931
artefacts.

## Official sources

| Source | Use it for |
|---|---|
| <https://unece.org/trade/uncefact/xml-schemas> | Schemas and the underlying data type modules. |
| EN 16931-3-3 | The normative BT → XPath binding. |
| <https://fnfe-mpe.org> | Factur-X profile schemas, which are CII subsets with worked examples. |

## Artefacts

`specs/cii-d22b/` — `CrossIndustryInvoice_100pD22B.xsd` and the `ram`, `qdt` and `udt` modules. Downloaded
manually; redistributable.

## Model mapping

CII has three top-level sections under `rsm:CrossIndustryInvoice`:

| Section | Carries |
|---|---|
| `ExchangedDocumentContext` | The profile identifier (`BT-24`) and test indicator. Read this **first**: it selects the profile and therefore the rules. |
| `ExchangedDocument` | Document-level terms: number, type code, issue date, notes. |
| `SupplyChainTradeTransaction` | Everything else, split into `Agreement`, `Delivery` and `Settlement`. |

The `udt` module is exactly the set of unqualified data types our `Field<T>` family mirrors — Amount,
BinaryObject, Code, DateTime, Identifier, Indicator, Quantity, Text. The mapping is deliberate: it is what
lets a field keep its `format`, `unitCode`, `currencyID` or `schemeID` attribute without special-casing.

## Validation

XSD, then the EN 16931 CII Schematron, then the profile's own Schematron (Factur-X, XRechnung CII, Peppol CII).

## Pitfalls

- **Dates carry a format code.** `udt:DateTimeString` has a `format` attribute, almost always `102`
  (`CCYYMMDD`), sometimes `610` or `616`. Parsing a date without honouring the attribute is wrong, and
  dropping the attribute on write is lossy. This is exactly what `DateField.FormatCode` preserves.
- **`IncludedSupplyChainTradeLineItem` ordering** matters, as does element order everywhere else.
- **The profile identifier drives everything.** A document declaring MINIMUM must not be validated against
  EN 16931 rules it deliberately omits.
- **Amounts appear at several levels** — line, allowance/charge, tax breakdown, document — and the `BR-CO-*`
  rules tie them together. Compute once, in the canonical model.
- **Namespace prefixes vary** between issuers (`ram`, `rsm`, `udt` are conventions, not requirements). Bind by
  namespace URI, never by prefix.

## Reference implementations

- [mustangproject](https://github.com/ZUGFeRD/mustangproject) — the reference for CII in practice.
- [ZUGFeRD-csharp](https://github.com/stephanstapel/ZUGFeRD-csharp) — useful catalogue of CII edge cases.
- [drafthorse](https://github.com/pretix/drafthorse) — compact, readable CII model.
