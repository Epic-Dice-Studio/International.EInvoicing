# ADR 0011 — Track the public API in files that are reviewed

**Status:** accepted, August 2026

## Context

This library's whole proposition is that you extend it from your own code: register a reader, a writer, a
profile, a rule set. That makes the public surface the product, not an implementation detail — and it makes
an accidental change to it expensive in a way an accidental change to a private method never is.

The developer-experience pass moved a great deal of that surface in a single week: entry points renamed,
overloads added, a facade made injectable. All of it was deliberate. The next such change might not be.

`PackageValidation` was already enabled, but it compares against a published baseline, and with nothing
stable published it checks framework compatibility and nothing else.

## Decision

Take a dependency on **`Microsoft.CodeAnalysis.PublicApiAnalyzers`**, and commit a
`PublicAPI.Shipped.txt` / `PublicAPI.Unshipped.txt` pair per shipping package.

Every public member appears in one of those files. Adding one fails the build until it is recorded; removing
one fails the build until the record is removed. Both show up in a pull request as a diff of a text file,
which is the point: an API change becomes something a reviewer sees rather than something they would have to
notice.

Nullability is tracked too (`#nullable enable` at the head of each file), so `string` becoming `string?` is
a change like any other.

## Consequences

**The analyzer's own rules apply to us.** `RS0026` — do not publish several overloads with optional
parameters — already found three places: `SecureXml.CreateReader`, `FrCdar.Collected` and `EInvoicing.Create`.
Each was rewritten as explicit overloads. That rule is worth obeying rather than suppressing: adding a
parameter to a published overload later is a break that compiles cleanly and fails at run time in someone
else's process.

**Regenerating is a command, not a chore.** After an intentional API change:

```bash
dotnet format analyzers <project> --diagnostics RS0016 --severity warn
```

writes the new entries into `PublicAPI.Unshipped.txt`.

**Everything sits in `Unshipped` for now.** Nothing stable has been released, so nothing is shipped. At 1.0
the unshipped entries move to `PublicAPI.Shipped.txt`, and from then on the two files say what was promised
and what has been added since.

## Alternatives considered

**A home-grown gate in `build/Tools`,** alongside the coverage and diagnostic ones. It would have avoided a
dependency, and the existing gates set that precedent. Rejected because reproducing this correctly — generics,
nullability, default values, explicit interface implementations — is work that is already done, and getting
it subtly wrong would give false confidence, which is worse than no gate.

**`PackageValidation` with a baseline.** Kept, and it will earn its place once 1.0 is published. It cannot
help before then, and it compares assemblies rather than showing a reviewer a diff.
