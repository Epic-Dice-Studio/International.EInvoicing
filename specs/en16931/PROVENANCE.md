# EN 16931 validation artefacts and code lists

| | |
|---|---|
| **Source** | <https://github.com/ConnectingEurope/eInvoicing-EN16931> |
| **Version** | `validation-1.3.16` |
| **Retrieved** | 2026-08-30 |
| **Licence** | European Union Public Licence v1.2 — see `LICENSE.upstream.txt` |
| **Redistributable** | verbatim, yes. Derived works are another matter — see `docs/adr/0009-artefact-licensing.md` |

Expected content: `ubl/schematron/EN16931-UBL-model.sch`, `cii/schematron/EN16931-CII-model.sch`, the
code lists, and the official test files.

The EN 16931 standard **text** (the semantic model, sold by CEN members) is not here and cannot be
redistributed. Buy it from AFNOR, DIN or BSI. The artefacts above encode its rules, which is what the code
needs.

## The compiled form, for testing the reader

`build/fetch-specs.sh pint` also fetches the **compiled** EN 16931 stylesheet of this same version, from
<https://github.com/phax/phive-rules>, into `compiled/`. It is not used at run time and is not
redistributed — it exists so that `CompiledSchematronTests` can hold the compiled-Schematron reader to the
source Schematron above: the same version in two serialisations must yield the same assertions.
