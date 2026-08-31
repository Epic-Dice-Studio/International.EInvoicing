# International.EInvoicing.Validation.Xsd

Schema validation, offline.

A document can satisfy every business rule and still be a shape no receiver can parse. Element **order** and
**cardinality** are the schema's business, and no Schematron rule looks at them: this library once wrote two
bank accounts inside one `cac:PaymentMeans`, which UBL does not allow and which every rule set accepted.

```csharp
EInvoicing library = EInvoicing.Create(builder => builder
    .AddDefaults()
    .AddUblSchema());          // the OASIS UBL 2.1 schemas, embedded

ValidationReport report = library.Validate(xml);
```

The schemas are the OASIS UBL 2.1 originals, redistributed under the OASIS IPR Policy and embedded, so
validation needs nothing fetched and no network. CII documents are not covered yet: the UN/CEFACT D22B
package is published as an archive this repository does not yet carry.

Full documentation: <https://github.com/Epic-Dice-Studio/International.EInvoicing>
