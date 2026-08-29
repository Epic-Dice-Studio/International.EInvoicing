# Peppol BIS Billing 3.0

## Scope and version

Peppol BIS Billing 3.0 is a CIUS of EN 16931 plus a set of additional rules, used by the Peppol network. It is
the de facto European exchange profile: Belgium mandates it from 2026, and the Nordics, the Netherlands,
Australia, New Zealand and Singapore rely on it.

UBL is the syntax in practice; a CII rule set also exists.

## Official sources

| Source | Use it for |
|---|---|
| <https://docs.peppol.eu/poacc/billing/3.0/> | The specification, with readable rule explanations and examples. |
| <https://github.com/OpenPEPPOL/peppol-bis-invoice-3> | Schematron rules and example documents. **No licence declared**, so this repository does not redistribute them — see [ADR 0009](../adr/0009-artefact-licensing.md). |
| Peppol release notes | Which EN 16931 artefact version a given release adopts. Record it — mismatches cause false failures. |
| Peppol code lists (EAS, ICD, participant identifiers) | Electronic address and organisation identifier schemes. |

## Artefacts

`specs/peppol/` — fetched by `build/fetch-specs.sh peppol` into a git-ignored folder. Nothing from Peppol is
committed to this repository: the upstream project declares no licence, so redistribution is not established.

## Model mapping

Peppol adds no elements: it restricts EN 16931 and constrains identifier schemes. The work is therefore in
rules and code lists rather than in the model. `International.EInvoicing.Peppol` registers the profile, its
rule set, and the EAS/ICD code lists.

Note the scope boundary: participant identifiers and endpoint addressing are modelled because they appear
*in the document* (`BT-34`, `BT-49`). SMP lookup and AS4 transmission are not, and never will be — this
library does no network I/O.

## Validation

EN 16931 rules first, then the Peppol rules. Both must pass. Peppol releases quarterly, so the rule version
is part of the validation report.

## Pitfalls

- **Two rule sets, one document.** Running only Peppol rules or only EN 16931 rules gives a false pass.
- **Version pairing matters.** A Peppol release adopts a specific EN 16931 artefact version.
- **Electronic address schemes are strictly coded.** `BT-34`/`BT-49` need a valid EAS code; guessing it from
  the country is a common and rejected shortcut.
- **Some rules are warnings.** Report severity faithfully — treating warnings as failures blocks legitimate
  invoices.

## Reference implementations

- [peppol-commons](https://github.com/phax/peppol-commons) — identifiers and code lists.
- [A-NZ Peppol BIS](https://github.com/A-NZ-PEPPOL/A-NZ-PEPPOL-BIS-3.0) — a well-documented national extension.
