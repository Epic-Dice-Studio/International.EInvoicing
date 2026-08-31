# International.EInvoicing.Countries.Greece

What Greek electronic invoicing adds on top of the norms.

Greece exchanges Peppol BIS Billing, with Greek rules that travel inside the Peppol rule set — and they ask
for two things nothing else in this library does.

**The invoice number is a compound key.** `GR-R-001` is fatal: when the supplier is Greek, BT-1 is six
segments separated by `|`, and every one of them is checked against the rest of the document.

```csharp
string number = GrInvoiceNumber.For(
    supplierTaxIdentifier: "100000003",     // must satisfy its checksum and match the seller's VAT number
    issueDate: new DateOnly(2026, 9, 1),    // must be the same date as BT-2, written DD/MM/YYYY
    branch: 0,
    documentType: "1.1",                    // one of six myDATA codes
    series: "A",
    number: "0001");
```

An ordinary invoice number is rejected outright, and a hand-built string fails for reasons that are hard to
read off a validation report — so `For` checks each part where it can, and names the rule when it refuses.

**The AFM has a checksum of its own.** The first eight digits are weighted by descending powers of two —
256, 128, 64, 32, 16, 8, 4, 2 — and the ninth is the sum modulo 11, modulo 10. `GrTaxIdentifier` verifies it,
and writes the number in scheme `9933`.

**And one MARK number.** `GR-R-004-1` requires exactly one additional document reference described as
`##M.AR.K##`, carrying the myDATA registration:

```csharp
invoice.AdditionalDocuments.Add(new AdditionalDocument
{
    Identifier = "400001234567890",
    Description = "##M.AR.K##",
});
```

**myDATA itself is out of scope.** Reporting every invoice to the platform is a transmission, not a document,
and this library performs no network I/O. What it does is produce the document myDATA expects to hear about.

The Greek rules are inside the Peppol rule set: `build/fetch-specs.sh peppol`, then `AddPeppolRulesFrom(...)`.

Full documentation: <https://github.com/Epic-Dice-Studio/International.EInvoicing>
