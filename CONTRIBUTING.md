# Contributing

Thank you for helping make electronic invoicing less painful for .NET developers.

## Getting started

You need the **.NET 10 SDK**. Nothing else.

```bash
git clone https://github.com/Epic-Dice-Studio/International.EInvoicing
cd International.EInvoicing
dotnet build -c Release
dotnet test  -c Release
```

Tests run on `net10.0` locally. CI additionally installs the .NET 8 runtime and runs
`dotnet test -p:TestAllTargetFrameworks=true`, so the library is exercised on every framework it ships for.

## Before you open a pull request

- `dotnet build -c Release` is warning-free. Warnings are errors here.
- `dotnet format --verify-no-changes` passes.
- Read [AGENTS.md](AGENTS.md). It is short and it is binding — especially §2 (code style: the code explains
  itself, comments are rare) and §4 (definition of done).
- If you added a diagnostic code, add its page under `docs/diagnostics/`. CI checks this.
- If you changed what the library supports, edit `docs/coverage.json` and run
  `dotnet run build/SyncCoverage.cs`. CI checks this too.
- If you added a dependency, add an ADR under `docs/adr/` explaining why.

## Adding support for a standard, a country or a profile

Start from the matching recipe in `docs/recipes/`. It contains the skeleton and the exact list of tests
expected. Support is only complete with all five test families — parse, round-trip, conformance, rules,
diagnostics — running against the **official** sample files, never files we invented.

## Normative artefacts

`specs/` holds XSD, Schematron and official samples. Never hand-edit them. To update a standard, bump the
version in the folder's `PROVENANCE.md` and run `build/fetch-specs.sh`. Artefacts whose licence forbids
redistribution are not committed; the `PROVENANCE.md` tells you where to download them.

## Reporting a security issue

Do not open a public issue. See [SECURITY.md](SECURITY.md).
