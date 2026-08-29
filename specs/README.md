# specs/ — normative artefacts

Schemas, Schematron rules and official sample documents, kept here so the build is hermetic and works
offline.

**Never hand-edit anything in this directory.** To update a standard, bump the version recorded in the
folder's `PROVENANCE.md` and run `build/fetch-specs.sh`. A hand-patched artefact silently diverges from the
norm, which is the one failure mode this library cannot afford.

Every folder carries a `PROVENANCE.md` stating the source URL, the exact version, the retrieval date, the
licence and whether the artefact may be redistributed. Artefacts that may **not** be redistributed are not
committed — their `PROVENANCE.md` explains where to obtain them and where to place them locally.

| Folder | Standard | Redistributable |
|---|---|---|
| `ubl-2.1/` | OASIS UBL 2.1 schemas | yes |
| `cii-d22b/` | UN/CEFACT Cross Industry Invoice D22B | yes |
| `cdar/` | UN/CEFACT Cross Domain Acknowledgement and Response | yes |
| `en16931/` | EN 16931 validation artefacts and code lists | yes (Apache-2.0) |
| `peppol/` | Peppol BIS Billing 3.0 artefacts | yes (Apache-2.0) |
| `xrechnung/` | XRechnung Schematron and test suite | yes (Apache-2.0) |
| `facturx/` | Factur-X / ZUGFeRD schemas and samples | partly — specification text is not |
| `fr-dse/` | French DGFiP external specifications | no — download required |
