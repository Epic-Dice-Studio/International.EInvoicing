# Reading the raw value behind a field

## The problem this solves

Your partner sends an invoice whose issue date is `20260829` with `format="102"`. Another sends
`2026-08-29T00:00:00+02:00`. A third sends `29/08/2026`, which is not legal but arrives anyway.

Most libraries hand you a `DateTime` and throw the rest away — so when accounting asks why the date shifted by
a day, or when a receiver rejects your reply because you echoed the date in a different format, you are left
re-parsing the XML by hand.

Here, every field keeps what it came from.

## The shape of a field

```csharp
invoice.IssueDate.Value        // DateOnly? — the typed value, null when it could not be parsed
invoice.IssueDate.Raw          // "20260829" — the exact text from the file
invoice.IssueDate.FormatCode   // "102" — the UNTDID 2379 attribute
invoice.IssueDate.Location     // where it was, for error messages
invoice.IssueDate.Diagnostic   // why Value is null, when it is
invoice.IssueDate.IsSet        // was the element present at all?
invoice.IssueDate.IsRawOnly    // present, but not interpretable
invoice.IssueDate.IsModified   // has your code written to it since parsing?
```

Typed use stays ordinary — there is an implicit conversion both ways:

```csharp
DateOnly? issued = invoice.IssueDate;
invoice.IssueDate = new DateOnly(2026, 9, 1);
```

## Field types

They mirror the UN/CEFACT unqualified data types, so a field carries exactly the attributes its syntax allows:

| Type | Value | Attributes kept |
|---|---|---|
| `DateField` | `DateOnly?` / `DateTimeOffset?` | `FormatCode` |
| `AmountField` | `decimal?` | `CurrencyCode` |
| `QuantityField` | `decimal?` | `UnitCode` |
| `IdentifierField` | `string?` | `SchemeId`, `SchemeAgencyId`, `SchemeVersionId` |
| `CodeField` | `string?` | `ListId`, `ListVersionId`, `ListAgencyId` |
| `TextField` | `string?` | `LanguageId` |
| `BinaryField` | `byte[]?` | `MimeCode`, `Filename` |
| `Field<T>` | `T?` | — |

## When a value cannot be parsed

Nothing throws. The field keeps its raw text, `Value` is null, `IsRawOnly` is true, and a diagnostic explains
what happened:

```csharp
if (invoice.IssueDate.IsRawOnly)
{
    logger.LogWarning(
        "Unusable issue date {Raw} at {Location}: {Reason}",
        invoice.IssueDate.Raw,
        invoice.IssueDate.Location,
        invoice.IssueDate.Diagnostic?.Message);
}
```

You decide whether that is fatal for your business process. The library does not decide for you.

## Why writing benefits too

A field you did not modify is written back from its raw text and original attributes. A field you did modify
is formatted according to the target profile. So a document you parse and re-emit without changes is
equivalent to the original after canonicalisation — which is what makes it safe to pass invoices through your
system without silently reformatting them.
