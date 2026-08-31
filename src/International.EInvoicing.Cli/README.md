# International.EInvoicing.Cli

Validate, inspect and convert electronic invoices from the command line.

```
dotnet tool install --global International.EInvoicing.Cli

einvoice validate invoice.xml
einvoice inspect  invoice.pdf
einvoice convert  invoice.xml --to cii > invoice-cii.xml
```

The reference validator in this space is a Java jar. This is the .NET one.

## What it does

| | |
|---|---|
| `validate <file\|directory>...` | Checks against every rule set that applies, and says which ones ran |
| `inspect <file\|directory>...` | What the document is, which profile it declares, what reading it reported |
| `convert <file> --to ubl\|cii` | Carries it to the other syntax, and reports what did not cross |
| `profiles` | The profiles this build knows |
| `rules` | The rule sets it can judge with |

UBL 2.1, UN/CEFACT CII, Factur-X and ZUGFeRD PDFs, and lifecycle messages.

## Exit codes

`0` conforming · `1` rejected, or read but unchecked · `2` could not run.

The three are kept apart on purpose: a script that treats "I had no rules for this" as success is a pipeline
that passes while checking nothing.

## Rule sets

EN 16931, XRechnung, France, Germany and Belgium are built in. The Peppol, Factur-X and most national
artefacts may not be redistributed, so they are fetched rather than packaged — see `build/fetch-specs.sh` in
the repository — and pointed at with `--rules`:

```
einvoice validate invoice.xml --rules ./specs/peppol/rules
```

A rule set loaded from a file judges every document in its syntax: nothing inside a Schematron artefact says
which profiles it governs. Directories are read one level deep for that reason; `--recurse` overrides it.

## Options

| | |
|---|---|
| `--rules <file\|directory>` | Add Schematron, source or already compiled to XSLT |
| `--recurse` | Read a `--rules` directory all the way down |
| `--strict` / `--lenient` | How hard reading should be on a document |
| `--json` | Machine-readable report (`validate`) |
| `--quiet` | Only what failed (`validate`) |
| `--out <file>` | Write there instead of standard output (`convert`) |

Part of [International.EInvoicing](https://github.com/Epic-Dice-Studio/International.EInvoicing).
