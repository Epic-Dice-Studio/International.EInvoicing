# ADR 0013 — Two editions of EN 16931, and which one a document declares

**Status:** accepted, August 2026

## Context

The standard this library is built on has been revised. CEN published **EN 16931-1:2026** in May 2026 and
**formally withdrew EN 16931-1:2017**, which remains compliant only for a migration period. The revision is a
ViDA revision: new business terms for the 2030 digital reporting requirements, invoice coding, repeated
orders, early-payment discounts and late-payment charges, wider handling of exempt supplies and special VAT
schemes, an extension methodology, and updated UBL and CII syntax bindings. It is **not backward
compatible**.

Three facts shape what can be decided today:

1. **The standard text is not public.** It is sold by CEN members. The list of business terms the revision
   adds cannot be derived from anything this repository is allowed to carry.
2. **The validation artefacts do not exist yet.** The maintainer of the EN 16931 artefacts said in
   [issue #445](https://github.com/ConnectingEurope/eInvoicing-EN16931/issues/445) that work on the 2026
   artefacts was starting; release 1.3.16 (April 2026), the one this library ships, is still a 2017 release.
3. **Every CIUS in circulation is a 2017 CIUS.** XRechnung, Peppol BIS Billing, Factur-X, the French
   Extended CTC FR — all of them name `urn:cen.eu:en16931:2017` as their base. OpenPEPPOL's BIS 4 is meant to
   move to the 2026 model and merge with PINT; it is not published.

So the model cannot be migrated yet, and pretending otherwise would mean inventing business terms and an
identifier. But documents declaring the new edition will appear before we can read them, and the library's
third promise — honest about its limits — applies exactly there.

## Decision

**The edition is a first-class thing the library can name, and a document declaring an edition we do not
implement is reported as such — not as an unknown profile.**

```csharp
En16931Edition.Implemented          // EN 16931-1:2017, what the model and the artefacts encode
En16931Edition.Revised              // EN 16931-1:2026, named so a caller can ask about it
En16931Edition.Of(declared)         // the edition a specification identifier declares, or null
```

The edition is read from the **year segment of the identifier** — `urn:cen.eu:en16931:2017`, and whatever the
2026 identifier turns out to be — rather than matched against a fixed list of published URNs. That is
deliberate: the URN for the 2026 edition is not something this library can assert today, and a pattern that
follows the scheme is honest where a hard-coded string would be a guess dressed as a fact.

Resolution then produces `EIV1044 UnsupportedEdition` instead of `EIV1042 UnknownProfile`: the document
*is* an EN 16931 invoice, and telling the caller it is unknown would send them looking for a profile
registration they cannot make. The document is read against the 2017 model, every shared term keeps its raw
text and attributes, and everything else goes to extension data. Validation runs the 2017 rules and the
coverage block names the edition they are for — `EN 16931-1:2017 (UBL)`.

A caller who has the 2026 specification can register a profile, a mapping and the artefacts themselves from
their own code, and their registration wins, as it does for any other profile.

## Consequences

**Good.** The revision cannot arrive silently. A 2026 invoice read today produces a usable document and an
error-severity diagnostic naming precisely what was missed and what to do about it, and no report claims a
clean pass against rules that do not cover the document in front of it. The upgrade path is already the
library's ordinary extension path.

**The cost.** `En16931Edition.Implemented` is a constant, so migrating the model means changing it and
everything that keys off it. That is the point: the compiler will list what has to move.

**What is still to decide**, once the specification and the artefacts are public: whether the two editions
live in one model with terms that are absent in 2017, or in two — the same question e-reporting answered by
taking its own model. `Field<T>` carries what a two-edition model needs either way, and a document read as
one edition and written as the other will need the same loss report the UBL ↔ CII conversion needs.

## Alternatives rejected

**Migrate the model to 2026 now.** Not possible without the standard text, and it would mean inventing terms.

**Hard-code `urn:cen.eu:en16931:2026` as a known profile.** It is the obvious continuation of the scheme and
it is probably right, but "probably right" in a published identifier is how a library ends up rejecting valid
documents. Reading the year covers it either way.

**Say nothing and let the fallback chain call it an unknown profile.** It parses the same, but the caller is
told the wrong thing about why.
