# XRechnung

## Scope and version

XRechnung is the German CIUS of EN 16931, maintained by KoSIT. It exists in both syntaxes (UBL and CII) and
comes with an Extension that adds elements beyond EN 16931.

We target the **3.x** line.

## Official sources

| Source | Use it for |
|---|---|
| <https://xeinkauf.de/xrechnung/> | The specification, CIUS and Extension. |
| <https://github.com/itplr-kosit/xrechnung-schematron> | Schematron rules. Apache-2.0. |
| <https://github.com/itplr-kosit/xrechnung-testsuite> | Official test documents — our golden corpus for Germany. |
| <https://github.com/itplr-kosit/validator> and its [configuration](https://github.com/itplr-kosit/validator-configuration-xrechnung) | The reference validator our CI cross-checks against. |

## Artefacts

`specs/xrechnung/` — Schematron and test suite. Fetched by `build/fetch-specs.sh`.

## Model mapping

XRechnung restricts EN 16931 and adds German specifics, chiefly the **Leitweg-ID** (`BT-10`, buyer reference)
which is mandatory for public-sector recipients and carries its own check digit.

`International.EInvoicing.Countries.Germany` owns the profile registration, the Leitweg-ID value type and the
German rule set, on top of the shared UBL and CII packages.

## Validation

XSD, EN 16931 rules, then the XRechnung rules for the relevant syntax. Documents using the Extension are
validated against the Extension rule set instead of the plain CIUS.

Our CI additionally runs the KoSIT validator (a Java tool) over everything we generate. It never becomes a
runtime dependency — it exists to prove our engine agrees with the reference.

## Pitfalls

- **Leitweg-ID validation is real validation**, not a regex: it has a structure and a check digit.
- **CIUS and Extension are different rule sets.** Validating an Extension document against the CIUS produces
  spurious errors.
- **Both syntaxes are equally valid.** A German receiver may send you either; support both from the start.
- **KoSIT releases pair a configuration with an XRechnung version.** Record the pair when updating artefacts.

## Reference implementations

- [KoSIT validator](https://github.com/itplr-kosit/validator) — the authority when we disagree with it.
- [mustangproject](https://github.com/ZUGFeRD/mustangproject) — XRechnung CII generation.
