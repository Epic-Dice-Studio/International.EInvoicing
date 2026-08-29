# 0007 — Target `net8.0` and `net10.0`

**Status:** Accepted · 2026-08-29

## Context

Much of the ERP software that would consume this library still runs on .NET Framework, which would mean a
`netstandard2.0` target: polyfills, conditional compilation, and an API shaped by the oldest platform.

## Decision

Target `net8.0` and `net10.0` — the current LTS releases. No `netstandard2.0`.

## Consequences

- Modern language and BCL features are available unconditionally: `DateOnly`, `required`/`init`, spans,
  `ArgumentNullException.ThrowIfNull`.
- .NET Framework consumers are not served. If demand proves real, a `netstandard2.0` target can be added later
  behind conditional compilation — the reverse would be a redesign.
- Test projects build for `net10.0` by default so that contributing needs only the .NET 10 SDK; CI passes
  `-p:TestAllTargetFrameworks=true` to exercise both.
