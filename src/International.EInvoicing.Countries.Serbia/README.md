# International.EInvoicing.Countries.Serbia

What Serbian electronic invoicing adds on top of the norms.

Serbia exchanges **SRBDT**, its CIUS of EN 16931, over the **SEF** — *Sistem e-Faktura* — where electronic
invoicing has been mandatory since 2023. This package carries the CIUS and its conformant extension, and runs
the 134 assertions Serbia publishes.

```csharp
EInvoicing library = EInvoicing.Create(serbia => serbia
    .AddDefaults()
    .AddSerbia()
    .AddSerbianRulesFrom("specs/national/serbia/schematron"));   // build/fetch-specs.sh national

EInvoice invoice = EInvoiceBuilder.Create(RsProfiles.SrbdtUbl)
    .WithNumber("2026-0001")
    .IssuedOn(new DateOnly(2026, 9, 1))
    .InCurrency("RSD")
    .Extend(document => document.TaxPointDateCode = "35")        // RSR-05 requires it
    // …
    .Build();
```

**The trap.** `RSR-05` requires the **tax point date code** (BT-8), which EN 16931 leaves optional and most
countries never ask for. In UBL it lives inside `cac:InvoicePeriod` as its description code, sharing the
element with the invoicing period — so an invoice may carry the code with no period dates at all. This
library did not write BT-8 at all until Serbia's rules asked for it.

`RsProfiles.SrbdtExtensionUbl` is the conformant extension, for invoices that go beyond the norm. Both
identifiers are read from the published rule set rather than transcribed.

The Serbian rules are fetched, not shipped: `build/fetch-specs.sh national`.

Full documentation: <https://github.com/Epic-Dice-Studio/International.EInvoicing>
