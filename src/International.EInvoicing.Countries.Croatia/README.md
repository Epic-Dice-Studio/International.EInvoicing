# International.EInvoicing.Countries.Croatia

What Croatian electronic invoicing adds on top of the norms.

Croatia's **Fiskalizacija 2.0** mandate has been live for domestic B2B since **1 January 2026**. Invoices are
UBL 2.1 exchanged over a five-corner Peppol-style network, so this package builds on
`International.EInvoicing.Peppol`. What lives here is the thing a Croatian invoice cannot be written without:
the **OIB** of *both* parties.

```csharp
CroatianEInvoicing hrvatska = CroatianEInvoicing.Create();

EInvoice invoice = hrvatska.Invoice()
    .WithNumber("2026-0001")
    .IssuedOn(new DateOnly(2026, 9, 1))
    .WithBuyerReference("REF-2026-0001")
    .From(seller => hrvatska.Describe(seller, "69435151530", "Dobavljač d.o.o."))
    .To(buyer => hrvatska.Describe(buyer, "12345678903", "Kupac d.o.o."))
    .AddLine(line => line.WithItem("Savjetovanje").WithNetAmount(3000m).WithVat("S", 25m))
    .WithComputedVatBreakdown()
    .WithComputedTotals()
    .Build();
```

`Describe` checks the OIB against ISO/IEC 7064 MOD 11,10 before writing it, derives the VAT number from it,
and puts it where both the legal registration and the electronic address are read from.

**What this package does not do**, and cannot:

- **The advanced electronic seal.** Every Croatian invoice must carry one, backed by an OIB-linked
  certificate from an approved Croatian trust service provider. Signing is not in this library's scope.
- **The fiscalisation messages.** Issuer and recipient each report to the tax administration in near real
  time. That is transport, which is permanently out of scope.
- **The HR-FISK 2.0 CIUS identifier.** It is published nowhere this repository can read, and a guessed
  identifier in BT-24 makes every document written with it wrong. Register it from your own code and it wins.
- **The six-digit KPD classification code** every line must carry. Set it yourself on `Item.ClassificationCodes`
  with the scheme your intermediary requires.

See [docs/standards/country-hr.md](https://github.com/Epic-Dice-Studio/International.EInvoicing/blob/main/docs/standards/country-hr.md).

Full documentation: <https://github.com/Epic-Dice-Studio/International.EInvoicing>
