## What this changes

<!-- One paragraph. Link the issue if there is one. -->

## Checklist

- [ ] `dotnet build -c Release` is warning-free
- [ ] `dotnet format --verify-no-changes` passes
- [ ] Code follows AGENTS.md §2 — it explains itself; comments are rare and only where the code cannot speak
- [ ] New diagnostic codes have a page in `docs/diagnostics/`
- [ ] `docs/coverage.json` updated and `dotnet run build/SyncCoverage.cs` run, if support changed
- [ ] New dependency justified by an ADR in `docs/adr/`

## For a new format, profile or country

- [ ] Golden files come from the **official** sample set, with provenance recorded
- [ ] Parse tests
- [ ] Round-trip tests (model equality and C14N equality)
- [ ] Conformance tests (XSD + official Schematron, zero error)
- [ ] Rule tests (one passing and one failing case per rule)
- [ ] Diagnostic tests (one per code the new code can emit)
