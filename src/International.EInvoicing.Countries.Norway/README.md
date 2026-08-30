# International.EInvoicing.Countries.Norway

What Norwegian electronic invoicing adds on top of the norms.

Norway exchanges **EHF 3.0**, a CIUS of Peppol BIS Billing, which is itself a CIUS of EN 16931 — so this
package builds on `International.EInvoicing.Peppol` rather than restating it. What is genuinely national
lives here: the profile identifier that declares EHF, and the **organisasjonsnummer** with the modulo 11
check Peppol enforces on scheme 0192.

```csharp
NorwegianEInvoicing norge = NorwegianEInvoicing.Create();

EInvoice invoice = norge.Invoice()                       // EHF 3.0, UBL, NOK, Peppol's business process
    .WithNumber("2026-0001")
    .IssuedOn(new DateOnly(2026, 9, 1))
    .WithBuyerReference("REF-2026-0001")                 // BT-10: Peppol requires it, EN 16931 does not
    .From(seller => norge.Describe(seller, "915 442 552", "Leverandør AS"))
    .To(buyer => buyer.Named("Kunde AS"))
    .AddLine(line => line.WithItem("Rådgivning").WithNetAmount(3000m).WithVat("S", 25m))
    .WithComputedVatBreakdown()
    .WithComputedTotals()
    .Build();
```

`Describe` checks the organisation number before writing it, and puts it in the scheme Peppol reserves for
it. The Norwegian validation rules travel inside the Peppol rule set, so `AddPeppolRulesFrom(directory)`
brings both.

Full documentation: <https://github.com/Epic-Dice-Studio/International.EInvoicing>
