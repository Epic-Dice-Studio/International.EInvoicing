# UBL 2.1 — OASIS Universal Business Language

## Scope and version

UBL 2.1 is one of the two syntaxes that carry EN 16931 (binding EN 16931-3-2). Two document types matter for
invoicing: `Invoice` and `CreditNote`. UBL is the syntax used by Peppol, and therefore by Belgium, the
Netherlands, the Nordics, Australia and New Zealand.

We target **UBL 2.1** (OASIS Standard, 2013). UBL 2.4 exists but no European mandate requires it.

## Official sources

| Source | Use it for |
|---|---|
| <https://docs.oasis-open.org/ubl/os-UBL-2.1/> | The schemas and the specification. |
| EN 16931-3-2 | The normative BT → XPath binding. |
| <https://docs.peppol.eu/poacc/billing/3.0/> | Readable examples of correct UBL, with rules. |

## Artefacts

`specs/ubl-2.1/` — `maindoc/UBL-Invoice-2.1.xsd`, `maindoc/UBL-CreditNote-2.1.xsd` and the `common/` schemas.
Downloaded manually (published as an archive, not a repository); redistributable.

## Model mapping

UBL splits names across two namespaces: `cbc:` for leaf values, `cac:` for aggregate components. The reader
and writer address elements by qualified name, never by local name alone — several `cbc:ID` elements exist
with entirely different meanings depending on their parent.

Attributes that must be preserved on the field, not discarded:

| UBL attribute | Field type |
|---|---|
| `currencyID` | `AmountField` |
| `unitCode` | `QuantityField` |
| `schemeID`, `schemeAgencyID` | `IdentifierField` |
| `listID`, `listVersionID` | `CodeField` |
| `languageID` | `TextField` |
| `mimeCode`, `filename` | `BinaryField` |

## Validation

XSD first, then the EN 16931 UBL Schematron, then any CIUS (Peppol BIS, national rules).

## Pitfalls

- **Element order is normative.** The XSD declares sequences; a writer that emits the right elements in the
  wrong order produces a document that fails XSD validation at the recipient. This is the single most common
  cause of rejected invoices, and the reason serialisation is hand-written here.
- **`cbc:ID` is context-dependent.** Resolve by path, never by name.
- **Decimal formatting.** Always invariant culture, always a period as decimal separator, never a thousands
  separator, and never scientific notation for large quantities.
- **Empty elements are not the same as absent elements.** An empty `cbc:Note` is a present-but-blank note and
  some receivers reject it. Absence must round-trip as absence, which `Field<T>.IsSet` makes explicit.
- **Credit notes are not invoices with negative amounts.** Amounts stay positive; the document type changes.

## Reference implementations

- [peppol-commons](https://github.com/phax/peppol-commons) — identifier and code list handling.
- [horstoeko/zugferd](https://github.com/horstoeko/zugferd) — for the profile-driven builder shape.
