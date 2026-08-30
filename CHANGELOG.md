# Changelog

All notable changes to this project are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the project adheres to
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Peppol BIS Billing 3.0 validation, measured against Peppol's own unit corpus: 227 of 227 UBL cases and
  127 of 127 CII cases agree with the published expected results. The artefacts declare no licence and are
  not shipped — `build/fetch-specs.sh peppol` fetches them, and they load like any other rule set.
- French e-reporting — *flux 10*: the transactions and payments transmissions, with a model, a reader, a
  writer and builders that add the totals up from the VAT split rather than asking for them twice. Measured
  against the DGFiP's published flux 10 rules, which is the only measurement available since no sample
  transmissions are published.
- French lifecycle statuses measured against the DGFiP's own rules: every status, to a trading partner and to
  the public portal, and the eleven published sample messages, checked on each build. The artefacts are
  fetched (`build/fetch-specs.sh france`), not redistributed.
- `DocumentStatusDetail` and `DocumentStatusCharacteristic` on the CDAR model: the reason behind a status, the
  action requested, and the values at issue, read and written rather than kept as extension data.
- `FrCdar.IssuedByBuyer` / `IssuedBySeller`, `FrStatusReason`, `FrRequestedAction`, `FrStatusValueType`, and
  `Collected(FrCollectedAmount)` — what the French rules require of a status, asked for in the builder.
- The French invoice profile `urn:cen.eu:en16931:2017#conformant#urn.cpro.gouv.fr:1p0:extended-ctc-fr`.
- Rule sets may define their own functions in XSLT; the engine runs those definitions rather than
  reimplementing them, which is how the twenty French `custom:` functions work.
- Repository foundations: multi-targeted build (`net8.0`, `net10.0`), central package management,
  deterministic packaging, MinVer versioning from git tags.
- `SecureXml` and `DocumentLimits`: hardened XML reading for untrusted documents.
- Documentation set: standards references, recipes, diagnostic catalogue, architecture decisions.
- CI: build and test matrix, packaging, documentation gates, upstream specification monitoring.

### Fixed
- A control character in a caller's text no longer stops a document being written. XML cannot carry those
  characters at all, so they are dropped and everything else — accents, symbols, emoji — is written as it
  was. Found by reading what the neighbouring libraries have had to answer; see `docs/prior-art.md`.
- A Schematron rule context is a match pattern, not a path from the document root. Reading it as a path
  silently matched nothing for every relative context, leaving rules such as BR-29, BR-30, BR-CL-13 and the
  whole French lifecycle set dormant.
- The XPath range operator (`0 to $n`), `reverse`, and `xsl:choose` in a rule set's own functions.
- `castable as` now asks about the type it was given rather than always about a number, and `substring` is
  the window XPath defines rather than an offset and a count — `substring($v, 0, $n)` takes the first
  `n - 1` characters, which is how the Peppol check-digit functions are written.
- Ordering comparisons on dates (`xs:date(a) >= xs:date(b)`), the `text()` node test, and the `replace`,
  `translate`, `xs:string` and `string-to-codepoints` functions, all of which the published rule sets use.
- A validation message now names the rule that failed even when the rule set puts its code in the message
  rather than in an attribute, as the French e-reporting artefacts do.
