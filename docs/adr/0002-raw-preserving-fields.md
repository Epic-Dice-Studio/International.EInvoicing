# 0002 — Every data property is a raw-preserving `Field<T>`

**Status:** Accepted · 2026-08-29

## Context

Invoice values carry meaning in their attributes: a date has a UNTDID 2379 format code, an amount a currency,
an identifier a scheme. Handing back a bare `DateOnly` discards that, and makes faithful re-emission
impossible. Developers then re-parse the XML themselves — the library becomes a burden.

## Decision

No data property is a bare CLR type. Each is a `Field<T>` or one of the specialised types mirroring the
UN/CEFACT unqualified data types (`DateField`, `AmountField`, `QuantityField`, `IdentifierField`, `CodeField`,
`TextField`, `BinaryField`). Each carries the typed value, the raw text, the attributes, the source location,
and any diagnostic explaining an unparsable value. Implicit conversions keep everyday use ordinary.

Fields are thin `readonly record struct`s: `{ T? Value; FieldSource? Source }`, where `FieldSource` is an
optional class holding raw text, attributes, location and diagnostic. A field built in code allocates nothing.

## Consequences

- Round-trip fidelity: an unmodified field is written from its raw text, so a parse-and-re-emit is equal to
  the original after C14N canonicalisation.
- An unparsable value no longer destroys a document; it becomes `IsRawOnly` plus a diagnostic.
- The model is more verbose to write by hand, which is why builders accept bare types.
- Serialising the model to JSON needs converters. Accepted.
