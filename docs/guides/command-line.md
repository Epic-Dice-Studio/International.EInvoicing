# The command line

## The problem this solves

The reference validator for electronic invoices is a Java jar. If your stack is .NET, checking a document
before you send it means installing a JRE, or pasting the invoice into somebody's website.

```
dotnet tool install --global International.EInvoicing.Cli --prerelease

einvoice validate invoice.xml
```

## Validating

```
einvoice validate invoice.xml
einvoice validate ./outbox            # a directory is walked
einvoice validate ./outbox --json     # for a pipeline
einvoice validate ./outbox --quiet    # only what failed
```

What comes out says what was **checked**, not only what failed:

```
conforming   outbox/FA-2026-001.xml
    checked      EN 16931-1:2017 (UBL) 1.3.16
    checked      XRechnung (UBL) 3.0

1/1 conforming.
```

and when the tool had less to work with than the document deserved, it says so:

```
conforming   outbox/peppol.xml
    note         declares urn:cen.eu:en16931:2017#compliant#urn:fdc:peppol.eu:2017:poacc:billing:3.0,
                 a specialisation of EN 16931, and only the base judged it. Its own rules are published
                 separately — see build/fetch-specs.sh, then --rules.
```

That note exists because the alternative is worse. A CIUS carries rules of its own; most of those artefacts
may not be redistributed, so they are absent until you fetch them. Without the note the document reads as
fully checked when only the base ran.

## Exit codes

| | |
|---|---|
| `0` | Every document conformed |
| `1` | A document was read and judged, and did not pass |
| `2` | The tool could not do the job — bad arguments, missing file, a PDF with no payload |

The last two are deliberately not the same. A CI job that treats "I could not read that" as "that failed
validation" is merely noisy; one that treats it as success is a pipeline that passes while checking nothing.

## Rule sets

EN 16931, XRechnung, France, Germany and Belgium are built in. The Peppol, Factur-X and most national
artefacts may not be redistributed:

```
./build/fetch-specs.sh peppol
einvoice validate invoice.xml --rules ./specs/peppol/rules
```

`--rules` takes a file or a directory, and reads both shapes publishers use — source Schematron and
Schematron already compiled to XSLT — working out which it is rather than making you say.

Two things to know. A rule set loaded from a file **judges every document in its syntax**: nothing inside a
Schematron artefact declares which profiles it governs, so the tool cannot narrow it. And a directory is read
**one level deep** for that reason — a published artefact tree holds rule sets for several jurisdictions side
by side, and a recursive sweep would hand your Peppol invoice to another country's rules and report their
verdict as though it meant something. `--recurse` overrides that when you know what is in the tree.

To see what is loaded:

```
einvoice rules
einvoice profiles
```

## Inspecting

Before validation there is a plainer question: what *is* this file?

```
$ einvoice inspect invoice.pdf
invoice.pdf
    kind         Cii
    carried in   a hybrid PDF
    profile      urn:cen.eu:en16931:2017#compliant#urn:factur-x.eu:1p0:basic
    resolved     exactly
    number       FA-2026-001
    issued       2026-09-01
    ...
    kept aside   2 element(s) the model has no field for
```

A Factur-X or ZUGFeRD PDF is opened and the payload taken out as it was embedded — not re-serialised from the
model, which would only prove the tool is self-consistent.

## Converting

```
einvoice convert invoice.xml --to cii > invoice-cii.xml
einvoice convert invoice.xml --to cii --out invoice-cii.xml
```

The document goes to standard output and the loss report to standard error, so the redirect above does the
obvious thing and still tells the person watching what did not cross. See
[converting between syntaxes](convert-between-syntaxes.md).

A lossless conversion is not a valid document: validate the result in the profile you intend to send.
