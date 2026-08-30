# Samples

## [International.EInvoicing.Samples](International.EInvoicing.Samples) — everything, in one run

A console application that exercises every capability the library has, one chapter at a time. Run it:

```bash
dotnet run --project samples/International.EInvoicing.Samples
```

It prints what it did as it goes, so you can read the output beside the code that produced it.

| Chapter | Source | The guide it belongs to |
|---|---|---|
| Assembling the library, with and without a container | [`Wiring.cs`](International.EInvoicing.Samples/Chapters/Wiring.cs) | [getting started](../docs/guides/getting-started.md) |
| Building, writing, reading back and validating an invoice | [`Invoices.cs`](International.EInvoicing.Samples/Chapters/Invoices.cs) | [writing](../docs/guides/writing.md), [reading](../docs/guides/reading.md) |
| Documents that fight back — unknown profiles, unreadable values, truncated XML | [`HostileDocuments.cs`](International.EInvoicing.Samples/Chapters/HostileDocuments.cs) | [reading](../docs/guides/reading.md), [raw values](../docs/guides/raw-values.md) |
| Adding a profile and a rule from your own code | [`Extending.cs`](International.EInvoicing.Samples/Chapters/Extending.cs) | [recipes](../docs/recipes/README.md) |
| Factur-X: the invoice inside a PDF | [`HybridPdf.cs`](International.EInvoicing.Samples/Chapters/HybridPdf.cs) | [Factur-X](../docs/standards/facturx.md) |
| French lifecycle statuses | [`FrenchLifecycle.cs`](International.EInvoicing.Samples/Chapters/FrenchLifecycle.cs) | [lifecycle](../docs/guides/lifecycle.md) |
| French e-reporting, flux 10 | [`FrenchEReporting.cs`](International.EInvoicing.Samples/Chapters/FrenchEReporting.cs) | [e-reporting](../docs/guides/e-reporting.md) |
| National identifiers, checked rather than matched | [`NationalIdentifiers.cs`](International.EInvoicing.Samples/Chapters/NationalIdentifiers.cs) | [identifiers](../docs/guides/identifiers.md) |
| Rule sets, shipped and fetched | [`NationalRuleSets.cs`](International.EInvoicing.Samples/Chapters/NationalRuleSets.cs) | [validation](../docs/guides/validation.md) |

## What it is for

**A starting point you can copy.** The invoice built in the second chapter passes EN 16931 — it is not a
fragment that would need six more fields before it validated.

**Documentation that cannot rot.** The sample is part of the solution, so CI compiles it on every push. An
API that changes and a sample that no longer builds are the same event.

**An honest tour.** The chapter on hostile documents deliberately feeds the library things it cannot fully
read, and prints what it does about them. What a library does when the input is wrong is worth more than what
it does when the input is right.

## The last chapter needs a fetch

Peppol and the DGFiP publish their Schematron under no licence, so this repository does not carry it. Without
those files the last chapter says so and moves on; with them, it loads and reports them:

```bash
build/fetch-specs.sh peppol france
```
