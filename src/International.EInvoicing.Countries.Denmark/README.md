# International.EInvoicing.Countries.Denmark

What Danish electronic invoicing adds on top of the norms.

Denmark exchanges **Peppol BIS Billing** over NemHandel, with Danish rules that travel inside the Peppol rule
set. So this package builds on `International.EInvoicing.Peppol`, and what lives here is genuinely national:
the **CVR number** in the schemes Peppol reserves for it, and the payment means codes Denmark accepts.

```csharp
DanishEInvoicing danmark = DanishEInvoicing.Create();

EInvoice invoice = danmark.Invoice()                     // Peppol BIS Billing, UBL, DKK
    .WithNumber("2026-0001")
    .IssuedOn(new DateOnly(2026, 9, 1))
    .WithBuyerReference("REF-2026-0001")                 // BT-10: Peppol requires it, EN 16931 does not
    .From(seller => danmark.Describe(seller, "DK12345670", "Leverandør ApS"))
    .To(buyer => buyer.Named("Kunde A/S"))
    .AddLine(line => line.WithItem("Rådgivning").WithNetAmount(3000m).WithVat("S", 25m))
    .WithComputedVatBreakdown()
    .WithComputedTotals()
    .Build();
```

**The trap worth knowing about.** Payment means code `30`, plain credit transfer, is perfectly valid
EN 16931 and is refused by `DK-R-005` between two Danish parties. `DkPaymentMeans` carries the codes Denmark
does accept, taken from the rule itself:

```csharp
invoice.Payment!.MeansTypeCode = DkPaymentMeans.SepaCreditTransfer;   // 58
```

Two things this package does not carry, both by choice. **OIOUBL 2.1**, the national format still used
domestically, is its own syntax rather than a profile of EN 16931. And **NemHandel BIS 4**, which the Danish
Business Authority committed to in March 2026 as the single domestic format by 2029, is built on Peppol BIS 4
and EN 16931-1:2026 — neither of which is published.

Full documentation: <https://github.com/Epic-Dice-Studio/International.EInvoicing>
