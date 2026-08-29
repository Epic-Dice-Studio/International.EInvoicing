# International.EInvoicing.Validation.En16931

The EN 16931 Schematron rules for UBL and CII, embedded and ready to run.

```csharp
ValidationReport report = En16931Rules.For(DocumentSyntax.Ubl).Validate(invoiceXml);

if (!report.IsConforming)
{
    foreach (ValidationMessage message in report.OfAtLeast(RuleSeverity.Error))
    {
        Console.WriteLine(message);   // BR-CO-14  Error  … at /Invoice/TaxTotal
    }
}
```

The artefacts are redistributed verbatim under their own licence, **EUPL-1.2**, which ships with the package
as `EN16931-LICENSE.txt`. They are executed as data, not translated into code, so replacing the file replaces
the rules.

Full documentation: <https://github.com/Epic-Dice-Studio/International.EInvoicing>
