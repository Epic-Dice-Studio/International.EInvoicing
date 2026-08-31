# International.EInvoicing.Countries.Japan

What Japanese electronic invoicing adds on top of the norms.

Japan is on **Peppol PINT** — `urn:peppol:pint:billing-1@jp-1` — with a business process of its own. Its
rules are lighter than its neighbours', with one requirement that catches people.

```csharp
JapaneseEInvoicing japan = JapaneseEInvoicing.Create();

EInvoice invoice = japan.Invoice()                       // PINT @jp-1, urn:peppol:bis:billing, JPY
    .WithNumber("2026-0001")
    .IssuedOn(new DateOnly(2026, 9, 1))
    .Extend(document => document.Period = new InvoicingPeriod   // aligned-ibrp-052 requires one
    {
        StartDate = new DateOnly(2026, 9, 1),
        EndDate = new DateOnly(2026, 9, 30),
    })
    .From(seller => japan.Describe(seller, "T1234567890123", "供給者株式会社"))
    .To(buyer => japan.Describe(buyer, "T9876543210987", "顧客株式会社"))
    .AddLine(line => line.WithItem("コンサルティング").WithNetAmount(1000m).WithVat("S", 10m))
    .WithComputedVatBreakdown()
    .WithComputedTotals()
    .Build();
```

**The trap.** `aligned-ibrp-052` requires an **invoice period** or a line period. EN 16931 leaves both
optional, so an invoice that is valid everywhere else is refused in Japan.

Japan's registration number — the one the qualified invoice system turns on — travels as BT-31, the term
EN 16931 gives it. No check digit is verified: the Japanese rules constrain how often it may appear, not its
shape, and this library does not invent validation its sources do not define.

Japan's rules also still accept the older `urn:fdc:peppol:jp:billing:3.0` and either family's business
process, which is unusual — most jurisdictions accept one. This package writes the PINT pair.

The Japanese rules are fetched, not shipped: `build/fetch-specs.sh pint`, then
`AddPeppolPintRulesFrom("specs/peppol/pint/schematron")`.

Full documentation: <https://github.com/Epic-Dice-Studio/International.EInvoicing>
