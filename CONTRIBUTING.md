# Contributing

Thank you for helping make electronic invoicing less painful for .NET developers.

## Getting started

You need the **.NET 10 SDK**. Nothing else.

```bash
git clone https://github.com/Epic-Dice-Studio/International.EInvoicing
cd International.EInvoicing
dotnet build -c Release
dotnet test  -c Release
dotnet run --project samples/International.EInvoicing.Samples   # every feature, once, out loud
```

Tests run on `net10.0` locally. CI additionally installs the .NET 8 runtime and runs
`dotnet test -p:TestAllTargetFrameworks=true`, so the library is exercised on every framework it ships for.

## Before you open a pull request

- `dotnet build -c Release` is warning-free. Warnings are errors here.
- `dotnet format --verify-no-changes --no-restore` passes. Keep `--no-restore`: without it, `dotnet format`
  reports fewer rules than CI does.
- Read [AGENTS.md](AGENTS.md). It is short and it is binding — especially §2 (code style: the code explains
  itself, comments are rare) and §4 (definition of done).
- If you added a diagnostic code, add its page under `docs/diagnostics/`. CI checks this.
- If you changed what the library supports, edit `docs/coverage.json` and run
  `dotnet run --project build/Tools -- coverage`. CI checks this too.
- If you changed the public API, the build tells you so. Record the change:
  `dotnet format analyzers <project> --diagnostics RS0016 --severity warn` writes the new entries into that
  package's `PublicAPI.Unshipped.txt`, and the diff is what a reviewer looks at. See
  [ADR 0011](docs/adr/0011-public-api-tracking.md).
- If you added a dependency, add an ADR under `docs/adr/` explaining why.
- If you changed the public API, check that `samples/International.EInvoicing.Samples` still shows it well.
  It is part of the solution, so CI already refuses a sample that stopped compiling — but a sample that
  compiles and no longer demonstrates the feature is just as stale.

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

## Releasing (maintainers)

Versions come from git tags via MinVer: tagging `v0.1.0` publishes `0.1.0`. Pushes to `main` publish
`x.y.z-preview.N` to GitHub Packages automatically, which needs no configuration.

Publishing to NuGet.org needs three things set up once, and no API key. NuGet.org keys now expire after 30
days, so the release workflow uses **trusted publishing**: it exchanges the workflow's OIDC token for a key
that lives about an hour.

1. **Reserve the package ID prefix** — optional, and not a form on the site. Review the criteria at
   <https://learn.microsoft.com/nuget/nuget-org/id-prefix-reservation>, then email `account@nuget.org` with
   the owner display name and the prefix requested. Note that the criteria discourage generic words, so
   `International.EInvoicing.*` may well be refused where a prefix naming the owner would not be. Publishing
   works either way; a reservation only stops someone else publishing under the same prefix.
2. **Create the trusted publishing policy.** On nuget.org, user menu → Trusted Publishing → new policy:
   - package owner: the nuget.org account or organisation that will own the packages
   - repository: `Epic-Dice-Studio/International.EInvoicing`
   - workflow file: `release.yml`
   - environment: `nuget`
3. **Add the repository secret `NUGET_USER`** with your nuget.org *username* — the profile name, not the
   email address. It is the only secret the release needs.

Optionally create the `nuget` GitHub environment (Settings → Environments) with required reviewers, so a
release waits for an explicit approval before anything leaves the repository.

Then:

```bash
git tag v0.1.0
git push origin v0.1.0
```
