# Getting started

## Install

```bash
dotnet add package International.EInvoicing
```

That one package brings UBL 2.1, UN/CEFACT CII, Factur-X profiles, lifecycle messages and EN 16931
validation. Install the individual packages instead if you want less — `International.EInvoicing.Ubl` alone,
say.

For hybrid PDFs, add the PDF half. It is separate so the choice of PDF library stays yours:

```bash
dotnet add package International.EInvoicing.FacturX.PdfSharp
```

## Read something

```csharp
using International.EInvoicing;

EInvoicing einvoicing = EInvoicing.CreateDefault();

DocumentResult result = einvoicing.Read(File.OpenRead("invoice.xml"));

if (result.Invoice is { } invoice)
{
    Console.WriteLine(invoice.Number.Value);          // BT-1
    Console.WriteLine(invoice.IssueDate.Value);       // BT-2, as a DateOnly
    Console.WriteLine(invoice.IssueDate.Raw);         // and exactly as the file wrote it
}
```

You did not say whether it was UBL or CII. You do not have to.

## Write something

```csharp
EInvoice invoice = EInvoiceBuilder.Create(KnownProfiles.En16931Cii)
    .WithNumber("FA-2026-001")
    .IssuedOn(new DateOnly(2026, 8, 30))
    .OfType("380")
    .InCurrency("EUR")
    .WithSeller(seller => seller.Named("Epic Dice Studio").WithVatIdentifier("FR12345678901"))
    .WithBuyer(buyer => buyer.Named("Acme"))
    .AddLine(line => line.WithIdentifier("1").WithItem("Consulting").WithNetAmount(450m).WithVat("S", 20m))
    .Build();

string xml = einvoicing.Write(invoice, DocumentFormat.Ubl);
```

## Check it

```csharp
ValidationReport report = einvoicing.Validate(xml);

Console.WriteLine(report);   // says what ran, then what failed
```

## With dependency injection

```csharp
services.AddEInvoicing(o => o
    .AddUbl()
    .AddCii()
    .AddFacturX()
    .AddCdar()
    .UseDiagnosticPreset(DiagnosticPreset.Balanced));

services.AddUblServices();
services.AddCiiServices();
services.AddEn16931Validation();
```

## The three promises, and where they show up

**Extensible without forking.** Register your own profile, reader, writer or rule set and it wins over the
one shipped. See the [recipes](../recipes/README.md).

**Nothing is lost, nothing explodes.** Every field keeps the text and attributes it came with
([raw values](raw-values.md)); readers report instead of throwing ([reading](reading.md)).

**Honest about its limits.** A profile with no rule set is reported as unchecked, never as passed
([validation](validation.md)).

## Where to go next

| | |
|---|---|
| [Reading a document](reading.md) | Streams, detection, diagnostics, limits |
| [Writing a document](writing.md) | Builders, credit notes, hybrid PDFs, extensions |
| [Lifecycle statuses](lifecycle.md) | French statuses, partner against public portal |
| [Validation](validation.md) | Rule sets, coverage, your own rules |
| [Raw values](raw-values.md) | Reaching the text behind any field |
| [Try it in the browser](https://epic-dice-studio.github.io/International.EInvoicing/demo/) | No install, nothing uploaded |
