# Validating a document

## The short way

```csharp
ValidationReport report = einvoicing.Validate(xml);

if (report.IsConforming)
{
    // No rule broken, and everything that should have checked it did.
}
```

## Three questions, not one

A validator that answers only "is it valid?" hides the question that matters more: *was it fully checked?*

| Property | Asks |
|---|---|
| `IsValid` | Did any rule fail at error severity? |
| `IsComplete` | Did every rule set that applies actually run? |
| `IsConforming` | Both. This is the one to act on. |

A document validated against fewer rule sets than apply to it is **unchecked**, not valid. Presenting the
first as the second is the most expensive thing a validator can do, so the report keeps them apart.

```csharp
foreach (RuleSetOutcome outcome in report.RuleSets)
{
    Console.WriteLine(outcome);
    // EN 16931-1:2017 (UBL) 1.3.16  ran
    // urn:acme:profile:2p0 —  skipped — this library implements no rule set for that profile,
    //                         so only EN 16931 was checked
}
```

## Reading what failed

```csharp
report.Errors;                 // the rules that failed
report.Warnings;               // the rules that fired without failing the document
report.NotRun;                 // the rule sets nobody ran, and why
report.Failed("BR-CO-10");     // did that one rule fail?
```

In a pipeline that must refuse a bad document, one call does the whole thing — and note that it insists on
`IsConforming`, so a document nothing checked does not slip through:

```csharp
einvoicing.Validate(xml).EnsureConforming();   // throws with the report as the message
```

```csharp
foreach (ValidationMessage message in report.OfAtLeast(RuleSeverity.Error))
{
    Console.WriteLine($"{message.RuleIdentifier}  {message.Message}");
    Console.WriteLine($"  at {message.Location}, about {message.BusinessTerm}");
}
```

Rules keep the identifiers their publisher gave them — `BR-CO-14`, `PEPPOL-EN16931-R040` — so a message can
be looked up in the specification rather than only in this library.

Severity is reported as the rule set declares it. Some published rules are warnings, and treating those as
failures blocks legitimate invoices.

## What runs today

`International.EInvoicing.Validation.En16931` carries the published EN 16931 artefacts, version 1.3.16, for
both UBL and CII. They are embedded, so validation works offline and ships with the version it was tested
against.

They encode **EN 16931-1:2017**, and the rule set says so in its name. That matters now: CEN published
EN 16931-1:2026 in May 2026 and withdrew the 2017 edition, so both will be in circulation for years. A
document declaring the newer edition still parses and is checked against the 2017 rules, and it is reported
as [EIV1044](../diagnostics/EIV1044.md) rather than passing quietly — `En16931Edition` is where the library
says which edition it implements. See [ADR 0013](../adr/0013-en16931-editions.md).

```csharp
using International.EInvoicing.Validation.En16931;
using International.EInvoicing.Validation.Schematron;

SchematronRuleSet rules = En16931Rules.For(DocumentSyntax.Ubl);
ValidationReport report = new SchematronValidator().Validate(xml, rules);
```

`International.EInvoicing.Validation.XRechnung` carries the German rules, version 3.0, likewise for both
syntaxes. XRechnung *restricts* EN 16931 rather than replacing it, so run both and combine the reports —
running only the German rules leaves the base ones unchecked, and the report would say so.

```csharp
var validator = new SchematronValidator();

ValidationReport report = validator
    .Validate(xml, En16931Rules.For(DocumentSyntax.Ubl))
    .And(validator.Validate(xml, XRechnungRules.For(DocumentSyntax.Ubl)));

report.IsComplete;   // true: both rule sets ran
```

Peppol's artefacts are **not** embedded: the publisher grants no redistribution. Run
`build/fetch-specs.sh peppol` to obtain them, then load them like any other rule set.

## Any rule set, including your own

The engine takes Schematron as data, so anything published as `.sch` runs — national rules, a customer's own
requirements, a draft you are testing. Register it once when you assemble the library and every validation
takes it into account:

```csharp
EInvoicing einvoicing = EInvoicing.Create(e => e
    .AddDefaults()                                                    // EN 16931, both syntaxes
    .AddXRechnungRules()                                              // only for documents declaring XRechnung
    .AddRulesFromFile(                                                // an artefact you fetched
        DocumentSyntax.Ubl, "artefacts/PEPPOL-EN16931-UBL.sch", "Peppol BIS Billing 3.0", "3.0")
    .AddRulesFromFile(                                                // and one of your own
        DocumentSyntax.Ubl, "acme-rules.sch", "Acme purchasing rules", "2026-08"));

ValidationReport report = einvoicing.Validate(xml);
```

The same calls work in a container: `services.AddEInvoicing(e => e.AddDefaults().AddXRechnungRules())`.

Each rule set decides for itself whether it governs the document in front of it. Pass `appliesTo` to narrow
one to the profiles it was written for:

```csharp
.AddRulesFromFile(
    DocumentSyntax.Ubl, "acme-rules.sch", "Acme purchasing rules", "2026-08",
    appliesTo: profile => profile.Value?.Contains("acme") == true)
```

Everything that ran appears in the coverage block, and a document nothing covered is reported as unchecked
with the call that would fix it.

### Rules written in C#

Not every rule is worth expressing in Schematron. Implement `IDocumentRuleSet` and register it the same way:

```csharp
public sealed class NoWeekendInvoices : IDocumentRuleSet
{
    public string Name => "Acme house rules";

    public string Version => "1.0";

    public bool AppliesTo(DocumentSyntax syntax, ProfileIdentifier specification) => true;

    public ValidationReport Validate(string document) => …;
}

einvoicing = EInvoicing.Create(e => e.AddDefaults().AddRules(new NoWeekendInvoices()));
```

## Why the artefacts are executed rather than translated

Because they change. These standards are revised roughly twice a year, and an engine that runs the published
file stays correct when you drop the new one in, where generated or hand-written rules drift from the norm at
every release.

The engine carries its own evaluator for the XPath subset those artefacts use — measured, not guessed: of
1972 expressions, all but ten constructs are XPath 1.0. Numbers are `decimal`, because the rules that compare
invoice totals against sums of lines are exactly where binary floating point reports a correct invoice as
wrong by a hundredth.

An expression the engine cannot read raises rather than being skipped. A rule that quietly does not run is
worse than one that fails loudly.

## What it is measured against

All 23 documents EN 16931 publishes as correct, all 80 CIUS documents of the official XRechnung test suite
against the EN 16931 rules, and all 86 of them against the German rules. Those tests run on every commit.

## Validation is not reading

They answer different questions, and both are worth asking.

```csharp
DocumentResult read = einvoicing.Read(xml);       // could I understand it?
ValidationReport report = einvoicing.Validate(xml); // does it follow the rules?
```

A document can be perfectly readable and break a dozen rules, or conform completely while using a profile
this library has no typed support for. Look at both.

## Next

- [Reading a document](reading.md)
- [Adding or suppressing a rule](../recipes/add-a-rule.md)
- [The diagnostic catalogue](../diagnostics/README.md)

## Run it

[`samples/International.EInvoicing.Samples/Chapters/NationalRuleSets.cs`](https://github.com/Epic-Dice-Studio/International.EInvoicing/blob/main/samples/International.EInvoicing.Samples/Chapters/NationalRuleSets.cs) is this page as code — rule sets shipped and fetched.

```bash
dotnet run --project samples/International.EInvoicing.Samples
```
