# Recipe — add or suppress a validation rule

## Prefer the official artefact

If the rule exists in an official Schematron, use the artefact. Hand-rewriting a published rule guarantees
divergence at the next release of the standard. Add the artefact to `specs/`, register it in the rule set, and
let the engine run it.

Write a rule in C# only when: no artefact exists (a national requirement published as prose), or the rule is
yours (a business rule of your own company).

## A rule in code

```csharp
public sealed class InvoiceNumberMustBeSequential : IValidationRule
{
    public string Code => "ACME-R001";
    public RuleSeverity Severity => RuleSeverity.Error;

    public ValidationMessage? Evaluate(EInvoice invoice, ValidationContext context) => ...;
}
```

Rules are single-purpose and side-effect free. A rule that needs to look at two documents is not a rule, it is
a service.

## Registering and suppressing

```csharp
services.AddEInvoicing(o => o
    .AddValidationRules(MyRules.All)
    .SuppressRule("BR-DE-15"));
```

Suppression exists because reality intrudes: a partner rejects a rule, or an artefact carries a known false
positive. A suppressed rule is reported as suppressed in the validation report — it never silently disappears.

## Prove it

One passing case and one failing case per rule, plus a test that the message carries the rule code, the
business term and a usable location. A rule whose failure message does not say where the problem is has not
been implemented, only added.
