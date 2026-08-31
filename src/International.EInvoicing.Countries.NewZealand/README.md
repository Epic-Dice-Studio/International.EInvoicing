# International.EInvoicing.Countries.NewZealand

What New Zealand electronic invoicing adds on top of the norms.

New Zealand exchanges **Peppol PINT**, not BIS Billing, and shares Australia's `@aunz-1` specialisation —
one Peppol authority across the Tasman. The document is the same as Australia's; the identifier is not.

```csharp
NewZealandEInvoicing newZealand = NewZealandEInvoicing.Create();

EInvoice invoice = newZealand.Invoice()                  // PINT @aunz-1, urn:peppol:bis:billing, NZD
    .WithNumber("2026-0001")
    .IssuedOn(new DateOnly(2026, 9, 1))
    .WithBuyerReference("REF-2026-0001")
    .From(seller => newZealand.Describe(seller, "9429040009597", "Supplier Ltd"))
    .To(buyer => newZealand.Describe(buyer, "9429040001373", "Customer Ltd"))
    .AddLine(line => line.WithItem("Consulting").WithNetAmount(1000m).WithVat("S", 15m))
    .WithComputedVatBreakdown()
    .WithComputedTotals()
    .Build();
```

**The NZBN is a GLN.** It is issued as a GS1 Global Location Number, which is why Peppol routes it under
scheme `0088` rather than one of New Zealand's own, and why the check digit is the GS1 one. `Describe`
verifies it before writing it, and a test hands every number this library accepts — and a set it refuses —
to Peppol's own `PEPPOL-COMMON-R040` and fails on disagreement.

**The A-NZ rules do not run yet**, because OpenPEPPOL publishes PINT's artefacts as pre-compiled XSLT and
this library's engine executes Schematron. A document is read and mapped, and reported as *unchecked* rather
than passed. See
[docs/standards/peppol-pint.md](https://github.com/Epic-Dice-Studio/International.EInvoicing/blob/main/docs/standards/peppol-pint.md).

Full documentation: <https://github.com/Epic-Dice-Studio/International.EInvoicing>
