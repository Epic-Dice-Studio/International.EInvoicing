# International.EInvoicing.Testing

A test kit for electronic invoicing: sample documents that conform, a round-trip harness that proves nothing
was lost, a corpus of documents that fight back, and assertions that read like the promise they defend.

```
dotnet add package International.EInvoicing.Testing
```

Framework-free — the assertions throw, which xUnit, NUnit, MSTest and a plain console app all understand.

## Documents that conform

```csharp
EInvoice invoice = SampleInvoices.Conforming();
EInvoice creditNote = SampleInvoices.ConformingCreditNote();
EInvoice awkward = SampleInvoices.WithSomethingUnmapped();
```

Each one passes EN 16931 as this library validates it, so when your test fails the fixture is not the
suspect. Building a document the base norm accepts takes some thirty terms; that is where an afternoon goes.
Pass your own profile and a `configure` callback to change one term and keep the rest correct.

## Round trips

```csharp
RoundTripResult result = RoundTrip.Check(library, receivedXml);

Expect.LostNothing(result);
```

The check is by element census, not by text. **Byte equality is not promised and should never be asserted**:
namespace prefixes, insignificant whitespace and attribute order are not normative. What must hold is that no
element the original carried is missing from the result — including the ones the model has no field for.

`RoundTrip.Check(library, invoice, format)` starts from a model instead, which tests the other half: that
what the writer emits, the reader understands.

## Documents that fight back

```csharp
foreach (HostileDocument document in HostileDocuments.All)
{
    DocumentResult result = library.Read(document.Xml);       // must not throw
    result.IsUsable.ShouldBe(document.StaysUsable);
}
```

A profile nobody registered, a date in a format nobody agreed to, an amount with a comma, an element with no
business term, XML that stops halfway, a zero-byte file. Point them at *your* reader and *your* profile: if
one of them throws, the promise is broken for whoever integrates with you.

## Assertions

| | |
|---|---|
| `Expect.Conforming(report)` | No rule failed **and** every rule set that applies ran |
| `Expect.Failed(report, "BR-CO-10")` | The half of a rule's test that gets forgotten |
| `Expect.Passed(report, "BR-CO-10")` | |
| `Expect.Checked(report, "XRechnung")` | The report is about what you think it is about |
| `Expect.Reported(result, "EIV1042")` | Reading reported a diagnostic by code |
| `Expect.Usable(result)` | Something usable came out, whatever had to be given up |
| `Expect.LostNothing(roundTrip)` | |
| `Expect.Raw(invoice.IssueDate, "20260901")` | The field kept the text the document carried |

The messages carry the evidence — which rules fired, which rule sets did not run, what was reported instead.
An assertion that fails with "expected true, was false" costs an hour.

`Expect.Conforming` is deliberately stricter than "no errors": a document judged by fewer rule sets than
apply to it is unchecked, not valid, and a test that accepts the first as the second passes while proving
nothing.

Part of [International.EInvoicing](https://github.com/Epic-Dice-Studio/International.EInvoicing).
