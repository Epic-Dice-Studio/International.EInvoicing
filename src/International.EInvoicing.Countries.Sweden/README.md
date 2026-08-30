# International.EInvoicing.Countries.Sweden

What Swedish electronic invoicing adds on top of the norms.

Sweden exchanges **Peppol BIS Billing** itself rather than a national format; what it adds is a set of
national rules that travel inside the Peppol rule set. So this package builds on
`International.EInvoicing.Peppol`, and what lives here is genuinely national: the **organisationsnummer**
with the Luhn check Peppol enforces on scheme 0007.

```csharp
SwedishEInvoicing sverige = SwedishEInvoicing.Create();

EInvoice invoice = sverige.Invoice()                     // Peppol BIS Billing, UBL, SEK
    .WithNumber("2026-0001")
    .IssuedOn(new DateOnly(2026, 9, 1))
    .WithBuyerReference("REF-2026-0001")                 // BT-10: Peppol requires it, EN 16931 does not
    .From(seller => sverige.Describe(seller, "556000-0009", "Leverantör AB"))
    .To(buyer => buyer.Named("Kund AB"))
    .AddLine(line => line.WithItem("Rådgivning").WithNetAmount(3000m).WithVat("S", 25m))
    .WithComputedVatBreakdown()
    .WithComputedTotals()
    .Build();
```

`Describe` checks the organisation number before writing it, and puts it in the scheme Peppol reserves for
it. The Swedish rules travel inside the Peppol rule set, so `AddPeppolRulesFrom(directory)` brings both.

Full documentation: <https://github.com/Epic-Dice-Studio/International.EInvoicing>
