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

**Invoicing in one country only?** There is a shorter way in: `FrenchEInvoicing`, `GermanEInvoicing` and
`BelgianEInvoicing` each know the profile, the business process and the rule sets their country expects —
see [one country, one object](country-shortcuts.md). Everything below still applies; they are built on it.

## Read something

```csharp
using International.EInvoicing;

EInvoicing einvoicing = EInvoicing.CreateDefault();

DocumentResult result = einvoicing.ReadFile("invoice.xml");

if (result.TryGetInvoice(out EInvoice? invoice))
{
    Console.WriteLine(invoice.Number.Value);          // BT-1
    Console.WriteLine(invoice.IssueDate.Value);       // BT-2, as a DateOnly
    Console.WriteLine(invoice.IssueDate.Raw);         // and exactly as the file wrote it
}
```

You did not say whether it was UBL or CII, or even whether it was an invoice. You do not have to.

Three ways to take the result, depending on what you already know:

```csharp
// You expect an invoice and would rather fail than branch:
EInvoice invoice = einvoicing.ReadFile("invoice.xml").RequireInvoice();

// You will handle whatever arrives:
var (kind, invoice, status) = einvoicing.Read(stream);

// It came off a network:
DocumentResult result = await einvoicing.ReadAsync(response.Content, cancellationToken);
```

## Write something

An invoice goes **from** a supplier **to** a customer, and reads that way:

```csharp
EInvoice invoice = EInvoiceBuilder.Create(KnownProfiles.En16931Ubl)
    .WithNumber("FA-2026-001")
    .IssuedOn(new DateOnly(2026, 8, 30))
    .InCurrency("EUR")
    .From("Epic Dice Studio", "FR12345678901")     // the seller
    .To("Acme", "FR44200000008")                   // the buyer
    .AddLine(line => line.WithIdentifier("1").WithItem("Consulting").WithNetAmount(450m).WithVat("S", 20m))
    .WithComputedVatBreakdown()                    // the VAT, grouped by rate
    .WithComputedTotals()                          // BT-106 … BT-115, from the lines
    .Build();

string xml = einvoicing.Write(invoice);
```

`From` and `To` take a full `PartyBuilder` too, when a name and a VAT number are not enough —
`From(seller => seller.Named("…").WithElectronicAddress("…", "0088"))` — and `WithSeller` / `WithBuyer` say
the same thing in the norm's own words.

**Let the totals be derived.** BR-CO-10 to BR-CO-17 are where documents most often stop validating, and the
cause is nearly always a total typed in beside the lines it summarises, one of which later changed.
`WithComputedTotals()` removes the chance to disagree.

**Naming the syntax is optional.** `Write(invoice)` uses the syntax the declared profile is written in — a
Peppol invoice is UBL, a Factur-X one is CII. Say it explicitly with `Write(invoice, DocumentFormat.Ubl)`
when you mean to.

## Check it

```csharp
ValidationReport report = einvoicing.Validate(xml);

Console.WriteLine(report);            // says what ran, then what failed

report.IsConforming;                  // no rule broken, and everything that should have checked it did
report.Errors;                        // just the failures
report.NotRun;                        // and what nobody checked
report.Failed("BR-CO-10");            // did that one rule fail?
```

In a pipeline that must not let a bad document through, one call does it:

```csharp
einvoicing.Validate(xml).EnsureConforming();   // throws with the whole report attached
```

Which rules run is what you assembled. `AddDefaults()` brings EN 16931; add the rest as you need them:

```csharp
EInvoicing einvoicing = EInvoicing.Create(e => e
    .AddDefaults()
    .AddXRechnungRules()                                     // the German CIUS
    .AddRulesFromFile(                                       // artefacts that may not be redistributed
        DocumentSyntax.Ubl, "artefacts/PEPPOL-EN16931-UBL.sch", "Peppol BIS Billing 3.0", "3.0"));
```

A document no registered rule set covers is reported as **unchecked**, with the call that would fix it —
never as valid.

## With dependency injection

The same calls, in a container. One registration wires the readers, the writers, the profiles and the rules,
and `EInvoicing` itself becomes injectable:

```csharp
builder.Services.AddEInvoicing(einvoicing => einvoicing
    .AddDefaults()                                   // UBL, CII, lifecycle, Factur-X, EN 16931
    .AddFrance()                                     // profiles and the lifecycle plumbing France needs
    .UseDiagnosticPreset(DiagnosticPreset.Balanced));
```

```csharp
public sealed class InvoiceEndpoint(EInvoicing einvoicing)
{
    public async Task<IResult> Post(Stream body, CancellationToken cancellationToken)
    {
        DocumentResult result = await einvoicing.ReadAsync(body, cancellationToken);

        return result.TryGetInvoice(out EInvoice? invoice)
            ? Results.Ok(invoice.Number.Value)
            : Results.BadRequest(result.Errors.Select(diagnostic => diagnostic.ToString()));
    }
}
```

Want less than `AddDefaults()`? Take the pieces: `AddUbl()`, `AddCii()`, `AddCdar()`, `AddFacturX()`,
`AddEn16931Rules()`, and `AddFacade()` if you still want `EInvoicing` injectable. Each one registers what it
needs — there is no second list of `Add…Services()` calls to remember.

For hybrid PDFs, add the implementation and it is picked up:

```csharp
builder.Services.AddEInvoicing(einvoicing => einvoicing.AddDefaults());
builder.Services.AddFacturXPdfSharp();
```

## The three promises, and where they show up

**Extensible without forking.** Register your own profile, reader, writer or rule set and it wins over the
one shipped. See the [recipes](../recipes/README.md).

**Nothing is lost, nothing explodes.** Every field keeps the text and attributes it came with
([raw values](raw-values.md)); readers report instead of throwing ([reading](reading.md)).

**Honest about its limits.** A profile with no rule set is reported as unchecked, never as passed
([validation](validation.md)).

## See all of it at once

Every page below has a chapter in the sample, which is part of the solution and therefore compiled on every
push — an API that changes and a sample that stops building are the same event.

```bash
dotnet run --project samples/International.EInvoicing.Samples
```

It builds an invoice that passes EN 16931, writes it in both syntaxes, reads it back, validates it, feeds
itself documents it cannot fully read, registers a profile and a rule of its own, puts an invoice inside a
PDF, and reports French lifecycle statuses and e-reporting. See
[samples/README.md](https://github.com/Epic-Dice-Studio/International.EInvoicing/blob/main/samples/README.md) for the chapter list.

## Where to go next

| | |
|---|---|
| [One country, one object](country-shortcuts.md) | France, Germany or Belgium, without assembling anything |
| [Reading a document](reading.md) | Streams, detection, diagnostics, limits |
| [Writing a document](writing.md) | Builders, credit notes, hybrid PDFs, extensions |
| [Lifecycle statuses](lifecycle.md) | French statuses, partner against public portal |
| [Validation](validation.md) | Rule sets, coverage, your own rules |
| [Raw values](raw-values.md) | Reaching the text behind any field |
| [The runnable sample](https://github.com/Epic-Dice-Studio/International.EInvoicing/blob/main/samples/README.md) | Every feature, one chapter at a time |
| [Try it in the browser](https://epic-dice-studio.github.io/International.EInvoicing/demo/) | No install, nothing uploaded |
