# International.EInvoicing.Validation.XRechnung

The XRechnung Schematron rules for UBL and CII, embedded and ready to run.

```csharp
ValidationReport report = new SchematronValidator()
    .Validate(xml, XRechnungRules.For(DocumentSyntax.Ubl));
```

XRechnung is a CIUS of EN 16931, so both rule sets apply: run this **and**
`International.EInvoicing.Validation.En16931`, and combine the reports so the result says both ran.

The artefacts are redistributed verbatim under Apache-2.0, which ships with the package.

Full documentation: <https://github.com/Epic-Dice-Studio/International.EInvoicing>
