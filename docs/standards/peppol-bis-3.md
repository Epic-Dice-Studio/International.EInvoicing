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

## Artefacts, and validating without them

`specs/peppol/` — fetched by `build/fetch-specs.sh peppol` into a git-ignored folder. Nothing from Peppol is
committed to this repository: the upstream project declares no licence, so redistribution is not established.
Checked again in August 2026 — the repository still carries no `LICENSE`, `COPYING` or `NOTICE` file, and no
licensing statement in its README.

**So can a document be checked as Peppol-conformant without redistributing the artefacts?** Yes, three ways,
and it is worth being precise about what each one gives you.

| | What it checks | What it costs |
|---|---|---|
| **Fetch once, load from a path** | Everything. The engine runs the published rules as they are | One command, and a file you keep beside your application |
| **EN 16931 alone** | The base rules Peppol restricts — roughly nine tenths of what a Peppol document must satisfy | Nothing: those artefacts are EUPL-1.2 and ship with this library |
| **Reimplement the Peppol rules in C#** | Whatever you reimplemented, until Peppol's next quarterly release | Drift, which is the failure this library is built to avoid |

The first is what this repository does, and it is one line once the file is in hand:

```csharp
SchematronRuleSet peppol = SchematronRuleSet.Load(
    File.ReadAllText("PEPPOL-EN16931-UBL.sch"), "Peppol BIS Billing 3.0", "3.0");

ValidationReport report = new SchematronValidator().Validate(xml, peppol);
```

Nothing in the library needs to change to accept an artefact you supply — that is what "run the rules as
data" means. And the second row is not a consolation prize: a report says plainly which rule sets ran and
which did not, so a document checked against EN 16931 alone is never presented as Peppol-conformant.

The durable fix is upstream: OpenPEPPOL adding an explicit licence to the repository. Their
[service desk](https://openpeppol.atlassian.net/servicedesk/customer/portal/1) is where to ask.

## Measured against Peppol's own corpus

Peppol publishes a unit corpus — `rules/unit-UBL-PEPPOL` and `rules/unit-CII-PEPPOL` — where each case is a
document fragment together with the number of times a named rule is expected to fire. It is a far stronger
measurement than an example document, because agreement is not a matter of opinion.

Once fetched, this engine agrees with every one of those cases: **227 of 227 for UBL, 127 of 127 for CII**.

Getting there needed five things the Peppol artefacts use and the earlier rule sets did not: the range
operator `0 to $n`, `reverse`, `castable as` with the target type it was asked about, `substring` treated as
a window rather than an offset and a count, and functions that choose between branches with `xsl:choose`.

The example documents in `rules/examples` are illustrations from the guide, not conformance cases: several
carry identifiers that fail `PEPPOL-COMMON-R040`. Measured against the unit corpus instead.

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
- **The example documents are not a conformance suite.** Several fail Peppol's own identifier rules. The unit
  corpus is what to measure against.

## Reference implementations

- [peppol-commons](https://github.com/phax/peppol-commons) — identifiers and code lists.
- [A-NZ Peppol BIS](https://github.com/A-NZ-PEPPOL/A-NZ-PEPPOL-BIS-3.0) — a well-documented national extension.
