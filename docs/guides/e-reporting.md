# E-reporting

What the French reform asks for **alongside** invoicing: a report to the tax administration of what invoicing
does not carry — sales to consumers, transactions with parties abroad, and when the money actually arrived.

It is a different document from an invoice. Not UBL, not CII, no XML namespace at all, and its own codes.
The DGFiP calls it *flux 10*.

## Two kinds of transmission, never mixed

| What you report | Flux | Entry point |
|---|---|---|
| Sales, invoice by invoice | 10.1 | `FrEReporting.Transactions(...).Invoice(...)` |
| Sales, totalled by day | 10.3 | `FrEReporting.Transactions(...).Day(...)` |
| Payments against an invoice | 10.2 | `FrEReporting.Payments(...).ForInvoice(...)` |
| Payments with no invoice behind them | 10.4 | `FrEReporting.Payments(...).ForTransactions(...)` |

A transmission reports transactions **or** payments. The rules reject one carrying both, and one carrying
neither, which is why they are two entry points rather than a flag.

## A day of counter sales

```csharp
using International.EInvoicing.Countries.France.EReporting;
using International.EInvoicing.Countries.France.EReporting.Building;
using International.EInvoicing.Countries.France.EReporting.Writing;

FrEReport report = FrEReporting
    .Transactions(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30))
    .From("0003", "PA-E Vendeur")          // the platform transmitting
    .For("100000009", "VENDEUR")           // the company being reported on
    .Day(new DateOnly(2026, 9, 1), FrEReportCodes.RetailTransactions, split => split
        .At(20m, 1000m)                    // €1000 at 20 %, VAT worked out
        .At(5.5m, 200m))
    .Counting(42)
    .Build();

string xml = new FrEReportWriter().WriteToString(report);
```

The totals are added up from the split rather than asked for separately: the published rules check that they
agree, and two numbers you have to keep consistent are two numbers that drift apart.

`At(rate, taxable)` works the VAT out and rounds it to the cent. Where you already have the figure — because
your ledger has it — pass it: `At(rate, taxable, tax)`.

## An invoice to a buyer abroad

```csharp
FrEReport report = FrEReporting
    .Transactions(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30))
    .From("0003", "PA-E Vendeur")
    .For("100000009", "VENDEUR")
    .Invoice(invoice => invoice
        .Numbered("F202600001", new DateOnly(2026, 9, 4))
        .InProcess("B1")
        .DueOn(new DateOnly(2026, 10, 4))
        .SoldBy("100000009", "FR32100000009")
        .BoughtAbroadBy("0223", "DE811569869", "DE", vatNumber: "DE811569869")
        .Taxed(20m, 1000m, 200m))
    .Build();
```

A buyer identified by SIREN (`0002`) or by a foreign registration (`0223`) must carry a VAT number; the rules
say so, and the parameter is there for it.

Where nothing is charged, the reason is not optional:

```csharp
.Exempt(1000m, "VATEX-EU-IC", "Livraison intracommunautaire")
```

Anything the builder does not cover — delivery addresses, lines, document-level discounts — is on the model,
reachable without leaving the builder:

```csharp
.Extend(invoice => invoice.Deliveries.Add(new FrReportedDelivery
{
    Date = new DateOnly(2026, 9, 3),
    Location = new FrPostalLocation { CityName = "Berlin", CountryCode = "DE" },
}))
```

## Payments

For services, VAT is due when payment is collected rather than when the invoice is issued, which is why
collection is reported separately from the sale.

```csharp
FrEReport report = FrEReporting
    .Payments(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30))
    .From("0003", "PA-E Vendeur")
    .For("100000009", "VENDEUR")
    .ForInvoice("F202600001", new DateOnly(2026, 9, 4), paidOn: new DateOnly(2026, 9, 20),
        split => split.At(20m, 1200m))
    .ForTransactions(new DateOnly(2026, 9, 21), split => split.At(5.5m, 211m))
    .Build();
```

A payment reports the amount **collected** at each rate — not a taxable base and a tax.

## Correcting a transmission

A transmission that replaces an earlier one for the same period says so:

```csharp
.Transmission(transmission => transmission.Replacing().WithIdentifier("100000009-202609-RE-1"))
```

Without `WithIdentifier`, one is derived from the company, the period and the kind of transmission.

## Reading one back

```csharp
ParseResult<FrEReport> result = new FrEReportReader().Read(stream);

foreach (FrTransactionSummary day in result.Value!.Transactions!.Summaries)
{
    Console.WriteLine($"{day.Date.Value}: {day.TaxExclusiveAmount.Value} + {day.TaxAmount.Value} VAT");
}
```

As everywhere else here, the reader reports rather than throws: a value it cannot interpret keeps its raw
text and says why, and an element the model does not describe is kept as extension data on the node that
carried it.

```csharp
day.Date.HasValue;        // false
day.Date.Raw;             // "1 septembre"
day.Date.Diagnostic;      // EIV2001, with what was expected
```

## Checking a transmission

The DGFiP publishes Schematron for flux 10 and no sample transmissions, so the documents this library builds
are measured against the rules themselves:

```bash
build/fetch-specs.sh france
```

```csharp
SchematronRuleSet rules = SchematronRuleSet.Load(
    File.ReadAllText("specs/fr-dse/rules/flux10/1.0/PPF_Flux10_v1_0.sch"), "PPF Flux 10", "1.0");

ValidationReport report = new SchematronValidator().Validate(xml, rules);
```

Those rules name themselves in the message rather than in an attribute — `[G2.33]`, `[G8.01]` — and the
report picks the code up, so a message says which rule failed rather than "(unnamed)".

## The codes

`FrEReportCodes` carries the closed lists: transaction categories, VAT rates, VAT categories, invoice types,
invoicing frameworks, and the codes saying when VAT becomes chargeable. They are there to choose from; the
published rules remain the authority.

## Next

- [The France standards page](../standards/country-fr.md)
- [Lifecycle statuses](lifecycle.md)
- [Validating a document](validation.md)

## Run it

[`samples/International.EInvoicing.Samples/Chapters/FrenchEReporting.cs`](../../samples/International.EInvoicing.Samples/Chapters/FrenchEReporting.cs) is this page as code — transactions and payments, both flux.

```bash
dotnet run --project samples/International.EInvoicing.Samples
```
