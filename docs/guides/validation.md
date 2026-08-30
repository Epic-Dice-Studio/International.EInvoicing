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
    // EN 16931 (UBL) 1.3.13  ran
    // urn:acme:profile:2p0 —  skipped — this library implements no rule set for that profile,
    //                         so only EN 16931 was checked
}
```

## Reading what failed

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

`International.EInvoicing.Validation.En16931` carries the published EN 16931 artefacts, version 1.3.13, for
both UBL and CII. They are embedded, so validation works offline and ships with the version it was tested
against.

```csharp
using International.EInvoicing.Validation.En16931;
using International.EInvoicing.Validation.Schematron;

SchematronRuleSet rules = En16931Rules.For(DocumentSyntax.Ubl);
ValidationReport report = new SchematronValidator().Validate(xml, rules);
```

Peppol's artefacts are **not** embedded: the publisher grants no redistribution. Run
`build/fetch-specs.sh peppol` to obtain them, then load them like any other rule set.

## Any rule set, including your own

The engine takes Schematron as data, so anything published as `.sch` runs — national rules, a customer's own
requirements, a draft you are testing.

```csharp
SchematronRuleSet mine = SchematronRuleSet.Load(
    File.ReadAllText("acme-rules.sch"),
    name: "Acme purchasing rules",
    version: "2026-08");

ValidationReport report = new SchematronValidator()
    .Validate(xml, En16931Rules.For(DocumentSyntax.Ubl))
    .And(new SchematronValidator().Validate(xml, mine));
```

Combining reports keeps both rule sets in the coverage block, so the result still says what ran.

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

All 23 documents EN 16931 publishes as correct, and all 80 CIUS documents of the official XRechnung test
suite. Those tests run on every commit.

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
