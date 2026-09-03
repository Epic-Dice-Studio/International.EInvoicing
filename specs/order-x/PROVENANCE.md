# Order-X 1.0

| | |
|---|---|
| **Source** | <https://fnfe-mpe.org>, <https://www.ferd-net.de> — carried by <https://github.com/ZUGFeRD/mustangproject> |
| **Version** | Order-X 1.0, on UN/CEFACT SCRDM CCBDA CIO D20B |
| **Retrieved** | *(fetched locally; not committed)* |
| **Licence** | FNFE-MPE / FeRD |
| **Redistributable** | no — their own package is registration-walled |

Order-X is the Franco-German order, order change and order response, in the same CII family as Factur-X and
by the same two publishers. Its root is `rsm:SCRDMCCBDACIOMessageStructure`, not `rsm:CrossIndustryInvoice`:
a different UN/CEFACT message, so the invoice schemas in `specs/cii-d22b` do not cover it.

`build/fetch-specs.sh order-x` fills:

- `schema/{basic,comfort,extended}` — the three profile XSDs
- `schematron/{basic,comfort,extended}` — the three rule sets, in source form
- `examples/` — the one published reference document, and the hybrid PDF carrying it

The three profiles are BASIC, COMFORT and EXTENDED, identified by
`urn:order-x.eu:1p0:{basic,comfort,extended}` in
`ExchangedDocumentContext/GuidelineSpecifiedDocumentContextParameter/ID`.

Nothing here is committed. The tests that need it skip when it is absent, which is also how CI runs.
