# 0003 — Readers report diagnostics instead of throwing

**Status:** Accepted · 2026-08-29

## Context

Inbound invoices are written by other people's software. They contain unknown profiles, illegal dates, codes
that were retired last quarter, and elements nobody mapped. A library that throws on each of these forces its
users to wrap every call in a `try`/`catch` and gives them nothing to act on.

## Decision

`IDocumentReader.Read` throws only on programming errors — a null argument, a disposed stream. Everything
else becomes a `Diagnostic` on a `ParseResult<T>`: a stable code, a severity, a category, a location, the
business term, what was expected, what was found, and the fallback applied.

Fallbacks are defined per category and are documented, not improvised. Profile resolution walks a chain —
exact profile, parent CIUS, base EN 16931, generic syntax — and reports every step it takes.

Policy is configurable: presets `Lenient`, `Balanced` (default), `Strict`, overridable per category, per code,
or by a caller-supplied predicate.

## Consequences

- Callers make the business decision about what is fatal. The library does not decide for them.
- Every code needs a documentation page; CI enforces it.
- Diagnostics are part of the public contract: changing a code is a breaking change.
- Tests must cover the hostile corpus, not only the happy path.
