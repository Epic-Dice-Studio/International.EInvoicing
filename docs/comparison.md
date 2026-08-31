# How this library compares

> Recorded **31 August 2026**, from the projects' own repositories, release notes and pricing pages. Every
> claim below is sourced; nothing is inferred from reputation. Re-check before quoting it: two of these
> projects changed licence or scope within the last six months.

[Prior art](prior-art.md) mines the neighbours' *issue trackers* for edge cases. This page does the other
half: what they **do**, what we do, and which of the differences are gaps worth closing.

## The field

| | Language | Licence | Scope | Validation |
|---|---|---|---|---|
| **This library** | .NET 8/10 | MIT, open | 20 countries, UBL + CII + CDAR, EN 16931, Peppol BIS + PINT, XRechnung, Factur-X, 5 national CIUS, e-reporting and tax data documents | Own Schematron engine, offline, no Java |
| **ZUGFeRD-csharp 18.0** | .NET | Open, **bug fixes only** | ZUGFeRD / Factur-X, XRechnung, some UBL output | None — moved to the commercial successor |
| **FactoorSharp** | .NET | **Commercial**, €349–1 099/year per developer tier | ZUGFeRD, Factur-X, XRechnung; validates FatturaPA and Facturae | Basic checks offline; the real validation is **online**, through Mustang, VeraPDF and Valitool |
| **mustangproject** | Java | APL 2.0, open | ZUGFeRD 1 → 2.5.2, Factur-X 1.09.x, XRechnung 3.0.2, Order-X, CII, UBL conversion | Full, offline, via Saxon |
| **Securibox.FacturX** | .NET | Open | Factur-X 1.08, five profiles. No UBL | XSD + Schematron + PDF/A-3 + XMP |
| **Mews fiscalizations** | .NET | Open | Reporting to national authorities — a different half of the problem | n/a |

**The single most important fact on this page**: in March 2026 ZUGFeRD-csharp — the .NET incumbent, 394 stars
and over a million downloads — shipped version 18.0 as its **last open-source release**. New features and
validation now live in [FactoorSharp](https://www.factoorsharp.de/en), a paid product. As of today, .NET has
no other maintained open-source library that validates an e-invoice against the published rules.

## Why validation is the hard part in .NET, and what everyone else does about it

EN 16931, Peppol and every national rule set are published as Schematron compiled to **XSLT 2.0**. .NET's
`XslCompiledTransform` implements XSLT 1.0 and throws on the first 2.0 construct. There is no official Saxon
port. The ecosystem's answer is Saxon-HE cross-compiled from Java bytecode by IKVM — which means a four-way
version matrix (Saxon, IKVM, the NuGet republisher, the runtime), Java package names in your C#, quarterly
artefact updates to track by hand, and an SVRL parsing layer to write yourself.

This library does not do that. It has **its own Schematron engine**: an XPath 2.0 subset measured against the
artefacts it must run, with the official conformance corpora as its test suite. No Java, no Saxon, no network
call, no rule set embedded at build time that goes stale. That is the difference that is hard to copy, and it
is why the country list below is possible at all.

It also reads **compiled** Schematron — the form several publishers ship *instead of* source — which is how
the Factur-X, Belgian, Romanian, Portuguese, Serbian, Croatian and Slovak rule sets run here at all.

## Where we are ahead

| | Us | Nearest competitor |
|---|---|---|
| **Countries** | 20 packages, from France to Japan | mustangproject: DE/FR; FactoorSharp: DE/FR + two validations |
| **Offline validation in .NET** | Own engine, no dependencies | FactoorSharp calls out to online services; nobody else in .NET validates |
| **Rule sets published only as compiled XSLT** | Read as data and run | Not attempted elsewhere |
| **Syntax conversion** | UBL ↔ CII **with a loss report** — what the crossing cost, found by reading the result back | mustangproject converts; the loss is not reported |
| **E-reporting and tax reporting** | French flux 10, Slovak and ViDA tax data documents, built from the invoice they report | None |
| **Lifecycle messages** | CDAR statuses, measured against the DGFiP's own rules and samples | None |
| **Reading hostile input** | Readers never throw; diagnostics with documented fallbacks; declared-encoding handling; depth, attachment and size limits; a published hostile corpus | Not addressed |
| **Raw preservation** | Every field keeps the text it arrived as, so nothing is silently normalised | Not offered |
| **A testing kit for integrators** | `International.EInvoicing.Testing`: conforming samples, round-trip by element census, the hostile corpus, framework-free assertions | None |
| **Licence** | MIT, and staying open | The .NET incumbent just closed |

## Where they are ahead — the real gaps

Ranked by what it would cost a user of this library today.

| Gap | Who has it | Why it matters | Size |
|---|---|---|---|
| **XSD schema validation** | Securibox, mustangproject, every Java tool | A document can be schema-invalid and still pass every Schematron rule that happens not to look at it. We ship the UBL 2.1 and CII D22B schemas under `specs/` and do not use them. This is the cheapest real gap on the list. | Small |
| **PDF/A-3 generation** | FactoorSharp, mustangproject | We attach CII to a PDF the caller already has. Turning an ordinary PDF into a conformant PDF/A-3 is what a caller with an existing print pipeline actually needs. Deliberately out of scope so far — worth revisiting, since it is the top reason to reach for a paid library. | Large |
| **PDF/A-3 and XMP conformance checking** | Securibox, FactoorSharp (via VeraPDF) | Half of "is this Factur-X valid?" is about the PDF, not the XML. We answer only the XML half. | Medium |
| **Visualisation** | mustangproject, FactoorSharp | Rendering an invoice for a human — the KoSIT and Factur-X stylesheets exist; running them needs the XSLT we deliberately do not host. | Medium |
| **Order-X** | mustangproject | Orders and order responses, same family, same syntax. Nothing in the model prevents it. | Medium |
| **ZUGFeRD 1.x reading and migration** | mustangproject, ZUGFeRD-csharp | A 2013 format still in archives. Reading it is a mapping job; nobody has asked. | Medium |
| **FatturaPA and Facturae** | FactoorSharp validates both | Italy and Spain as *national formats* rather than as Peppol. Both are on the roadmap and both are blocked on the same signature decision. | Large |
| **A REST server / container** | mustangserver | Not a library concern, but it is how many teams consume one. | Medium |

Two of these — PDF/A-3 generation and visualisation — are the ones a paying customer of FactoorSharp is
paying for. They are also the two furthest from what this library claims to be. That tension is worth
deciding deliberately rather than by drift; see the scope note in the [roadmap](roadmap.md).

## What we should not copy

- **Online validation.** FactoorSharp's extended validation calls out to Mustang, VeraPDF and Valitool. An
  invoice is a business record; sending one to a third party to ask whether it is valid is a decision the
  caller should make explicitly, never a library default.
- **Embedding rule sets at build time.** Rules change quarterly. This library loads them from artefacts you
  control, and says which ones ran.
- **One-country depth as a strategy.** It is what makes the incumbents excellent in Germany and useless in
  Norway.

## How this page stays honest

Every "we do this" above is measured by something in the test suite — 1 611 tests across 33 suites, including
the official EN 16931 and Peppol conformance corpora, the XRechnung test suite, and per-country invoices
judged by that country's own published rules. Every "they do this" is from the project's own documentation,
linked below. Where a claim could not be verified from a primary source, it is not on this page.

| Source | Used for |
|---|---|
| [ZUGFeRD-csharp releases](https://github.com/stephanstapel/ZUGFeRD-csharp/releases) | 18.0 scope, and the move to commercial |
| [ZUGFeRD-csharp README](https://github.com/stephanstapel/ZUGFeRD-csharp) | Bug-fixes-only status |
| [FactoorSharp](https://www.factoorsharp.de/en) | Formats, online validation, licence tiers and prices |
| [mustangproject](https://www.mustangproject.org/) | Formats, operations, licence |
| [Securibox.FacturX](https://github.com/Securibox/facturx) | Factur-X profiles, XSD + Schematron + PDF/A-3 + XMP validation |
| [Mews fiscalizations](https://github.com/MewsSystems/fiscalizations) | Scope |
| [Why validating Peppol UBL e-invoices in .NET is harder than it looks](https://dev.to/invoicexml/why-validating-peppol-ubl-e-invoices-in-net-is-harder-than-it-looks-3m2k) | The XSLT 2.0 problem and the IKVM/Saxon workaround |
