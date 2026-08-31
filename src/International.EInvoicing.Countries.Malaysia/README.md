# International.EInvoicing.Countries.Malaysia

What Malaysian electronic invoicing adds on top of the norms.

Malaysia's **MyInvois** exchanges **Peppol PINT** — `urn:peppol:pint:billing-1@my-1`, not BIS Billing — with
a business process of its own. This package declares both and invoices in MYR.

```csharp
MalaysianEInvoicing malaysia = MalaysianEInvoicing.Create();

EInvoice invoice = malaysia.Invoice()                    // PINT @my-1, urn:peppol:bis:billing, MYR
    .WithNumber("2026-0001")
    .IssuedOn(new DateOnly(2026, 9, 1))
    .From(seller => malaysia.Describe(seller, "202001234567", "Pembekal Sdn Bhd", "C12345678901"))
    .To(buyer => malaysia.Describe(buyer, "202101234567", "Pelanggan Sdn Bhd"))
    .AddLine(line => line.WithItem("Perundingan").WithNetAmount(1000m).WithVat(MyTaxCategory.SalesTax, 10m))
    .WithComputedVatBreakdown()
    .WithComputedTotals()
    .Build();
```

**Two traps, both fatal rules.**

- **Three identifiers EN 16931 treats as optional are mandatory here.** `ibr-02-my` and `ibr-03-my` want the
  **BRN** of *both* parties, and `ibr-04-my` wants the supplier's **TIN** as well. `Describe` puts the BRN in
  the legal registration (BT-30, BT-47) and the TIN in the tax registration (BT-32), which is written under a
  scheme other than VAT — which is where the Malaysian rule looks for it.
- **`S` is not a Malaysian tax category.** The standard code is `SA`, and the list includes entries with no
  European equivalent at all: high-value goods, low-value goods, tourism tax. `MyTaxCategory` carries it, read
  out of `aligned-ibrp-cl-01-my` itself.

Submitting to LHDN is a national API — transport, and permanently out of scope here. The document is PINT,
and the document is what this library does.

The Malaysian rules are fetched, not shipped: `build/fetch-specs.sh pint`, then
`AddPeppolPintRulesFrom("specs/peppol/pint/schematron")`.

Full documentation: <https://github.com/Epic-Dice-Studio/International.EInvoicing>
