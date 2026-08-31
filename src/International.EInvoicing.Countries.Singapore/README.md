# International.EInvoicing.Countries.Singapore

What Singaporean electronic invoicing adds on top of the norms.

Singapore exchanges **InvoiceNow**, which runs on **Peppol PINT** — `urn:peppol:pint:billing-1@sg-1`, not
BIS Billing — with a business process of its own. This package declares both, invoices in SGD, and taxes in
**GST**.

```csharp
SingaporeEInvoicing singapore = SingaporeEInvoicing.Create();

EInvoice invoice = singapore.Invoice()                   // PINT @sg-1, urn:peppol:bis:billing, SGD, GST
    .WithNumber("2026-0001")
    .IssuedOn(new DateOnly(2026, 9, 1))
    .Extend(document => document.DocumentUuid = Guid.NewGuid().ToString())   // BR-108-GST-SG wants one
    .From(seller => seller
        .Named("Supplier Pte Ltd")
        .WithLegalRegistration("201912345A"))                                // BR-112-GST-SG wants one
    .To(buyer => buyer.Named("Customer Pte Ltd"))
    .AddLine(line => line.WithItem("Consulting").WithNetAmount(1000m).WithVat(SgTaxCategory.StandardRated, 9m))
    .WithComputedVatBreakdown()
    .WithComputedTotals()
    .Build();
```

**Three traps, all of them fatal rules.**

- **`S` is not a Singapore tax category.** The code every European example uses is rejected by
  `BR-CL-17-GST-SG`. Singapore's standard-rated code is `SR`; `SgTaxCategory` carries the whole list, read
  out of the rule itself.
- **A document UUID is required** (`BR-108-GST-SG`), which EN 16931 has no term for.
- **The supplier needs a legal entity registration** (`BR-112-GST-SG`), not just a name and a tax number.

**No `Describe` here, deliberately.** Every other country package writes its legal identifier into the scheme
that country's rules name. Singapore's rules name none — they constrain arithmetic, decimals and GST
terminology — and the EAS code list this library ships carries codes without meanings. Guessing a scheme
identifier is as wrong as guessing a profile, so the choice stays with you.

The Singaporean rules are fetched, not shipped: `build/fetch-specs.sh pint`, then
`AddPeppolPintRulesFrom("specs/peppol/pint/schematron")`.

Full documentation: <https://github.com/Epic-Dice-Studio/International.EInvoicing>
