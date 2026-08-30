# International.EInvoicing.Countries.Iceland

What Icelandic electronic invoicing adds on top of the norms.

Iceland exchanges **Peppol BIS Billing**, with Icelandic rules that travel inside the Peppol rule set. So
this package builds on `International.EInvoicing.Peppol`, and what lives here is the **kennitala** and the
place the Icelandic rules insist on finding it.

`IS-R-002` and `IS-R-004` are fatal: both parties need a legal entity identifier carrying scheme `0196`.

```csharp
IcelandicEInvoicing island = IcelandicEInvoicing.Create();

EInvoice invoice = island.Invoice()                      // Peppol BIS Billing, UBL, ISK
    .WithNumber("2026-0001")
    .IssuedOn(new DateOnly(2026, 9, 1))
    .WithBuyerReference("REF-2026-0001")                 // BT-10: Peppol requires it, EN 16931 does not
    .From(seller => island.Describe(seller, "120000-0350", "Seljandi ehf"))
    .To(buyer => island.Describe(buyer, "120111-1250", "Kaupandi ehf"))
    .AddLine(line => line.WithItem("Ráðgjöf").WithNetAmount(3000m).WithVat("S", 24m))
    .WithComputedVatBreakdown()
    .WithComputedTotals()
    .Build();
```

`Describe` verifies the kennitala's modulo 11 check digit before writing it. The date part is not
interpreted: kennitölur issued to businesses add 40 to the day, and the library has no business deciding
which of the two a caller meant.

Full documentation: <https://github.com/Epic-Dice-Studio/International.EInvoicing>
