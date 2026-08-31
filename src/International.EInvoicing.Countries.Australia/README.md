# International.EInvoicing.Countries.Australia

What Australian electronic invoicing adds on top of the norms.

Australia is the first country in this library on **Peppol PINT** rather than Peppol BIS Billing. That
distinction is the whole point of the package: PINT is what Peppol publishes for jurisdictions outside
Europe, and it disagrees with BIS Billing about **both** the profile identifier and the business process. An
invoice with one right and the other wrong looks correct and is not.

```csharp
AustralianEInvoicing australia = AustralianEInvoicing.Create();

EInvoice invoice = australia.Invoice()                   // PINT @aunz-1, urn:peppol:bis:billing, AUD
    .WithNumber("2026-0001")
    .IssuedOn(new DateOnly(2026, 9, 1))
    .WithBuyerReference("REF-2026-0001")
    .From(seller => australia.Describe(seller, "51 824 753 556", "Supplier Pty Ltd"))
    .To(buyer => australia.Describe(buyer, "53 004 085 616", "Customer Pty Ltd"))
    .AddLine(line => line.WithItem("Consulting").WithNetAmount(1000m).WithVat("S", 10m))
    .WithComputedVatBreakdown()
    .WithComputedTotals()
    .Build();
```

`Describe` checks the **ABN** before writing it, in scheme `0151` where Peppol looks for it. The check is not
a trailing digit: all eleven digits are weighted and the sum must divide by 89, so a transposition anywhere
in the number is caught. A test hands every ABN this library accepts, and a set it refuses, to Peppol's own
`PEPPOL-COMMON-R050` and fails on disagreement.

**The A-NZ rules do not run yet.** OpenPEPPOL publishes PINT's artefacts as pre-compiled XSLT and this
library's engine executes Schematron, so a document is read and mapped with its jurisdiction rules reported
as *unchecked* rather than passed. See
[docs/standards/peppol-pint.md](https://github.com/Epic-Dice-Studio/International.EInvoicing/blob/main/docs/standards/peppol-pint.md).

Australia and New Zealand share one Peppol authority and one PINT specialisation, so a New Zealand invoice
uses this same profile — the NZBN is the identifier that differs.

Full documentation: <https://github.com/Epic-Dice-Studio/International.EInvoicing>
