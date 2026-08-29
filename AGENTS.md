# AGENTS.md — working agreement for contributors and coding agents

This file is short on purpose. It states the rules that are **not** negotiable. Everything else lives in
`docs/`, and every rule below links to the page that explains it.

---

## 1. Commands

```bash
dotnet build -c Release                       # must be warning-free (warnings are errors)
dotnet test  -c Release                       # runs on net10.0
dotnet test  -c Release -p:TestAllTargetFrameworks=true   # adds net8.0 (CI does this)
dotnet format --verify-no-changes --no-restore   # exactly what CI runs; without --no-restore it misses rules
dotnet pack  -c Release -o artifacts          # versions come from git tags via MinVer

dotnet run --project build/Tools -- coverage     # regenerate the README support matrix
dotnet run --project build/Tools -- diagnostics  # every emitted EIV code has a catalogue page
```

## 2. Code style

Write code that explains itself. **Do not narrate it with comments.**

- Naming and structure carry the meaning. If a block needs a comment to be understood, extract a method with
  the name the comment would have had.
- Comment only what code cannot say: a security constraint, a normative rule from a specification (quote the
  rule id — `BR-CO-10`, `BT-24`), or a deliberate deviation and why.
- XML doc comments **are** required on public API — this is a published library and `CS1591` is a warning,
  which means an error here. Keep them to one or two sentences.
- SOLID and KISS over cleverness: one responsibility per type, dependencies on abstractions, no speculative
  generality. The simplest thing that satisfies the norm wins.
- Four spaces, file-scoped namespaces, `var` only when the type is obvious, `InvariantCulture` always.

## 3. Architectural rules

| Rule | Why |
|---|---|
| **Every data property is a `Field<T>`**, never a bare `string`/`decimal`/`DateOnly`. | Callers must be able to reach the raw text and the XML attributes of any field. See `docs/guides/raw-values.md`. |
| **Readers never throw on input data.** Malformed, unknown or unsupported content becomes a diagnostic plus a documented fallback. | The promise of the library. Exceptions are for programming errors only (null argument, disposed stream). |
| **Every diagnostic code has a page** in `docs/diagnostics/`. | CI fails otherwise. A code without an explanation is worse than no code. |
| **Never silently succeed.** A profile that is not supported must surface in the parse diagnostics *and* in `ValidationReport.IsComplete`. | Honesty is the product. |
| **No reflection in serialisation.** Readers and writers use `XmlReader`/`XmlWriter` explicitly. | Element order is normative in UBL and CII; performance and AOT depend on it. |
| **All XML goes through `SecureXml`.** | Invoices arrive from third parties: XXE, entity expansion and memory exhaustion are real. |
| **No network I/O anywhere in the library.** | Transport is permanently out of scope; code lists are embedded, never fetched. |
| **New dependency ⇒ new ADR** in `docs/adr/`. | Dependencies are a tax paid by every consumer. |
| **A country-specific type is prefixed with its ISO 3166 alpha-2 code** (`FrCdarProfile`, `DeLeitwegId`). A feature shared by several countries belongs to a format package, not duplicated per country. | See `docs/recipes/add-a-country.md`. |

## 4. Definition of done for a format or a country

A pull request that adds parsing or generation is not complete until all five test families exist:

1. **Parse** — every golden file of that standard is read; key business terms asserted.
2. **RoundTrip** — parse → model → write → re-parse yields an equal model, and XML equal to the original
   after C14N canonicalisation. Byte equality is *not* required.
3. **Conformance** — everything generated passes XSD plus the official Schematron with zero error.
4. **Rules** — one passing and one failing case per business rule implemented.
5. **Diagnostics** — one test per diagnostic code the new code can emit.

Plus: the entry in `docs/coverage.json` (then `dotnet run --project build/Tools -- coverage`), and the
matching page in `docs/standards/`.

## 5. Where to look before writing code

| You are about to… | Read first |
|---|---|
| add a syntax (UBL, CII, …) | `docs/recipes/add-a-format.md`, then `docs/standards/<syntax>.md` |
| add a country | `docs/recipes/add-a-country.md` |
| add a profile or CIUS | `docs/recipes/add-a-profile.md` |
| add or change a validation rule | `docs/recipes/add-a-rule.md` |
| touch the value or diagnostic model | `docs/adr/` — these are load-bearing decisions |

Normative artefacts (XSD, Schematron, official samples) live in `specs/`, each folder carrying a
`PROVENANCE.md` naming the source, version, retrieval date and licence. Never hand-edit them; update the
version and re-run `build/fetch-specs.sh`.

## 6. Reference implementations

When a mapping is unclear, compare against these before inventing an interpretation. Read them for the
*semantics*; do not copy code.

**Before declaring a format or a country done, mine their issue trackers** — see
[docs/prior-art.md](docs/prior-art.md). Their open issues are the edge cases we have not met yet, and reading
them costs an hour where rediscovering them costs a production incident. Record what you find, including
findings that led to nothing.

- **Java** — [mustangproject](https://github.com/ZUGFeRD/mustangproject) (Factur-X/ZUGFeRD reference),
  [ph-schematron](https://github.com/phax/ph-schematron), [peppol-commons](https://github.com/phax/peppol-commons).
- **PHP** — [horstoeko/zugferd](https://github.com/horstoeko/zugferd) (profile-driven builder design).
- **Python** — [akretion/factur-x](https://github.com/akretion/factur-x), [drafthorse](https://github.com/pretix/drafthorse).
- **C#** — [ZUGFeRD-csharp](https://github.com/stephanstapel/ZUGFeRD-csharp) — useful for CII pitfalls.
